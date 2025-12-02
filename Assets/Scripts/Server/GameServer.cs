using System.Collections.Generic;
using System.Net;
using UnityEngine;
using CustomNetworking.Core;
using CustomNetworking.Protocol;
using CustomNetworking.Serialization;

namespace CustomNetworking.Server
{
    /// <summary>
    /// Main game server - handles connections, game loop, and state broadcasting
    /// </summary>
    public class GameServer : MonoBehaviour
    {
        [Header("Server Settings")]
        [SerializeField] private int port = 7777;
        [SerializeField] private int maxPlayers = 20;
        [SerializeField] private int tickRate = 20; // 20 Hz
        [SerializeField] private int snapshotInterval = 3; // Send snapshot every 3 ticks
        
        private UdpSocketManager socketManager;
        private Dictionary<IPEndPoint, NetworkConnection> connections;
        private Dictionary<uint, ServerPlayer> players;
        private ServerGameState gameState;
        private ServerGameLogic gameLogic;
        
        private uint nextClientId = 1;
        private uint serverTick = 0;
        private float tickTimer = 0;
        private float tickDuration;
        
        private bool isRunning = false;
        private System.Random random;
        
        // World setup
        private bool worldInitialized = false;
        
        void Start()
        {
            random = new System.Random();
            tickDuration = 1.0f / tickRate;
            
            connections = new Dictionary<IPEndPoint, NetworkConnection>();
            players = new Dictionary<uint, ServerPlayer>();
            gameState = new ServerGameState();
            gameLogic = new ServerGameLogic();
            
            socketManager = new UdpSocketManager();
        }
        
        void Update()
        {
            if (!isRunning)
                return;
            
            // Process incoming packets
            ProcessIncomingPackets();
            
            // Fixed tick update
            tickTimer += Time.deltaTime;
            while (tickTimer >= tickDuration)
            {
                tickTimer -= tickDuration;
                ServerTick();
            }
            
            // Process outgoing packets
            socketManager.ProcessSendQueue();
        }
        
        void OnDestroy()
        {
            StopServer();
        }
        
        public void StartServer()
        {
            if (isRunning)
                return;
            
            if (socketManager.Start(port))
            {
                isRunning = true;
                Debug.Log($"Server started on port {port}");
                
                // Initialize world
                InitializeWorld();
            }
            else
            {
                Debug.LogError("Failed to start server");
            }
        }
        
        public void StopServer()
        {
            if (!isRunning)
                return;
            
            isRunning = false;
            socketManager.Stop();
            
            connections.Clear();
            players.Clear();
            gameState.Clear();
            
            Debug.Log("Server stopped");
        }
        
        private void InitializeWorld()
        {
            if (worldInitialized)
                return;
            
            // Create boundary walls
            gameState.SpawnWall(new Vector3(0, 0, 25), new Vector3(50, 5, 1)); // North
            gameState.SpawnWall(new Vector3(0, 0, -25), new Vector3(50, 5, 1)); // South
            gameState.SpawnWall(new Vector3(25, 0, 0), new Vector3(1, 5, 50)); // East
            gameState.SpawnWall(new Vector3(-25, 0, 0), new Vector3(1, 5, 50)); // West
            
            // Create some obstacles
            gameState.SpawnWall(new Vector3(10, 0, 10), new Vector3(5, 5, 1));
            gameState.SpawnWall(new Vector3(-10, 0, 10), new Vector3(1, 5, 5));
            gameState.SpawnWall(new Vector3(10, 0, -10), new Vector3(1, 5, 5));
            gameState.SpawnWall(new Vector3(-10, 0, -10), new Vector3(5, 5, 1));
            gameState.SpawnWall(new Vector3(0, 0, 0), new Vector3(3, 5, 3));
            
            worldInitialized = true;
            Debug.Log($"SERVER: World initialized with boundary and obstacle walls");
        }
        
