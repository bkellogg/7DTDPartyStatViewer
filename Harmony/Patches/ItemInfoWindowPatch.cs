using System.Linq;
using System.Text;
using HarmonyLib;
using PartyStatViewer.NetPackages;

namespace PartyStatViewer.Harmony.Patches
{
    [HarmonyPatch(typeof(XUiC_ItemInfoWindow), "GetBindingValueInternal")]
    public static class ItemInfoWindowPatch
    {
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

            // Debug: Log item name
            string itemName = itemStack.itemValue.ItemClass?.Name ?? "null";
            Log.Out($"[PartyStatViewer] Item selected: {itemName}");

            // Check if it's a book/magazine we care about
            var bookInfo = BookTypeDetector.GetBookInfo(itemStack.itemValue.ItemClass);
            Log.Out($"[PartyStatViewer] BookInfo valid: {bookInfo.isValid}, type: {bookInfo.type}, seriesId: {bookInfo.seriesId}");
            if (!bookInfo.isValid) return;

            // Get skill data - always fresh, no caching
            bool isSinglePlayerOrServer = SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer;

            CachedSkillData skillData;
            if (isSinglePlayerOrServer)
            {
                // Single player/server - gather data directly
                var localPlayer = GameManager.Instance.World.GetPrimaryPlayer();
                if (localPlayer == null) return;

                var playerSkills = SkillDataManager.GatherSkillDataForPlayer(
                    localPlayer, bookInfo.seriesId, bookInfo.type, bookInfo.maxLevel);
                string displayName = SkillDataManager.GetDisplayNameForProgression(bookInfo.seriesId);

                skillData = new CachedSkillData
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
                // Multiplayer client - request from server, store response for this frame
                skillData = SkillDataCache.Get(bookInfo.seriesId);

                // Always request fresh data, but don't spam while waiting
                if (!SkillDataCache.IsPending(bookInfo.seriesId))
                {
                    SkillDataCache.MarkPending(bookInfo.seriesId);
                    SkillDataCache.InvalidateEntry(bookInfo.seriesId); // Clear old data
                    RequestSkillDataFromServer(bookInfo);
                }

                if (skillData == null)
                {
                    // Still waiting for server response
                    string loadingMsg = LoadingMessages.GetLoadingMessage(bookInfo.type, bookInfo.seriesId);
                    value = value + "\n\n--- Party Progress ---\n" + loadingMsg;
                    return;
                }
            }

            Log.Out($"[PartyStatViewer] Got cached data: {skillData.playerSkills?.Count ?? 0} players");

            // TODO: Re-enable party check after testing
            // Hide section if not in a party (solo play)
            // if (skillData.playerSkills == null || skillData.playerSkills.Count <= 1)
            // {
            //     Log.Out("[PartyStatViewer] Hiding - not in party or solo play");
            //     return;
            // }

            // Get the unlock progression name for this specific book
            string unlocks = itemStack.itemValue.ItemClass?.Unlocks ?? "";
            Log.Out($"[PartyStatViewer] Item unlocks: '{unlocks}'");

            // Append party skill section to description
            value = value + "\n\n" + FormatSkillSection(skillData, bookInfo, unlocks);
        }

        private static string FormatSkillSection(CachedSkillData data, BookTypeDetector.BookInfo bookInfo, string unlocks)
        {
            var sb = new StringBuilder();
            int localEntityId = GameManager.Instance.World.GetPrimaryPlayerId();

            // Header line
            string header = data.skillType == SkillType.PerkBook
                ? $"--- {data.displayName} Progress ---"
                : $"--- {data.displayName} Skill ---";
            sb.AppendLine(header);

            // For perk books, check if player already has this volume
            if (data.skillType == SkillType.PerkBook && !string.IsNullOrEmpty(unlocks))
            {
                // Find which volume number this book is by matching unlocks to BookGroup children
                int volumeNum = GetVolumeNumberForUnlock(data.seriesId, unlocks);
                var localPlayer = data.playerSkills.FirstOrDefault(p => p.entityId == localEntityId);

                Log.Out($"[PartyStatViewer] Warning check: unlocks='{unlocks}', volumeNum={volumeNum}, volumesRead='{localPlayer.volumesRead}'");

                if (volumeNum > 0 && HasVolume(localPlayer.volumesRead, volumeNum))
                {
                    sb.AppendLine($"\u26a0 You already have Vol {volumeNum}!");
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
            {
                Log.Out($"[PartyStatViewer] GetVolumeNumberForUnlock: Could not find BookGroup '{bookGroupName}'");
                return 0;
            }

            int volumeNumber = 0;
            foreach (var childClass in bookGroupClass.Children)
            {
                volumeNumber++;
                if (volumeNumber > 7) break; // Only check volumes, not completion perk

                // Check if this child's name matches the unlocks value (case-insensitive)
                if (string.Equals(childClass.Name, unlocks, System.StringComparison.OrdinalIgnoreCase))
                {
                    Log.Out($"[PartyStatViewer] GetVolumeNumberForUnlock: Found '{unlocks}' at volume {volumeNumber}");
                    return volumeNumber;
                }
            }

            Log.Out($"[PartyStatViewer] GetVolumeNumberForUnlock: Could not find '{unlocks}' in children of '{bookGroupName}'");
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
