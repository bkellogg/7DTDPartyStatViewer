using System.Collections.Generic;
using System.IO;

namespace PartyStatViewer.NetPackages
{
    public enum SkillType : byte
    {
        PerkBook = 0,
        CraftingMagazine = 1
    }

    public class NetPackageSkillDataResponse : NetPackage
    {
        private string bookSeriesId;
        private byte skillType;
        private int maxLevel;
        private string displayName;
        private List<PlayerSkillData> playerSkills;

        public struct PlayerSkillData
        {
            public int entityId;
            public string playerName;
            public int currentLevel;
            // For perk books: comma-separated list of read volume numbers (e.g., "1,3,5")
            public string volumesRead;
        }

        public NetPackageSkillDataResponse Setup(
            string seriesId,
            SkillType type,
            int max,
            string name,
            List<PlayerSkillData> skills)
        {
            this.bookSeriesId = seriesId;
            this.skillType = (byte)type;
            this.maxLevel = max;
            this.displayName = name;
            this.playerSkills = skills;
            return this;
        }

        public override void read(PooledBinaryReader _reader)
        {
            bookSeriesId = _reader.ReadString();
            skillType = _reader.ReadByte();
            maxLevel = _reader.ReadInt32();
            displayName = _reader.ReadString();
            int count = _reader.ReadInt32();
            playerSkills = new List<PlayerSkillData>(count);
            for (int i = 0; i < count; i++)
            {
                playerSkills.Add(new PlayerSkillData
                {
                    entityId = _reader.ReadInt32(),
                    playerName = _reader.ReadString(),
                    currentLevel = _reader.ReadInt32(),
                    volumesRead = _reader.ReadString()
                });
            }
        }

        public override void write(PooledBinaryWriter _writer)
        {
            base.write(_writer);
            _writer.Write(bookSeriesId);
            _writer.Write(skillType);
            _writer.Write(maxLevel);
            _writer.Write(displayName);
            _writer.Write(playerSkills.Count);
            foreach (var data in playerSkills)
            {
                _writer.Write(data.entityId);
                _writer.Write(data.playerName);
                _writer.Write(data.currentLevel);
                _writer.Write(data.volumesRead ?? "");
            }
        }

        public override void ProcessPackage(World _world, GameManager _callbacks)
        {
            if (_world == null) return;

            SkillDataCache.Store(bookSeriesId, (SkillType)skillType, maxLevel, displayName, playerSkills);
        }

        public override NetPackageDirection PackageDirection => NetPackageDirection.ToClient;

        public override int GetLength()
        {
            int len = (bookSeriesId != null ? bookSeriesId.Length : 0) + 1 + 4 +
                      (displayName != null ? displayName.Length : 0) + 4;
            if (playerSkills != null)
            {
                foreach (var p in playerSkills)
                {
                    len += 4 + (p.playerName != null ? p.playerName.Length : 0) + 4;
                }
            }
            return len;
        }
    }
}
