using System.Collections.Generic;
using System.Linq;
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
                        return;
                }
            }

            Log.Warning("[PartyStatViewer] Could not find level field via reflection");
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
            return pv.Level;
        }

        /// <summary>
        /// Gets the book progress for a player, returning both the count and which volumes are read.
        /// </summary>
        private static (int count, string volumesRead) GetBookGroupProgress(EntityPlayer player, string bookGroupName, int maxVolumes)
        {
            if (Progression.ProgressionClasses == null)
                return (0, "");

            if (!Progression.ProgressionClasses.TryGetValue(bookGroupName, out ProgressionClass bookGroupClass))
                return (0, "");

            // Iterate through children and track which volumes are read (up to maxVolumes)
            var readVolumes = new List<int>();
            int volumeNumber = 0;

            foreach (var childClass in bookGroupClass.Children)
            {
                volumeNumber++;

                // Stop at max volumes (skip completion perk)
                if (volumeNumber > maxVolumes)
                    break;

                ProgressionValue childPv = player.Progression.GetProgressionValue(childClass.Name);
                int level = childPv != null ? childPv.Level : 0;

                if (level > 0)
                    readVolumes.Add(volumeNumber);
            }

            return (readVolumes.Count, string.Join(",", readVolumes));
        }

        /// <summary>
        /// Gets schematic progress for a player by checking CVars for each recipe the schematic unlocks.
        /// Returns count of known recipes and comma-separated list of known recipe names.
        /// </summary>
        private static (int count, string knownRecipes) GetSchematicProgress(EntityPlayer player, string schematicItemName)
        {
            ItemClass itemClass = ItemClass.GetItemClass(schematicItemName, false);
            if (itemClass == null)
                return (0, "");

            string[] recipes = BookTypeDetector.GetSchematicRecipes(itemClass);
            if (recipes == null || recipes.Length == 0)
                return (0, "");

            var knownRecipes = new List<string>();
            foreach (string recipeName in recipes)
            {
                if (player.GetCVar(recipeName) == 1f)
                    knownRecipes.Add(recipeName);
            }

            return (knownRecipes.Count, string.Join(",", knownRecipes));
        }

        public static void HandleSkillDataRequest(
            int requestingEntityId,
            string bookSeriesId,
            SkillType skillType,
            int maxLevel)
        {
            EntityPlayer requestingPlayer = GameManager.Instance.World.GetEntity(requestingEntityId) as EntityPlayer;
            if (requestingPlayer == null) return;

            string displayName = GetDisplayNameForProgression(bookSeriesId);

            var playerSkills = new List<NetPackageSkillDataResponse.PlayerSkillData>();
            foreach (EntityPlayer player in GetPartyMembers(requestingPlayer))
            {
                int level;
                string volumesRead = "";

                if (skillType == SkillType.PerkBook)
                {
                    var progress = GetBookGroupProgress(player, bookSeriesId, maxLevel);
                    level = progress.count;
                    volumesRead = progress.volumesRead;
                }
                else if (skillType == SkillType.Schematic)
                {
                    var progress = GetSchematicProgress(player, bookSeriesId);
                    level = progress.count;
                    volumesRead = progress.knownRecipes;
                }
                else
                {
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

            SingletonMonoBehaviour<ConnectionManager>.Instance.SendPackage(
                NetPackageManager.GetPackage<NetPackageSkillDataResponse>()
                    .Setup(bookSeriesId, skillType, maxLevel, displayName, playerSkills),
                false, requestingEntityId, -1, -1, null, 192);
        }

        public static string GetDisplayNameForProgression(string progressionName)
        {
            foreach (var kvp in BookTypeDetector.PerkBookMappings)
            {
                if (kvp.Value.progressionName == progressionName)
                    return kvp.Value.displayName;
            }
            foreach (var kvp in BookTypeDetector.CraftingMagazineMappings)
            {
                if (kvp.Value.skill == progressionName)
                    return kvp.Value.displayName;
            }

            // For schematics, the seriesId is the item class name - try localization
            ItemClass itemClass = ItemClass.GetItemClass(progressionName, false);
            if (itemClass != null)
                return Localization.Get(progressionName);

            return progressionName;
        }

        public static IEnumerable<EntityPlayer> GetPartyMembers(EntityPlayer player)
        {
            yield return player;

            Party party = player.Party;
            if (party == null) yield break;

            foreach (EntityPlayer member in party.MemberList)
            {
                if (member.entityId != player.entityId)
                    yield return member;
            }
        }

        public static List<NetPackageSkillDataResponse.PlayerSkillData> GatherPartySkillData(
            BookTypeDetector.BookInfo bookInfo,
            EntityPlayer forPlayer)
        {
            return GatherSkillDataForPlayer(forPlayer, bookInfo.seriesId, bookInfo.type, bookInfo.maxLevel);
        }

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
                    var progress = GetBookGroupProgress(player, progressionName, maxLevel);
                    level = progress.count;
                    volumesRead = progress.volumesRead;
                }
                else if (skillType == SkillType.Schematic)
                {
                    var progress = GetSchematicProgress(player, progressionName);
                    level = progress.count;
                    volumesRead = progress.knownRecipes;
                }
                else
                {
                    ProgressionValue pv = player.Progression.GetProgressionValue(progressionName);
                    level = GetActualLevel(pv);
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
