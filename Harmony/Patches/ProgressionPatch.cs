using System.Collections.Generic;
using HarmonyLib;
using PartyStatViewer.NetPackages;

namespace PartyStatViewer.Harmony.Patches
{
    /// <summary>
    /// Broadcasts skill updates to party members when any player's book/crafting skill changes.
    /// </summary>
    [HarmonyPatch(typeof(ProgressionValue))]
    [HarmonyPatch("Level", MethodType.Setter)]
    public static class ProgressionPatch
    {
        static void Postfix(ProgressionValue __instance)
        {
            if (!SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer) return;

            // Only broadcast for book/crafting progressions we care about
            var progressionClass = __instance.ProgressionClass;
            if (progressionClass == null) return;
            if (!progressionClass.IsBook && !progressionClass.IsCrafting) return;

            // Find which player owns this progression
            EntityPlayer owner = FindProgressionOwner(__instance);
            if (owner == null) return;

            // Only broadcast if the player is in a party
            if (owner.Party == null) return;

            // Determine skill type and gather all players' data for this progression
            string progressionName = progressionClass.Name;
            SkillType skillType = progressionClass.IsBook ? SkillType.PerkBook : SkillType.CraftingMagazine;
            int maxLevel = progressionClass.MaxLevel;
            string displayName = SkillDataManager.GetDisplayNameForProgression(progressionName);

            // Send updated skill data to each party member
            foreach (EntityPlayer partyMember in SkillDataManager.GetPartyMembers(owner))
            {
                // Gather this party member's view of party skill levels
                var playerSkills = new List<NetPackageSkillDataResponse.PlayerSkillData>();
                foreach (EntityPlayer member in SkillDataManager.GetPartyMembers(partyMember))
                {
                    ProgressionValue pv = member.Progression.GetProgressionValue(progressionName);
                    int level = pv != null ? pv.Level : 0;
                    playerSkills.Add(new NetPackageSkillDataResponse.PlayerSkillData
                    {
                        entityId = member.entityId,
                        playerName = member.EntityName,
                        currentLevel = level
                    });
                }

                // Send to this party member
                SingletonMonoBehaviour<ConnectionManager>.Instance.SendPackage(
                    NetPackageManager.GetPackage<NetPackageSkillDataResponse>()
                        .Setup(progressionName, skillType, maxLevel, displayName, playerSkills),
                    false, partyMember.entityId, -1, -1, null, 192);
            }
        }

        private static EntityPlayer FindProgressionOwner(ProgressionValue pv)
        {
            // Find the player who owns this progression by matching the ProgressionValue reference
            foreach (EntityPlayer player in GameManager.Instance.World.Players.list)
            {
                if (player.Progression.GetProgressionValue(pv.ProgressionClass.Name) == pv)
                    return player;
            }
            return null;
        }
    }
}
