using CustomNetworking.Serialization;

namespace CustomNetworking.Protocol
{
    public class ConnectAcceptMessage : NetworkMessage
    {
        public override MessageType Type => MessageType.ConnectAccept;

        public uint ClientId { get; set; }
        public uint ServerTick { get; set; }
        public uint PlayerEntityId { get; set; }

        public override void Serialize(PacketWriter writer)
        {
            writer.WriteUInt(ClientId);
            writer.WriteUInt(ServerTick);
            writer.WriteUInt(PlayerEntityId);
        }

        public override void Deserialize(PacketReader reader)
        {
            ClientId = reader.ReadUInt();
            ServerTick = reader.ReadUInt();
            PlayerEntityId = reader.ReadUInt();
        }
    }
}

