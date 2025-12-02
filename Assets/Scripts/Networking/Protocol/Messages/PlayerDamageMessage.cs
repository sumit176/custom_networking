using CustomNetworking.Serialization;

namespace CustomNetworking.Protocol
{
    public class PlayerDamageMessage : NetworkMessage
    {
        public override MessageType Type => MessageType.PlayerDamage;

        public uint TargetId { get; set; }
        public uint SourceId { get; set; }
        public byte Damage { get; set; }
        public byte NewHealth { get; set; }

        public override void Serialize(PacketWriter writer)
        {
            writer.WriteUInt(TargetId);
            writer.WriteUInt(SourceId);
            writer.WriteByte(Damage);
            writer.WriteByte(NewHealth);
        }

        public override void Deserialize(PacketReader reader)
        {
            TargetId = reader.ReadUInt();
            SourceId = reader.ReadUInt();
            Damage = reader.ReadByte();
            NewHealth = reader.ReadByte();
        }
    }
}