        /// <summary>
        /// Server tick - process inputs, update game state, send updates
        /// </summary>
        private void ServerTick()
        {
            serverTick++;
            
            // Update connections
            foreach (var connection in connections.Values)
            {
                connection.Update(Time.time);
            }
            
            // Remove disconnected players
            List<uint> disconnectedPlayers = new List<uint>();
            foreach (var player in players.Values)
            {
                if (!player.Connection.IsConnected)
                {
                    disconnectedPlayers.Add(player.ClientId);
                }
            }
            
            foreach (uint clientId in disconnectedPlayers)
            {
                DisconnectPlayer(clientId);
            }
            
            // Update respawn timers
            foreach (var player in players.Values)
            {
                player.UpdateRespawn(tickDuration);
                
                // Check if ready to respawn
                if (!player.IsAlive && player.RespawnTimer <= 0)
                {
                    RespawnPlayer(player);
                }
            }
            
            // Process player inputs
            foreach (var player in players.Values)
            {
                if (player.IsAlive)
                {
                    PlayerInputMessage input = player.GetNextInput();
                    if (input != null)
                    {
                        ProcessPlayerInput(player, input);
                    }
                }
            }
            
            // Update game logic
            gameLogic.UpdateCooldowns(tickDuration);
            
            // Update entity physics (movement, lifetime)
            List<uint> expiredEntities = gameState.UpdateEntitiesPhysics(tickDuration);
            foreach (uint entityId in expiredEntities)
            {
                gameState.DespawnEntity(entityId);
                BroadcastMessage(new EntityDespawnMessage { EntityId = entityId });
            }
            
            // Process collisions (applies damage)
            List<GameEvent> events = gameLogic.ProcessProjectileCollisions(gameState);
            foreach (var evt in events)
            {
                HandleGameEvent(evt);
            }
            
            // Send state updates (uses current state vs previous state)
            if (serverTick % snapshotInterval == 0)
            {
                SendSnapshot();
            }
            else
            {
                SendDeltaUpdate();
            }
            
            // Update "previous" state for next tick's delta calculation
            gameState.UpdatePreviousState();
        }
        
        private void ProcessIncomingPackets()
        {
            while (socketManager.TryReceive(out UdpSocketManager.ReceivedPacket receivedPacket))
            {
                Packet packet = Packet.Deserialize(receivedPacket.Data, 0, receivedPacket.Length);
                
                if (packet == null)
                    continue;
                
                // Get or create connection
                NetworkConnection connection;
                if (!connections.TryGetValue(receivedPacket.RemoteEndPoint, out connection))
                {
                    connection = new NetworkConnection(receivedPacket.RemoteEndPoint);
                    connections[receivedPacket.RemoteEndPoint] = connection;
                }
                
                float currentTime = Time.time;
                connection.OnPacketReceived(currentTime);
                
                // Process ACK and update RTT
                float? sendTime = connection.Reliable.ProcessAck(packet.Ack, packet.AckBitfield, currentTime);
                if (sendTime.HasValue)
                {
                    connection.UpdateRtt(sendTime.Value, currentTime);
                }
                
                // Deserialize message
                MessageType messageType = (MessageType)packet.PacketType;
                NetworkMessage message = NetworkMessage.FromBytes(messageType, packet.Payload);
                
                if (message != null)
                {
                    HandleMessage(connection, message);
                }
            }
        }
        
        private void HandleMessage(NetworkConnection connection, NetworkMessage message)
        {
            switch (message.Type)
            {
                case MessageType.ConnectRequest:
                    HandleConnectRequest(connection, message as ConnectRequestMessage);
                    break;
                
                case MessageType.Disconnect:
                    HandleDisconnect(connection, message as DisconnectMessage);
                    break;
                
                case MessageType.PlayerInput:
                    HandlePlayerInput(connection, message as PlayerInputMessage);
                    break;
            }
        }
        
        private void HandleConnectRequest(NetworkConnection connection, ConnectRequestMessage message)
        {
            // Check if already connected
            foreach (var serverPlayer in players.Values)
            {
                if (serverPlayer.Connection.RemoteEndPoint.Equals(connection.RemoteEndPoint))
                {
                    return; // Already connected
                }
            }
            
            // Check max players
            if (players.Count >= maxPlayers)
            {
                SendMessage(connection, new ConnectRejectMessage { Reason = "Server full" });
                return;
            }
            
            // Accept connection
            uint clientId = nextClientId++;
            ServerPlayer player = new ServerPlayer(clientId, message.PlayerName, connection);
            players[clientId] = player;
            
            Debug.Log($"Player connected: {message.PlayerName} (ID: {clientId})");
            
            // Spawn player tank
            Vector3 spawnPos = ServerPhysics.GetSafeSpawnPosition(gameState, random);
            uint entityId = gameState.SpawnTank(message.PlayerName, spawnPos);
            player.EntityId = entityId;
            player.Respawn();
            
            // Send accept message with player's entity ID
            SendMessage(connection, new ConnectAcceptMessage
            {
                ClientId = clientId,
                ServerTick = serverTick,
                PlayerEntityId = entityId
            });
            
            // Send full world state to new player
            SendSnapshotToPlayer(player);
            
            // Notify other players about new tank
            BroadcastMessage(new EntitySpawnMessage
            {
                EntityId = entityId,
                EntityType = EntityType.Tank,
                PosX = spawnPos.x,
                PosY = spawnPos.y,
                PosZ = spawnPos.z,
                RotY = 0,
                OwnerId = entityId,
                PlayerName = message.PlayerName
            }, clientId);
        }
        
