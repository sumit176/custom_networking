using CustomNetworking.Serialization;

namespace CustomNetworking.Protocol
{
    /// <summary>
    /// Base class for all network messages
    /// </summary>
    public abstract class NetworkMessage
    {
        public abstract MessageType Type { get; }

        /// <summary>
        /// Serialize message to packet writer
        /// </summary>
        public abstract void Serialize(PacketWriter writer);

        /// <summary>
        /// Deserialize message from packet reader
        /// </summary>
        public abstract void Deserialize(PacketReader reader);

        /// <summary>
        /// Helper to create message payload
        /// </summary>
        public byte[] ToBytes()
        {
            PacketWriter writer = new PacketWriter();
            Serialize(writer);
            return writer.ToArray();
        }

        /// <summary>
        /// Factory method to create message from type and payload
        /// </summary>
        public static NetworkMessage FromBytes(MessageType type, byte[] payload)
        {
            NetworkMessage message = CreateMessage(type);
            if (message == null)
                return null;

            PacketReader reader = new PacketReader(payload, 0, payload.Length);
            message.Deserialize(reader);
            return message;
        }

        private static NetworkMessage CreateMessage(MessageType type)
        {
            switch (type)
            {
                case MessageType.ConnectRequest: return new ConnectRequestMessage();
                case MessageType.ConnectAccept: return new ConnectAcceptMessage();
                case MessageType.ConnectReject: return new ConnectRejectMessage();
                case MessageType.Disconnect: return new DisconnectMessage();
                case MessageType.Heartbeat: return new HeartbeatMessage();
                case MessageType.PlayerInput: return new PlayerInputMessage();
                case MessageType.Snapshot: return new SnapshotMessage();
                case MessageType.DeltaUpdate: return new DeltaUpdateMessage();
                case MessageType.EntitySpawn: return new EntitySpawnMessage();
                case MessageType.EntityDespawn: return new EntityDespawnMessage();
                case MessageType.PlayerDamage: return new PlayerDamageMessage();
                case MessageType.PlayerDeath: return new PlayerDeathMessage();
                case MessageType.PlayerRespawn: return new PlayerRespawnMessage();
                case MessageType.ProjectileSpawn: return new ProjectileSpawnMessage();
                default: return null;
            }
        }
    }
}

