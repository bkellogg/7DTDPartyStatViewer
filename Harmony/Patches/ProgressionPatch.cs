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

            // For individual books (type Book), we need to use the parent BookGroup
            // For crafting skills, use the progression directly
            string progressionName;
            SkillType skillType;
            int maxLevel;

            if (progressionClass.IsBook)
            {
                // Individual book volume - get parent BookGroup info
                var parentClass = progressionClass.Parent;
                if (parentClass == null || !parentClass.IsBookGroup) return;

                progressionName = parentClass.Name;
                skillType = SkillType.PerkBook;
                maxLevel = 7; // Perk books always have 7 volumes
            }
            else if (progressionClass.IsCrafting)
            {
                progressionName = progressionClass.Name;
                skillType = SkillType.CraftingMagazine;
                maxLevel = progressionClass.MaxLevel;
            }
            else
            {
                return; // Not a progression type we care about
            }

            // Find which player owns this progression
            EntityPlayer owner = FindProgressionOwner(__instance);
            if (owner == null) return;

            // Only broadcast if the player is in a party
            if (owner.Party == null) return;

            string displayName = SkillDataManager.GetDisplayNameForProgression(progressionName);

            // Send updated skill data to each party member using the same logic as SkillDataManager
            foreach (EntityPlayer partyMember in SkillDataManager.GetPartyMembers(owner))
            {
                var playerSkills = SkillDataManager.GatherSkillDataForPlayer(
                    partyMember, progressionName, skillType, maxLevel);

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