        private void HandleDisconnect(NetworkConnection connection, DisconnectMessage message)
        {
            // Find player by connection
            uint clientIdToRemove = 0;
            foreach (var player in players.Values)
            {
                if (player.Connection.RemoteEndPoint.Equals(connection.RemoteEndPoint))
                {
                    clientIdToRemove = player.ClientId;
                    break;
                }
            }
            
            if (clientIdToRemove != 0)
            {
                DisconnectPlayer(clientIdToRemove);
            }
        }
        
        private void DisconnectPlayer(uint clientId)
        {
            if (!players.TryGetValue(clientId, out ServerPlayer player))
                return;
            
            Debug.Log($"Player disconnected: {player.PlayerName} (ID: {clientId})");
            
            // Despawn player entity
            gameState.DespawnEntity(player.EntityId);
            BroadcastMessage(new EntityDespawnMessage { EntityId = player.EntityId });
            
            // Remove player
            players.Remove(clientId);
            connections.Remove(player.Connection.RemoteEndPoint);
        }
        
        private void HandlePlayerInput(NetworkConnection connection, PlayerInputMessage message)
        {
            // Find player by connection
            ServerPlayer player = null;
            foreach (var p in players.Values)
            {
                if (p.Connection.RemoteEndPoint.Equals(connection.RemoteEndPoint))
                {
                    player = p;
                    break;
                }
            }
            
            if (player != null && player.IsAlive)
            {
                player.AddInput(message);
            }
        }
        
        private void ProcessPlayerInput(ServerPlayer player, PlayerInputMessage input)
        {
            ServerEntity tank = gameState.GetEntity(player.EntityId);
            if (tank == null)
                return;
            
            // Move tank
            ServerPhysics.MoveTank(tank, new Vector2(input.MoveX, input.MoveY), tickDuration, gameState);
            
            // Rotate tank
            Vector2 aimInput = new Vector2(input.AimX, input.AimY);
            if (input.Shoot)
            {
                Debug.Log($"SERVER RECEIVED AimInput: ({aimInput.x:F2}, {aimInput.y:F2}) from player {player.PlayerName}");
            }
            ServerPhysics.RotateTank(tank, aimInput);
            
            // Shoot
            if (input.Shoot)
            {
                uint projectileId = gameLogic.TryShoot(tank, gameState);
                if (projectileId != 0)
                {
                    ServerEntity projectile = gameState.GetEntity(projectileId);
                    if (projectile != null)
                    {
                        var msg = new ProjectileSpawnMessage
                        {
                            ProjectileId = projectileId,
                            OwnerId = tank.EntityId,
                            PosX = projectile.Position.x,
                            PosY = projectile.Position.y,
                            PosZ = projectile.Position.z,
                            VelX = projectile.Velocity.x,
                            VelY = projectile.Velocity.y,
                            VelZ = projectile.Velocity.z
                        };
                        // Debug.Log($"SERVER SEND: Pos=({msg.PosX:F2}, {msg.PosY:F2}, {msg.PosZ:F2}), Vel=({msg.VelX:F2}, {msg.VelY:F2}, {msg.VelZ:F2})");
                        BroadcastMessage(msg);
                    }
                }
            }
        }
        
