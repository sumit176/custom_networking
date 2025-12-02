using CustomNetworking.Serialization;

namespace CustomNetworking.Protocol
{
    public class ConnectRequestMessage : NetworkMessage
    {
        public override MessageType Type => MessageType.ConnectRequest;

        public string PlayerName { get; set; }
        public uint ProtocolVersion { get; set; }

        public ConnectRequestMessage()
        {
            ProtocolVersion = 1;
        }

        public override void Serialize(PacketWriter writer)
        {
            writer.WriteUInt(ProtocolVersion);
            writer.WriteString(PlayerName);
        }

        public override void Deserialize(PacketReader reader)
        {
            ProtocolVersion = reader.ReadUInt();
            PlayerName = reader.ReadString();
        }
    }
}

