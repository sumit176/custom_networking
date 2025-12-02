using CustomNetworking.Serialization;

namespace CustomNetworking.Protocol
{
    public class ConnectRejectMessage : NetworkMessage
    {
        public override MessageType Type => MessageType.ConnectReject;

        public string Reason { get; set; }

        public override void Serialize(PacketWriter writer)
        {
            writer.WriteString(Reason);
        }

        public override void Deserialize(PacketReader reader)
        {
            Reason = reader.ReadString();
        }
    }
}