        private void HandleGameEvent(GameEvent evt)
        {
            if (evt is DamageEvent damageEvt)
            {
                BroadcastMessage(new PlayerDamageMessage
                {
                    TargetId = damageEvt.TargetId,
                    SourceId = damageEvt.SourceId,
                    Damage = damageEvt.Damage,
                    NewHealth = damageEvt.NewHealth
                });
            }
            else if (evt is DeathEvent deathEvt)
            {
                BroadcastMessage(new PlayerDeathMessage
                {
                    PlayerId = deathEvt.PlayerId,
                    KillerId = deathEvt.KillerId
                });
                
                // Find player and mark as dead
                foreach (var player in players.Values)
                {
                    if (player.EntityId == deathEvt.PlayerId)
                    {
                        player.Die();
                        
                        // Award kill
                        foreach (var killer in players.Values)
                        {
                            if (killer.EntityId == deathEvt.KillerId)
                            {
                                killer.Kills++;
                                break;
                            }
                        }
                        break;
                    }
                }
            }
            else if (evt is DespawnEvent despawnEvt)
            {
                // Already handled
            }
        }
        
        private void RespawnPlayer(ServerPlayer player)
        {
            Vector3 spawnPos = ServerPhysics.GetSafeSpawnPosition(gameState, random);
            
            ServerEntity tank = gameState.GetEntity(player.EntityId);
            if (tank != null)
            {
                tank.Position = spawnPos;
                tank.Health = 100;
                tank.Rotation = 0;
                player.Respawn();
                
                BroadcastMessage(new PlayerRespawnMessage
                {
                    PlayerId = player.EntityId,
                    PosX = spawnPos.x,
                    PosY = spawnPos.y,
                    PosZ = spawnPos.z
                });
            }
        }
        
        private void SendSnapshot()
        {
            SnapshotMessage snapshot = new SnapshotMessage
            {
                ServerTick = serverTick
            };
            
            foreach (var entity in gameState.GetAllEntities())
            {
                // Skip static entities - they don't change and were sent on initial connection
                if (entity.Type == EntityType.Wall)
                    continue;
                    
                snapshot.Entities.Add(entity.ToEntityState());
            }
            
            BroadcastMessage(snapshot);
        }
        
        private void SendSnapshotToPlayer(ServerPlayer player)
        {
            SnapshotMessage snapshot = new SnapshotMessage
            {
                ServerTick = serverTick
            };
            
            foreach (var entity in gameState.GetAllEntities())
            {
                snapshot.Entities.Add(entity.ToEntityState());
            }
            
            SendMessage(player.Connection, snapshot);
        }
        
        private void SendDeltaUpdate()
        {
            List<ServerEntity> changedEntities = gameState.GetChangedEntities();
            
            if (changedEntities.Count == 0)
                return;
            
            DeltaUpdateMessage delta = new DeltaUpdateMessage
            {
                ServerTick = serverTick,
                BaselineTick = serverTick - 1
            };
            
            foreach (var entity in changedEntities)
            {
                // Skip static entities
                if (entity.Type == EntityType.Wall)
                    continue;
                    
                delta.Deltas.Add(entity.ToDelta());
            }
            
            BroadcastMessage(delta);
        }
        
        private void SendMessage(NetworkConnection connection, NetworkMessage message)
        {
            // Determine if this message type should use unreliable channel
            // Snapshots and DeltaUpdates don't need reliability as newer data supersedes old
            bool useUnreliable = message.Type == MessageType.Snapshot || 
                                 message.Type == MessageType.DeltaUpdate;
            
            Packet packet = new Packet
            {
                Sequence = useUnreliable ? 
                    connection.Unreliable.GetNextSequence() : 
                    connection.Reliable.GetNextSequence(),
                Ack = connection.Reliable.Ack,
                AckBitfield = connection.Reliable.GetAckBitfield(),
                PacketType = (byte)message.Type,
                Payload = message.ToBytes()
            };
            
            byte[] data = packet.Serialize();
            socketManager.Send(data, data.Length, connection.RemoteEndPoint);
            
            // Only track for reliability if using reliable channel
            if (!useUnreliable)
            {
                connection.Reliable.AddPendingPacket(packet.Sequence, data, Time.time);
            }
        }
        
        private void BroadcastMessage(NetworkMessage message, uint excludeClientId = 0)
        {
            foreach (var player in players.Values)
            {
                if (excludeClientId != 0 && player.ClientId == excludeClientId)
                    continue;
                
                SendMessage(player.Connection, message);
            }
        }
        
        public string GetStatus()
        {
            if (!isRunning)
                return "Server: Stopped";
            
            return $"Server: Running | Port: {port} | Players: {players.Count}/{maxPlayers} | Tick: {serverTick}";
        }
    }
}

