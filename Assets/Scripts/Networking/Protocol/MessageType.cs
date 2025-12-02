namespace CustomNetworking.Protocol
{
    /// <summary>
    /// All network message types
    /// </summary>
    public enum MessageType : byte
    {
        // Connection management
        ConnectRequest = 0,
        ConnectAccept = 1,
        ConnectReject = 2,
        Disconnect = 3,
        Heartbeat = 4,

        // Input
        PlayerInput = 10,

        // State synchronization
        Snapshot = 20,
        DeltaUpdate = 21,

        // Entity lifecycle
        EntitySpawn = 30,
        EntityDespawn = 31,

        // Game events
        PlayerDamage = 40,
        PlayerDeath = 41,
        PlayerRespawn = 42,
        ProjectileSpawn = 43,
    }
}

