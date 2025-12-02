using CustomNetworking.Serialization;

namespace CustomNetworking.Protocol
{
    public class PlayerDeathMessage : NetworkMessage
    {
        public override MessageType Type => MessageType.PlayerDeath;

        public uint PlayerId { get; set; }
        public uint KillerId { get; set; }

        public override void Serialize(PacketWriter writer)
        {
            writer.WriteUInt(PlayerId);
            writer.WriteUInt(KillerId);
        }

        public override void Deserialize(PacketReader reader)
        {
            PlayerId = reader.ReadUInt();
            KillerId = reader.ReadUInt();
        }
    }
}

