using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using PartyStatViewer.NetPackages;

namespace PartyStatViewer.Harmony.Patches
{
    [HarmonyPatch(typeof(XUiC_ItemInfoWindow), "GetBindingValueInternal")]
    public static class ItemInfoWindowPatch
    {
        // Store reference to last active window for refresh triggering
        private static XUiC_ItemInfoWindow _lastActiveWindow;
        private static string _lastSeriesId;

        // Current cached data - only re-fetch when seriesId changes
        private static CachedSkillData _currentData;

        /// <summary>
        /// Called by NetPackageSkillDataResponse when data arrives.
        /// </summary>
        public static void OnDataReceived(string seriesId)
        {
            if (!string.Equals(_lastSeriesId, seriesId, System.StringComparison.OrdinalIgnoreCase))
                return;

            if (_currentData != null && string.Equals(_currentData.seriesId, seriesId, System.StringComparison.OrdinalIgnoreCase))
            {
                // Already have data displayed - this is an update from someone reading a book
                // Just clear cache, no refresh (avoids flicker with stale data)
                _currentData = null;
            }
            else
            {
                // No data yet - this is initial load response, refresh to show it
                if (_lastActiveWindow != null)
                {
                    try
                    {
                        _lastActiveWindow.RefreshBindings();
                    }
                    catch (System.Exception)
                    {
                        // Silently ignore
                    }
                }
            }
        }

        static void Postfix(
            XUiC_ItemInfoWindow __instance,
            ref string value,
            string bindingName,
            ref bool __result)
        {
            // Only intercept item description
            if (bindingName != "itemdescription") return;
            if (!__result) return;

            // Get current item
            ItemStack itemStack = __instance.itemStack;
            if (itemStack.IsEmpty()) return;

            // Check if it's a book/magazine we care about
            var bookInfo = BookTypeDetector.GetBookInfo(itemStack.itemValue.ItemClass);
            if (!bookInfo.isValid) return;

            // Store reference for refresh triggering
            _lastActiveWindow = __instance;
            _lastSeriesId = bookInfo.seriesId;

            bool isSinglePlayerOrServer = SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer;

            // Check if we already have data for this book
            if (_currentData != null && string.Equals(_currentData.seriesId, bookInfo.seriesId, System.StringComparison.OrdinalIgnoreCase))
            {
                // Already have data for this book, use it
                string cachedUnlocks = itemStack.itemValue.ItemClass?.Unlocks ?? "";
                value = value + "\n\n" + FormatSkillSection(_currentData, bookInfo, cachedUnlocks);
                return;
            }

            // Different book or no data yet - need to fetch
            if (isSinglePlayerOrServer)
            {
                // Single player/server - gather data directly (once)
                var localPlayer = GameManager.Instance.World.GetPrimaryPlayer();
                if (localPlayer == null) return;

                var playerSkills = SkillDataManager.GatherSkillDataForPlayer(
                    localPlayer, bookInfo.seriesId, bookInfo.type, bookInfo.maxLevel);
                string displayName = SkillDataManager.GetDisplayNameForProgression(bookInfo.seriesId);

                _currentData = new CachedSkillData
                {
                    seriesId = bookInfo.seriesId,
                    skillType = bookInfo.type,
                    maxLevel = bookInfo.maxLevel,
                    displayName = displayName,
                    playerSkills = playerSkills
                };
            }
            else
            {
                // Multiplayer client - check if response arrived
                var responseData = SkillDataCache.GetAndClear(bookInfo.seriesId);

                if (responseData != null)
                {
                    // Got response, store it
                    _currentData = responseData;
                }
                else
                {
                    // No response yet - send request if not already pending
                    if (!SkillDataCache.IsPending(bookInfo.seriesId))
                    {
                        SkillDataCache.MarkPending(bookInfo.seriesId);
                        RequestSkillDataFromServer(bookInfo);
                    }

                    // Show loading message
                    string loadingMsg = LoadingMessages.GetLoadingMessage(bookInfo.type, bookInfo.seriesId);
                    string loadingHeader;
                    if (bookInfo.type == SkillType.PerkBook)
                        loadingHeader = "[Party Progress]";
                    else if (bookInfo.type == SkillType.Schematic)
                        loadingHeader = "[Party Schematics]";
                    else
                        loadingHeader = "[Party Skill]";
                    value = value + "\n\n" + loadingHeader + "\n" + loadingMsg;
                    return;
                }
            }

            // Get the unlock progression name for this specific book
            string unlocks = itemStack.itemValue.ItemClass?.Unlocks ?? "";

            // Append party skill section to description
            value = value + "\n\n" + FormatSkillSection(_currentData, bookInfo, unlocks);
        }

