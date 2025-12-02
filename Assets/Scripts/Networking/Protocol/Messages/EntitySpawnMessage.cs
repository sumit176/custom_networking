using CustomNetworking.Serialization;

namespace CustomNetworking.Protocol
{
    public class EntitySpawnMessage : NetworkMessage
    {
        public override MessageType Type => MessageType.EntitySpawn;

        public uint EntityId { get; set; }
        public EntityType EntityType { get; set; }
        public float PosX { get; set; }
        public float PosY { get; set; }
        public float PosZ { get; set; }
        public float RotY { get; set; }
        public uint OwnerId { get; set; }
        public string PlayerName { get; set; }

        public override void Serialize(PacketWriter writer)
        {
            writer.WriteUInt(EntityId);
            writer.WriteByte((byte)EntityType);
            writer.WriteFloat(PosX);
            writer.WriteFloat(PosY);
            writer.WriteFloat(PosZ);
            writer.WriteFloat(RotY);
            writer.WriteUInt(OwnerId);
            writer.WriteString(PlayerName ?? "");
        }

        public override void Deserialize(PacketReader reader)
        {
            EntityId = reader.ReadUInt();
            EntityType = (EntityType)reader.ReadByte();
            PosX = reader.ReadFloat();
            PosY = reader.ReadFloat();
            PosZ = reader.ReadFloat();
            RotY = reader.ReadFloat();
            OwnerId = reader.ReadUInt();
            PlayerName = reader.ReadString();
        }
    }
}

