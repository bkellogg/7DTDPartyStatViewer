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
        public DateTime cachedAt;

        // Short duration - just long enough to display after server response
        private const int CacheDurationSeconds = 3;
        public bool IsExpired => (DateTime.Now - cachedAt).TotalSeconds > CacheDurationSeconds;
    }

    public static class SkillDataCache
    {
        private static readonly Dictionary<string, CachedSkillData> _cache =
            new Dictionary<string, CachedSkillData>();

        private static readonly HashSet<string> _pendingRequests = new HashSet<string>();

        public static CachedSkillData Get(string seriesId)
        {
            if (_cache.TryGetValue(seriesId, out var data))
            {
                if (!data.IsExpired)
                    return data;

                _cache.Remove(seriesId);
            }
            return null;
        }

        public static bool IsPending(string seriesId)
        {
            return _pendingRequests.Contains(seriesId);
        }

        public static void MarkPending(string seriesId)
        {
            _pendingRequests.Add(seriesId);
        }

        public static void Store(
            string seriesId,
            SkillType skillType,
            int maxLevel,
            string displayName,
            List<NetPackageSkillDataResponse.PlayerSkillData> playerSkills)
        {
            _pendingRequests.Remove(seriesId);
            _cache[seriesId] = new CachedSkillData
            {
                seriesId = seriesId,
                skillType = skillType,
                maxLevel = maxLevel,
                displayName = displayName,
                playerSkills = playerSkills,
                cachedAt = DateTime.Now
            };
        }

        public static void InvalidateEntry(string seriesId)
        {
            _cache.Remove(seriesId);
        }

        public static void InvalidateAll()
        {
            _cache.Clear();
            _pendingRequests.Clear();
        }

        public static (int entryCount, int pendingCount, List<CachedSkillData> entries) GetCacheInfo()
        {
            return (_cache.Count, _pendingRequests.Count, new List<CachedSkillData>(_cache.Values));
        }
    }
}
