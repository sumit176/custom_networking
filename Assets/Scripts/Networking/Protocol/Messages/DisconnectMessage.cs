using CustomNetworking.Serialization;

namespace CustomNetworking.Protocol
{
    public class DisconnectMessage : NetworkMessage
    {
        public override MessageType Type => MessageType.Disconnect;

        public string Reason { get; set; }

        public override void Serialize(PacketWriter writer)
        {
            writer.WriteString(Reason ?? "");
        }

        public override void Deserialize(PacketReader reader)
        {
            Reason = reader.ReadString();
        }
    }
}

