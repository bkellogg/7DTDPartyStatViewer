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

            // Get cached skill data (or request from server if not cached)
            var skillData = SkillDataCache.Get(bookInfo.seriesId);
            if (skillData == null)
            {
                Log.Out($"[PartyStatViewer] No cached data for {bookInfo.seriesId}, requesting from server...");
                // Request from server, display will update when response arrives
                RequestSkillDataFromServer(bookInfo);
                return;
            }

            Log.Out($"[PartyStatViewer] Got cached data: {skillData.playerSkills?.Count ?? 0} players");

            // Hide section if not in a party (solo play)
            if (skillData.playerSkills == null || skillData.playerSkills.Count <= 1)
            {
                Log.Out("[PartyStatViewer] Hiding - not in party or solo play");
                return;
            }

            // Append party skill section to description
            value = value + "\n\n" + FormatSkillSection(skillData);
        }

        private static string FormatSkillSection(CachedSkillData data)
        {
            var sb = new StringBuilder();

            // Header line
            string header = data.skillType == SkillType.PerkBook
                ? $"--- {data.displayName} Progress ---"
                : $"--- {data.displayName} Skill ---";
            sb.AppendLine(header);

            // Sort players: highest level first, local player ("You") always last
            int localEntityId = GameManager.Instance.World.GetPrimaryPlayerId();
            var sorted = data.playerSkills
                .OrderByDescending(p => p.entityId == localEntityId ? -1 : p.currentLevel)
                .ToList();

            // Format each player's entry
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
                        sb.AppendLine($"{playerLabel}: {player.currentLevel}/{data.maxLevel} volumes");
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

        private static void RequestSkillDataFromServer(BookTypeDetector.BookInfo bookInfo)
        {
            var localPlayer = GameManager.Instance.World.GetPrimaryPlayer();
            if (localPlayer == null) return;

            SingletonMonoBehaviour<ConnectionManager>.Instance.SendToServer(
                NetPackageManager.GetPackage<NetPackageSkillDataRequest>()
                    .Setup(localPlayer.entityId, bookInfo.seriesId, bookInfo.type, bookInfo.maxLevel));
        }
    }
}
