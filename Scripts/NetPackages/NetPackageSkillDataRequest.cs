using System.IO;

namespace PartyStatViewer.NetPackages
{
    public class NetPackageSkillDataRequest : NetPackage
    {
        private int requestingEntityId;
        private string bookSeriesId;
        private byte skillType;
        private int maxLevel;

        public NetPackageSkillDataRequest Setup(int entityId, string seriesId, SkillType type, int max)
        {
            this.requestingEntityId = entityId;
            this.bookSeriesId = seriesId;
            this.skillType = (byte)type;
            this.maxLevel = max;
            return this;
        }

        public override void read(PooledBinaryReader _reader)
        {
            requestingEntityId = _reader.ReadInt32();
            bookSeriesId = _reader.ReadString();
            skillType = _reader.ReadByte();
            maxLevel = _reader.ReadInt32();
        }

        public override void write(PooledBinaryWriter _writer)
        {
            base.write(_writer);
            _writer.Write(requestingEntityId);
            _writer.Write(bookSeriesId);
            _writer.Write(skillType);
            _writer.Write(maxLevel);
        }

        public override void ProcessPackage(World _world, GameManager _callbacks)
        {
            if (_world == null) return;
            if (!SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer) return;

            SkillDataManager.HandleSkillDataRequest(
                requestingEntityId,
                bookSeriesId,
                (SkillType)skillType,
                maxLevel);
        }

        public override int GetLength()
        {
            return 4 + (bookSeriesId != null ? bookSeriesId.Length : 0) + 1 + 4;
        }

        public override NetPackageDirection PackageDirection => NetPackageDirection.ToServer;
    }
}
