using CustomNetworking.Serialization;

namespace CustomNetworking.Protocol
{
    public class PlayerInputMessage : NetworkMessage
    {
        public override MessageType Type => MessageType.PlayerInput;

        public uint InputSequence { get; set; }
        public uint ServerTick { get; set; }
        public float MoveX { get; set; }
        public float MoveY { get; set; }
        public float AimX { get; set; }
        public float AimY { get; set; }
        public bool Shoot { get; set; }

        public override void Serialize(PacketWriter writer)
        {
            writer.WriteUInt(InputSequence);
            writer.WriteUInt(ServerTick);
            writer.WriteFloat(MoveX);
            writer.WriteFloat(MoveY);
            writer.WriteFloat(AimX);
            writer.WriteFloat(AimY);
            writer.WriteBool(Shoot);
        }

        public override void Deserialize(PacketReader reader)
        {
            InputSequence = reader.ReadUInt();
            ServerTick = reader.ReadUInt();
            MoveX = reader.ReadFloat();
            MoveY = reader.ReadFloat();
            AimX = reader.ReadFloat();
            AimY = reader.ReadFloat();
            Shoot = reader.ReadBool();
        }
    }
}

