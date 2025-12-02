using CustomNetworking.Serialization;

namespace CustomNetworking.Protocol
{
    public class EntityDespawnMessage : NetworkMessage
    {
        public override MessageType Type => MessageType.EntityDespawn;

        public uint EntityId { get; set; }

        public override void Serialize(PacketWriter writer)
        {
            writer.WriteUInt(EntityId);
        }

        public override void Deserialize(PacketReader reader)
        {
            EntityId = reader.ReadUInt();
        }
    }
}

