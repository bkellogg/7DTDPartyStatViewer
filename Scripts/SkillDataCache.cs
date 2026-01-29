using System;
using System.Collections.Generic;
using PartyStatViewer.NetPackages;

namespace PartyStatViewer
{
    public class CachedSkillData
    {
        public string seriesId;
        public SkillType skillType;
        public int maxLevel;
        public string displayName;
        public List<NetPackageSkillDataResponse.PlayerSkillData> playerSkills;
    }

    /// <summary>
    /// Tracks pending requests and stores the most recent response for immediate display.
    /// No long-term caching - data is cleared after being displayed once.
    /// </summary>
    public static class SkillDataCache
    {
        // Store just the latest response for each series (for immediate display after response arrives)
        private static readonly Dictionary<string, CachedSkillData> _latestResponse =
            new Dictionary<string, CachedSkillData>();

        // Track pending requests with timeout
        private static readonly Dictionary<string, DateTime> _pendingRequests =
            new Dictionary<string, DateTime>();

        // Pending requests timeout after this many seconds
        private const double PendingTimeoutSeconds = 2.0;

        /// <summary>
        /// Gets the latest response data and clears it (one-time use).
        /// </summary>
        public static CachedSkillData GetAndClear(string seriesId)
        {
            if (_latestResponse.TryGetValue(seriesId, out var data))
            {
                _latestResponse.Remove(seriesId);
                return data;
            }
            return null;
        }

        /// <summary>
        /// Checks if there's a response ready without consuming it.
        /// </summary>
        public static bool HasResponse(string seriesId)
        {
            return _latestResponse.ContainsKey(seriesId);
        }

        public static bool IsPending(string seriesId)
        {
            if (_pendingRequests.TryGetValue(seriesId, out var requestTime))
            {
                // Check if pending request has timed out
                if ((DateTime.Now - requestTime).TotalSeconds > PendingTimeoutSeconds)
                {
                    _pendingRequests.Remove(seriesId);
                    return false;
                }
                return true;
            }
            return false;
        }

        public static void MarkPending(string seriesId)
        {
            _pendingRequests[seriesId] = DateTime.Now;
        }

        public static void Store(
            string seriesId,
            SkillType skillType,
            int maxLevel,
            string displayName,
            List<NetPackageSkillDataResponse.PlayerSkillData> playerSkills)
        {
            _pendingRequests.Remove(seriesId);
            _latestResponse[seriesId] = new CachedSkillData
            {
                seriesId = seriesId,
                skillType = skillType,
                maxLevel = maxLevel,
                displayName = displayName,
                playerSkills = playerSkills
            };
        }

        public static void ClearAll()
        {
            _latestResponse.Clear();
            _pendingRequests.Clear();
        }

        public static (int responseCount, int pendingCount) GetStats()
        {
            return (_latestResponse.Count, _pendingRequests.Count);
        }
    }
}