        private static string FormatSkillSection(CachedSkillData data, BookTypeDetector.BookInfo bookInfo, string unlocks)
        {
            var sb = new StringBuilder();
            int localEntityId = GameManager.Instance.World.GetPrimaryPlayerId();

            // Header line
            string header;
            if (data.skillType == SkillType.PerkBook)
                header = "[Party Progress]";
            else if (data.skillType == SkillType.Schematic)
                header = "[Party Schematics]";
            else
                header = "[Party Skill]";
            sb.AppendLine(header);

            // For perk books, check if player already has this volume
            if (data.skillType == SkillType.PerkBook && !string.IsNullOrEmpty(unlocks))
            {
                // Find which volume number this book is by matching unlocks to BookGroup children
                int volumeNum = GetVolumeNumberForUnlock(data.seriesId, unlocks);
                var localPlayer = data.playerSkills.FirstOrDefault(p => p.entityId == localEntityId);

                if (volumeNum > 0 && HasVolume(localPlayer.volumesRead, volumeNum))
                {
                    sb.AppendLine($"\u26a0 You already have Vol {volumeNum}!");
                }
            }

            // For schematics, check if local player already knows all recipes
            if (data.skillType == SkillType.Schematic)
            {
                var localPlayer = data.playerSkills.FirstOrDefault(p => p.entityId == localEntityId);
                if (localPlayer.currentLevel >= data.maxLevel)
                {
                    sb.AppendLine("\u26a0 You already know this!");
                }
            }

            // Sort players: highest level first, local player ("You") always last
            var sorted = data.playerSkills
                .OrderByDescending(p => p.entityId == localEntityId ? -1 : p.currentLevel)
                .ToList();

            // Format each player's entry - compact view (just counts)
            foreach (var player in sorted)
            {
                string playerLabel = player.entityId == localEntityId ? "You" : player.playerName;
                bool isComplete = player.currentLevel >= data.maxLevel;

                if (data.skillType == SkillType.PerkBook)
                {
                    if (isComplete)
                    {
                        sb.AppendLine($"{playerLabel}: {player.currentLevel}/{data.maxLevel} \u2713 COMPLETE");
                    }
                    else
                    {
                        // Show missing volumes inline (compact)
                        string missing = FormatMissingVolumes(player.volumesRead, data.maxLevel);
                        sb.AppendLine($"{playerLabel}: {player.currentLevel}/{data.maxLevel} (need {missing})");
                    }
                }
                else if (data.skillType == SkillType.Schematic)
                {
                    if (data.maxLevel == 1)
                    {
                        // Single-recipe schematic (common case)
                        sb.AppendLine(isComplete
                            ? $"{playerLabel}: Known \u2713"
                            : $"{playerLabel}: Not learned");
                    }
                    else
                    {
                        // Multi-recipe schematic
                        if (isComplete)
                        {
                            sb.AppendLine($"{playerLabel}: {player.currentLevel}/{data.maxLevel} \u2713 All known");
                        }
                        else
                        {
                            string missing = FormatMissingRecipes(player.volumesRead, data.seriesId);
                            sb.AppendLine($"{playerLabel}: {player.currentLevel}/{data.maxLevel} (need {missing})");
                        }
                    }
                }
                else
                {
                    string completionMark = isComplete ? " \u2713 MAX" : "";
                    sb.AppendLine($"{playerLabel}: {player.currentLevel}/{data.maxLevel}{completionMark}");
                }
            }

            return sb.ToString().TrimEnd();
        }

