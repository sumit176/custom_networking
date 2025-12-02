using CustomNetworking.Serialization;

namespace CustomNetworking.Protocol
{
    public class ProjectileSpawnMessage : NetworkMessage
    {
        public override MessageType Type => MessageType.ProjectileSpawn;

        public uint ProjectileId { get; set; }
        public uint OwnerId { get; set; }
        public float PosX { get; set; }
        public float PosY { get; set; }
        public float PosZ { get; set; }
        public float VelX { get; set; }
        public float VelY { get; set; }
        public float VelZ { get; set; }

        public override void Serialize(PacketWriter writer)
        {
            writer.WriteUInt(ProjectileId);
            writer.WriteUInt(OwnerId);
            writer.WriteFloat(PosX);
            writer.WriteFloat(PosY);
            writer.WriteFloat(PosZ);
            writer.WriteFloat(VelX);
            writer.WriteFloat(VelY);
            writer.WriteFloat(VelZ);
        }

        public override void Deserialize(PacketReader reader)
        {
            ProjectileId = reader.ReadUInt();
            OwnerId = reader.ReadUInt();
            PosX = reader.ReadFloat();
            PosY = reader.ReadFloat();
            PosZ = reader.ReadFloat();
            VelX = reader.ReadFloat();
            VelY = reader.ReadFloat();
            VelZ = reader.ReadFloat();
        }
    }
}

