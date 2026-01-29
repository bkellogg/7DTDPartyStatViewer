using System.Collections.Generic;
using System.Reflection;
using PartyStatViewer.NetPackages;

namespace PartyStatViewer
{
    public static class SkillDataManager
    {
        // Cache the reflection field for performance
        private static readonly FieldInfo LevelField;
        private static bool _reflectionWarningLogged = false;

        static SkillDataManager()
        {
            // Try to find the level field with different possible binding flags
            // The game's DLLs may have publicized fields (decompiled version shows [PublicizedFrom])
            var bindingCombinations = new[]
            {
                BindingFlags.NonPublic | BindingFlags.Instance,
                BindingFlags.Public | BindingFlags.Instance,
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy,
            };

            var fieldNames = new[] { "level", "Level", "_level" };

            foreach (var fieldName in fieldNames)
            {
                foreach (var binding in bindingCombinations)
                {
                    LevelField = typeof(ProgressionValue).GetField(fieldName, binding);
                    if (LevelField != null)
                    {
                        Log.Out($"[PartyStatViewer] Found level field: {LevelField.Name} (Type: {LevelField.FieldType}, Binding: {binding})");
                        return;
                    }
                }
            }

            // If we still didn't find it, log all fields for debugging
            Log.Warning("[PartyStatViewer] Could not find level field via reflection. Listing all fields...");
            foreach (var field in typeof(ProgressionValue).GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public))
            {
                Log.Out($"[PartyStatViewer]   Field: {field.Name} (Type: {field.FieldType})");
            }
        }

        /// <summary>
        /// Gets the actual level value from a ProgressionValue, bypassing the Level property
        /// which returns MaxLevel for skill types.
        /// </summary>
        private static int GetActualLevel(ProgressionValue pv)
        {
            if (pv == null) return 0;

            // If we have the reflection field, use it
            if (LevelField != null)
            {
                try
                {
                    object value = LevelField.GetValue(pv);
                    if (value != null)
                    {
                        return (int)value;
                    }
                }
                catch (System.Exception ex)
                {
                    if (!_reflectionWarningLogged)
                    {
                        Log.Warning($"[PartyStatViewer] Reflection failed: {ex.Message}");
                        _reflectionWarningLogged = true;
                    }
                }
            }

            // Fallback: Use the Level property (may return MaxLevel for skills)
            // This is better than returning 0
            return pv.Level;
        }

        /// <summary>
        /// Gets the book progress for a player, returning both the count and which volumes are read.
        /// For perk book series (BookGroup), we need to count child Book progressions.
        /// </summary>
        /// <returns>Tuple of (readCount, volumesReadString) where volumesReadString is like "1,3,5"</returns>
        private static (int count, string volumesRead) GetBookGroupProgress(EntityPlayer player, string bookGroupName, int maxVolumes)
        {
            // Look up the ProgressionClass to get all book volumes
            if (Progression.ProgressionClasses == null)
            {
                Log.Warning("[PartyStatViewer] ProgressionClasses is null");
                return (0, "");
            }

            if (!Progression.ProgressionClasses.TryGetValue(bookGroupName, out ProgressionClass bookGroupClass))
            {
                Log.Warning($"[PartyStatViewer] Could not find ProgressionClass for '{bookGroupName}'");
                return (0, "");
            }

            Log.Out($"[PartyStatViewer] BookGroup '{bookGroupName}': maxVolumes={maxVolumes}, Children.Count={bookGroupClass.Children.Count}");

            // Iterate through children and track which volumes are read (up to maxVolumes)
            var readVolumes = new List<int>();
            int volumeNumber = 0;

            foreach (var childClass in bookGroupClass.Children)
            {
                volumeNumber++;

                // Stop at max volumes (skip completion perk)
                if (volumeNumber > maxVolumes)
                {
                    break;
                }

                ProgressionValue childPv = player.Progression.GetProgressionValue(childClass.Name);
                int level = childPv != null ? childPv.Level : 0;

                if (level > 0)
                {
                    readVolumes.Add(volumeNumber);
                }
            }

            string volumesStr = string.Join(",", readVolumes);
            Log.Out($"[PartyStatViewer] GetBookGroupProgress for {player.EntityName}: '{bookGroupName}' = {readVolumes.Count}/{maxVolumes}, volumes: [{volumesStr}]");
            return (readVolumes.Count, volumesStr);
        }

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
                int level;
                string volumesRead = "";

                if (skillType == SkillType.PerkBook)
                {
                    // For perk books, count individual read volumes
                    var progress = GetBookGroupProgress(player, bookSeriesId, maxLevel);
                    level = progress.count;
                    volumesRead = progress.volumesRead;
                }
                else
                {
                    // For crafting magazines, use reflection to get the actual level
                    ProgressionValue pv = player.Progression.GetProgressionValue(bookSeriesId);
                    level = GetActualLevel(pv);
                }

                playerSkills.Add(new NetPackageSkillDataResponse.PlayerSkillData
                {
                    entityId = player.entityId,
                    playerName = player.EntityName,
                    currentLevel = level,
                    volumesRead = volumesRead
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
            return GatherSkillDataForPlayer(forPlayer, bookInfo.seriesId, bookInfo.type, bookInfo.maxLevel);
        }

        /// <summary>
        /// Gathers skill data for all party members for a given progression.
        /// Used for both local (single player) and server-side requests.
        /// </summary>
        public static List<NetPackageSkillDataResponse.PlayerSkillData> GatherSkillDataForPlayer(
            EntityPlayer forPlayer,
            string progressionName,
            SkillType skillType,
            int maxLevel)
        {
            var result = new List<NetPackageSkillDataResponse.PlayerSkillData>();
            foreach (EntityPlayer player in GetPartyMembers(forPlayer))
            {
                int level;
                string volumesRead = "";

                if (skillType == SkillType.PerkBook)
                {
                    // For perk books, count individual read volumes
                    var progress = GetBookGroupProgress(player, progressionName, maxLevel);
                    level = progress.count;
                    volumesRead = progress.volumesRead;
                }
                else
                {
                    // For crafting magazines, use reflection to get the actual level
                    ProgressionValue pv = player.Progression.GetProgressionValue(progressionName);
                    level = GetActualLevel(pv);
                    Log.Out($"[PartyStatViewer] Player {player.EntityName}: crafting progression '{progressionName}' = {level} (pv null: {pv == null})");
                }

                result.Add(new NetPackageSkillDataResponse.PlayerSkillData
                {
                    entityId = player.entityId,
                    playerName = player.EntityName,
                    currentLevel = level,
                    volumesRead = volumesRead
                });
            }
            return result;
        }
    }
}
