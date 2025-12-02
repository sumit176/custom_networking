using CustomNetworking.Serialization;

namespace CustomNetworking.Protocol
{
    public class HeartbeatMessage : NetworkMessage
    {
        public override MessageType Type => MessageType.Heartbeat;

        public uint Timestamp { get; set; }

        public override void Serialize(PacketWriter writer)
        {
            writer.WriteUInt(Timestamp);
        }

        public override void Deserialize(PacketReader reader)
        {
            Timestamp = reader.ReadUInt();
        }
    }
}

