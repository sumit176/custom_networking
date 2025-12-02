using CustomNetworking.Serialization;

namespace CustomNetworking.Protocol
{
    public class PlayerRespawnMessage : NetworkMessage
    {
        public override MessageType Type => MessageType.PlayerRespawn;

        public uint PlayerId { get; set; }
        public float PosX { get; set; }
        public float PosY { get; set; }
        public float PosZ { get; set; }

        public override void Serialize(PacketWriter writer)
        {
            writer.WriteUInt(PlayerId);
            writer.WriteFloat(PosX);
            writer.WriteFloat(PosY);
            writer.WriteFloat(PosZ);
        }

        public override void Deserialize(PacketReader reader)
        {
            PlayerId = reader.ReadUInt();
            PosX = reader.ReadFloat();
            PosY = reader.ReadFloat();
            PosZ = reader.ReadFloat();
        }
    }
}

