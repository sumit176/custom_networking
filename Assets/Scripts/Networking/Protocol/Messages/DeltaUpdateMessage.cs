using System.Collections.Generic;
using CustomNetworking.Serialization;

namespace CustomNetworking.Protocol
{
    /// <summary>
    /// Delta update containing only changed entities since last snapshot
    /// </summary>
    public class DeltaUpdateMessage : NetworkMessage
    {
        public override MessageType Type => MessageType.DeltaUpdate;

        public uint ServerTick { get; set; }
        public uint BaselineTick { get; set; } // Tick this delta is relative to
        public List<EntityDelta> Deltas { get; set; }

        public DeltaUpdateMessage()
        {
            Deltas = new List<EntityDelta>();
        }

        public override void Serialize(PacketWriter writer)
        {
            writer.WriteUInt(ServerTick);
            writer.WriteUInt(BaselineTick);
            writer.WriteUShort((ushort)Deltas.Count);
            
            foreach (var delta in Deltas)
            {
                delta.Serialize(writer);
            }
        }

        public override void Deserialize(PacketReader reader)
        {
            ServerTick = reader.ReadUInt();
            BaselineTick = reader.ReadUInt();
            ushort count = reader.ReadUShort();
            
            Deltas.Clear();
            for (int i = 0; i < count; i++)
            {
                EntityDelta delta = new EntityDelta();
                delta.Deserialize(reader);
                Deltas.Add(delta);
            }
        }
    }

    /// <summary>
    /// Delta for a single entity (only includes changed fields)
    /// </summary>
    public class EntityDelta
    {
        public uint EntityId { get; set; }
        public byte ChangeFlags { get; set; } // Bitmask of changed fields
        
        // Optional fields (only sent if changed)
        public float? PosX { get; set; }
        public float? PosY { get; set; }
        public float? PosZ { get; set; }
        public float? RotY { get; set; }
        public byte? Health { get; set; }

        // Change flags
        private const byte FLAG_POS_X = 1 << 0;
        private const byte FLAG_POS_Y = 1 << 1;
        private const byte FLAG_POS_Z = 1 << 2;
        private const byte FLAG_ROT_Y = 1 << 3;
        private const byte FLAG_HEALTH = 1 << 4;

        public void Serialize(PacketWriter writer)
        {
            writer.WriteUInt(EntityId);
            
            // Build change flags
            byte flags = 0;
            if (PosX.HasValue) flags |= FLAG_POS_X;
            if (PosY.HasValue) flags |= FLAG_POS_Y;
            if (PosZ.HasValue) flags |= FLAG_POS_Z;
            if (RotY.HasValue) flags |= FLAG_ROT_Y;
            if (Health.HasValue) flags |= FLAG_HEALTH;
            
            writer.WriteByte(flags);
            
            // Write changed values
            if (PosX.HasValue) writer.WriteFloat(PosX.Value);
            if (PosY.HasValue) writer.WriteFloat(PosY.Value);
            if (PosZ.HasValue) writer.WriteFloat(PosZ.Value);
            if (RotY.HasValue) writer.WriteFloat(RotY.Value);
            if (Health.HasValue) writer.WriteByte(Health.Value);
        }

        public void Deserialize(PacketReader reader)
        {
            EntityId = reader.ReadUInt();
            ChangeFlags = reader.ReadByte();
            
            if ((ChangeFlags & FLAG_POS_X) != 0) PosX = reader.ReadFloat();
            if ((ChangeFlags & FLAG_POS_Y) != 0) PosY = reader.ReadFloat();
            if ((ChangeFlags & FLAG_POS_Z) != 0) PosZ = reader.ReadFloat();
            if ((ChangeFlags & FLAG_ROT_Y) != 0) RotY = reader.ReadFloat();
            if ((ChangeFlags & FLAG_HEALTH) != 0) Health = reader.ReadByte();
        }
    }
}