        /// <summary>
        /// Finds the volume number (1-based) for a given unlock progression name by matching
        /// it against the children of the BookGroup.
        /// </summary>
        private static int GetVolumeNumberForUnlock(string bookGroupName, string unlocks)
        {
            if (string.IsNullOrEmpty(bookGroupName) || string.IsNullOrEmpty(unlocks))
                return 0;

            if (Progression.ProgressionClasses == null)
                return 0;

            if (!Progression.ProgressionClasses.TryGetValue(bookGroupName, out ProgressionClass bookGroupClass))
                return 0;

            int volumeNumber = 0;
            foreach (var childClass in bookGroupClass.Children)
            {
                volumeNumber++;
                if (volumeNumber > 7) break; // Only check volumes, not completion perk

                // Check if this child's name matches the unlocks value (case-insensitive)
                if (string.Equals(childClass.Name, unlocks, System.StringComparison.OrdinalIgnoreCase))
                    return volumeNumber;
            }

            return 0;
        }

        /// <summary>
        /// Checks if a volume number is in the volumesRead string
        /// </summary>
        private static bool HasVolume(string volumesRead, int volumeNum)
        {
            if (string.IsNullOrEmpty(volumesRead)) return false;

            foreach (var part in volumesRead.Split(','))
            {
                if (int.TryParse(part.Trim(), out int vol) && vol == volumeNum)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Returns a compact string of missing volume numbers (e.g., "1,4,6")
        /// </summary>
        private static string FormatMissingVolumes(string volumesRead, int maxLevel)
        {
            // Parse the comma-separated volume numbers that ARE read
            var readSet = new System.Collections.Generic.HashSet<int>();
            if (!string.IsNullOrEmpty(volumesRead))
            {
                foreach (var part in volumesRead.Split(','))
                {
                    if (int.TryParse(part.Trim(), out int vol))
                    {
                        readSet.Add(vol);
                    }
                }
            }

            // Build list of missing volumes
            var missing = new System.Collections.Generic.List<int>();
            for (int i = 1; i <= maxLevel; i++)
            {
                if (!readSet.Contains(i))
                {
                    missing.Add(i);
                }
            }

            return missing.Count > 0 ? string.Join(",", missing) : "none";
        }

        /// <summary>
        /// Returns a compact string of missing recipe names for a multi-recipe schematic.
        /// </summary>
        private static string FormatMissingRecipes(string knownRecipes, string schematicItemName)
        {
            ItemClass itemClass = ItemClass.GetItemClass(schematicItemName, false);
            if (itemClass == null) return "?";

            string[] recipes = BookTypeDetector.GetSchematicRecipes(itemClass);
            if (recipes == null || recipes.Length == 0) return "?";

            var knownSet = new HashSet<string>();
            if (!string.IsNullOrEmpty(knownRecipes))
            {
                foreach (var name in knownRecipes.Split(','))
                {
                    string trimmed = name.Trim();
                    if (trimmed.Length > 0)
                        knownSet.Add(trimmed);
                }
            }

            var missing = new List<string>();
            foreach (string recipe in recipes)
            {
                if (!knownSet.Contains(recipe))
                    missing.Add(Localization.Get(recipe));
            }

            return missing.Count > 0 ? string.Join(", ", missing) : "none";
        }

        private static void RequestSkillDataFromServer(BookTypeDetector.BookInfo bookInfo)
        {
            // This is only called for multiplayer clients
            var localPlayer = GameManager.Instance.World.GetPrimaryPlayer();
            if (localPlayer == null) return;

            SingletonMonoBehaviour<ConnectionManager>.Instance.SendToServer(
                NetPackageManager.GetPackage<NetPackageSkillDataRequest>()
                    .Setup(localPlayer.entityId, bookInfo.seriesId, bookInfo.type, bookInfo.maxLevel));
        }
    }
}
