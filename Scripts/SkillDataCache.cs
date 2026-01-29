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

        private const int CacheDurationSeconds = 5;
        public bool IsExpired => (DateTime.Now - cachedAt).TotalSeconds > CacheDurationSeconds;
    }

    public static class SkillDataCache
    {
        private static readonly Dictionary<string, CachedSkillData> _cache =
            new Dictionary<string, CachedSkillData>();

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

        public static void Store(
            string seriesId,
            SkillType skillType,
            int maxLevel,
            string displayName,
            List<NetPackageSkillDataResponse.PlayerSkillData> playerSkills)
        {
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

        public static void InvalidateAll()
        {
            _cache.Clear();
        }
    }
}
