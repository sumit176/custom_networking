using System.Collections.Generic;
using CustomNetworking.Serialization;

namespace CustomNetworking.Protocol
{
    /// <summary>
    /// Full world state snapshot
    /// </summary>
    public class SnapshotMessage : NetworkMessage
    {
        public override MessageType Type => MessageType.Snapshot;

        public uint ServerTick { get; set; }
        public List<EntityState> Entities { get; set; }

        public SnapshotMessage()
        {
            Entities = new List<EntityState>();
        }

        public override void Serialize(PacketWriter writer)
        {
            writer.WriteUInt(ServerTick);
            writer.WriteUShort((ushort)Entities.Count);
            
            foreach (var entity in Entities)
            {
                entity.Serialize(writer);
            }
        }

        public override void Deserialize(PacketReader reader)
        {
            ServerTick = reader.ReadUInt();
            ushort count = reader.ReadUShort();
            
            Entities.Clear();
            for (int i = 0; i < count; i++)
            {
                EntityState entity = new EntityState();
                entity.Deserialize(reader);
                Entities.Add(entity);
            }
        }
    }

    /// <summary>
    /// Entity state data for snapshots
    /// </summary>
    public class EntityState
    {
        public uint EntityId { get; set; }
        public EntityType Type { get; set; }
        public float PosX { get; set; }
        public float PosY { get; set; }
        public float PosZ { get; set; }
        public float RotY { get; set; } // Rotation around Y axis (for top-down)
        public byte Health { get; set; }
        public uint OwnerId { get; set; } // For projectiles, walls = 0
        public string PlayerName { get; set; } // For tanks only
        public float ScaleX { get; set; } // For walls
        public float ScaleY { get; set; } // For walls
        public float ScaleZ { get; set; } // For walls

        public void Serialize(PacketWriter writer)
        {
            writer.WriteUInt(EntityId);
            writer.WriteByte((byte)Type);
            writer.WriteFloat(PosX);
            writer.WriteFloat(PosY);
            writer.WriteFloat(PosZ);
            writer.WriteFloat(RotY);
            writer.WriteByte(Health);
            writer.WriteUInt(OwnerId);
            writer.WriteString(PlayerName ?? "");
            writer.WriteFloat(ScaleX);
            writer.WriteFloat(ScaleY);
            writer.WriteFloat(ScaleZ);
        }

        public void Deserialize(PacketReader reader)
        {
            EntityId = reader.ReadUInt();
            Type = (EntityType)reader.ReadByte();
            PosX = reader.ReadFloat();
            PosY = reader.ReadFloat();
            PosZ = reader.ReadFloat();
            RotY = reader.ReadFloat();
            Health = reader.ReadByte();
            OwnerId = reader.ReadUInt();
            PlayerName = reader.ReadString();
            ScaleX = reader.ReadFloat();
            ScaleY = reader.ReadFloat();
            ScaleZ = reader.ReadFloat();
        }
    }

    public enum EntityType : byte
    {
        Tank = 0,
        Projectile = 1,
        Wall = 2
    }
}

