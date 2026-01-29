using System.Collections.Generic;
using PartyStatViewer.NetPackages;

namespace PartyStatViewer
{
    public static class SkillDataManager
    {
        public static void HandleSkillDataRequest(
            int requestingEntityId,
            string bookSeriesId,
            SkillType skillType,
            int maxLevel)
        {
            // Get requesting player to find their party
            EntityPlayer requestingPlayer = GameManager.Instance.World.GetEntity(requestingEntityId) as EntityPlayer;
            if (requestingPlayer == null) return;

            // Get display name from our mappings (or use seriesId as fallback)
            string displayName = GetDisplayNameForProgression(bookSeriesId);

            // Gather party members' skill levels (including the requesting player)
            var playerSkills = new List<NetPackageSkillDataResponse.PlayerSkillData>();
            foreach (EntityPlayer player in GetPartyMembers(requestingPlayer))
            {
                ProgressionValue pv = player.Progression.GetProgressionValue(bookSeriesId);
                int level = pv != null ? pv.Level : 0;
                playerSkills.Add(new NetPackageSkillDataResponse.PlayerSkillData
                {
                    entityId = player.entityId,
                    playerName = player.EntityName,
                    currentLevel = level
                });
            }

            // Send response to requesting client
            SingletonMonoBehaviour<ConnectionManager>.Instance.SendPackage(
                NetPackageManager.GetPackage<NetPackageSkillDataResponse>()
                    .Setup(bookSeriesId, skillType, maxLevel, displayName, playerSkills),
                false, requestingEntityId, -1, -1, null, 192);
        }

        public static string GetDisplayNameForProgression(string progressionName)
        {
            // Look up in PerkBookMappings
            foreach (var kvp in BookTypeDetector.PerkBookMappings)
            {
                if (kvp.Value.progressionName == progressionName)
                    return kvp.Value.displayName;
            }
            // Look up in CraftingMagazineMappings
            foreach (var kvp in BookTypeDetector.CraftingMagazineMappings)
            {
                if (kvp.Value.skill == progressionName)
                    return kvp.Value.displayName;
            }
            return progressionName; // Fallback
        }

        /// <summary>
        /// Gets all party members for a player (including the player themselves).
        /// </summary>
        public static IEnumerable<EntityPlayer> GetPartyMembers(EntityPlayer player)
        {
            // Always include the player themselves
            yield return player;

            // Get player's party
            Party party = player.Party;
            if (party == null) yield break;

            // Include all other party members
            foreach (EntityPlayer member in party.MemberList)
            {
                if (member.entityId != player.entityId)
                    yield return member;
            }
        }

        /// <summary>
        /// Gathers skill data for all party members for a given book series.
        /// </summary>
        public static List<NetPackageSkillDataResponse.PlayerSkillData> GatherPartySkillData(
            BookTypeDetector.BookInfo bookInfo,
            EntityPlayer forPlayer)
        {
            var result = new List<NetPackageSkillDataResponse.PlayerSkillData>();
            foreach (EntityPlayer player in GetPartyMembers(forPlayer))
            {
                ProgressionValue pv = player.Progression.GetProgressionValue(bookInfo.seriesId);
                int level = pv != null ? pv.Level : 0;
                result.Add(new NetPackageSkillDataResponse.PlayerSkillData
                {
                    entityId = player.entityId,
                    playerName = player.EntityName,
                    currentLevel = level
                });
            }
            return result;
        }
    }
}
