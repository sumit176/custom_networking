using System;
using System.Net;
using UnityEngine;
using CustomNetworking.Core;
using CustomNetworking.Protocol;
using CustomNetworking.Serialization;

namespace CustomNetworking.Client
{
    public class GameClient : MonoBehaviour
    {
        public enum ClientState
        {
            Disconnected,
            Connecting,
            Connected,
            InGame
        }
        
        [Header("Connection")]
        [SerializeField] private string serverAddress = "127.0.0.1";
        [SerializeField] private int serverPort = 7777;
        [SerializeField] private string playerName = "Player";
        
        private UdpSocketManager socketManager;
        private NetworkConnection serverConnection;
        private PacketSimulator packetSimulator;
        
        private ClientState state;
        private uint clientId;
        private ClientGameState gameState;
        private ClientPrediction prediction;
        
        private uint inputSequence;
        private float inputSendTimer;
        private const float INPUT_SEND_RATE = 1.0f / 20.0f; // 20Hz
        
        // Input state
        private Vector2 moveInput;
        private Vector2 aimInput;
        private bool shootInput;
        
        // Public properties
        public ClientState State => state;
        public uint ClientId => clientId;
        public float Ping => serverConnection?.Rtt ?? 0;
        public bool IsConnected => state == ClientState.Connected || state == ClientState.InGame;
        
        // Events
        public event Action OnConnected;
        public event Action<string> OnDisconnected;
        public event Action<string> OnConnectionFailed;
        
        void Start()
        {
            gameState = new ClientGameState();
            prediction = new ClientPrediction();
            packetSimulator = new PacketSimulator();
            
            socketManager = new UdpSocketManager();
            state = ClientState.Disconnected;
        }
        
        void Update()
        {
            if (state == ClientState.Disconnected)
                return;
            
            // Update connection
            if (serverConnection != null)
            {
                serverConnection.Update(Time.time);
                
                // Check for timeout
                if (!serverConnection.IsConnected && state != ClientState.Disconnected)
                {
                    Disconnect("Connection timed out");
                    return;
                }
            }
            
            // Process incoming packets
            ProcessIncomingPackets();
            
            // Send input if in game
            if (state == ClientState.InGame)
            {
                inputSendTimer += Time.deltaTime;
                if (inputSendTimer >= INPUT_SEND_RATE)
                {
                    inputSendTimer -= INPUT_SEND_RATE;
                    SendInput();
                }
                
                // Update prediction
                if (prediction != null && gameState.LocalPlayerId != 0)
                {
                    ClientTank localTank = gameState.GetLocalPlayerTank();
                    if (localTank != null)
                    {
                        // Update tank with predicted position
                        localTank.Position = prediction.PredictedPosition;
                        localTank.Rotation = prediction.PredictedRotation;
                        localTank.UpdateGameObject();
                    }
                }
            }
            
            // Update game state
            gameState.Update(Time.deltaTime);
            
            // Process packet simulator delayed packets
            var delayedPackets = packetSimulator.GetDelayedPackets(Time.time);
            foreach (var packet in delayedPackets)
            {
                socketManager.Send(packet.data, packet.length, packet.endPoint);
            }
            
            // Process send queue
            socketManager.ProcessSendQueue();
        }
        
        void OnDestroy()
        {
            Disconnect("Client shutting down");
        }
        
        /// <summary>
        /// Connect to server
        /// </summary>
        public void Connect(string address, int port, string name)
        {
            if (state != ClientState.Disconnected)
                return;
            
            serverAddress = address;
            serverPort = port;
            playerName = name;
            
            if (socketManager.Connect(serverAddress, serverPort))
            {
                IPAddress ipAddress;
                if (!IPAddress.TryParse(serverAddress, out ipAddress))
                {
                    // Try DNS resolution
                    try
                    {
                        IPAddress[] addresses = Dns.GetHostAddresses(serverAddress);
                        if (addresses.Length > 0)
                        {
                            ipAddress = addresses[0];
                        }
                        else
                        {
                            OnConnectionFailed?.Invoke("Could not resolve hostname");
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        OnConnectionFailed?.Invoke($"DNS error: {ex.Message}");
                        return;
                    }
                }
                
                IPEndPoint serverEndPoint = new IPEndPoint(ipAddress, serverPort);
                serverConnection = new NetworkConnection(serverEndPoint);
                
                state = ClientState.Connecting;
                
                // Send connect request
                SendMessage(new ConnectRequestMessage
                {
                    PlayerName = playerName,
                    ProtocolVersion = 1
                });
                
                Debug.Log($"Connecting to {serverAddress}:{serverPort}...");
            }
            else
            {
                OnConnectionFailed?.Invoke("Failed to create socket");
            }
        }
        
        /// <summary>
        /// Disconnect from server
        /// </summary>
        public void Disconnect(string reason = "User disconnect")
        {
            if (state == ClientState.Disconnected)
                return;
            
            if (state == ClientState.Connected || state == ClientState.InGame)
            {
                SendMessage(new DisconnectMessage { Reason = reason });
            }
            
            socketManager.Stop();
            gameState.Clear();
            prediction.Clear();
            
            state = ClientState.Disconnected;
            OnDisconnected?.Invoke(reason);
            
            Debug.Log($"Disconnected: {reason}");
        }
        
        public void SetInput(Vector2 move, Vector2 aim, bool shoot)
        {
            moveInput = move;
            aimInput = aim;
            shootInput = shootInput || shoot;
        }
        
        /// <summary>
        /// Enable/disable packet simulation
        /// </summary>
        public void SetPacketSimulation(bool enabled, float lossPercent, float latencyMs, float jitterMs)
        {
            packetSimulator.Enabled = enabled;
            packetSimulator.PacketLossPercent = lossPercent;
            packetSimulator.LatencyMs = latencyMs;
            packetSimulator.JitterMs = jitterMs;
        }
        
        private void SendInput()
        {
            inputSequence++;
            
            PlayerInputMessage input = new PlayerInputMessage
            {
                InputSequence = inputSequence,
                ServerTick = 0, // Will be filled by server
                MoveX = moveInput.x,
                MoveY = moveInput.y,
                AimX = aimInput.x,
                AimY = aimInput.y,
                Shoot = shootInput
            };
            
            SendMessage(input);
            
            // Apply prediction
            prediction.PredictMovement(inputSequence, moveInput, aimInput, INPUT_SEND_RATE);
            
            // Reset shoot input after sending
            shootInput = false;
        }
        
        private void ProcessIncomingPackets()
        {
            while (socketManager.TryReceive(out UdpSocketManager.ReceivedPacket receivedPacket))
            {
                // Apply packet simulation
                if (!packetSimulator.ProcessIncoming(receivedPacket.Data, receivedPacket.Length))
                {
                    continue; // Packet dropped by simulator
                }
                
                Packet packet = Packet.Deserialize(receivedPacket.Data, 0, receivedPacket.Length);
                
                if (packet == null)
                    continue;
                
                if (serverConnection != null)
                {
                    float currentTime = Time.time;
                    serverConnection.OnPacketReceived(currentTime);
                    serverConnection.Reliable.ProcessReceivedPacket(packet.Sequence);
                    
                    // Process ACK and update RTT
                    float? sendTime = serverConnection.Reliable.ProcessAck(packet.Ack, packet.AckBitfield, currentTime);
                    if (sendTime.HasValue)
                    {
                        serverConnection.UpdateRtt(sendTime.Value, currentTime);
                    }
                }
                
                // Deserialize message
                MessageType messageType = (MessageType)packet.PacketType;
                NetworkMessage message = NetworkMessage.FromBytes(messageType, packet.Payload);
                
                if (message != null)
                {
                    HandleMessage(message);
                }
            }
        }
        
        private void HandleMessage(NetworkMessage message)
        {
            switch (message.Type)
            {
                case MessageType.ConnectAccept:
                    HandleConnectAccept(message as ConnectAcceptMessage);
                    break;
                
                case MessageType.ConnectReject:
                    HandleConnectReject(message as ConnectRejectMessage);
                    break;
                
                case MessageType.Snapshot:
                    HandleSnapshot(message as SnapshotMessage);
                    break;
                
                case MessageType.DeltaUpdate:
                    HandleDeltaUpdate(message as DeltaUpdateMessage);
                    break;
                
                case MessageType.EntitySpawn:
                    HandleEntitySpawn(message as EntitySpawnMessage);
                    break;
                
                case MessageType.EntityDespawn:
                    HandleEntityDespawn(message as EntityDespawnMessage);
                    break;
                
                case MessageType.ProjectileSpawn:
                    HandleProjectileSpawn(message as ProjectileSpawnMessage);
                    break;
                
                case MessageType.PlayerDamage:
                    HandlePlayerDamage(message as PlayerDamageMessage);
                    break;
                
                case MessageType.PlayerDeath:
                    HandlePlayerDeath(message as PlayerDeathMessage);
                    break;
                
                case MessageType.PlayerRespawn:
                    HandlePlayerRespawn(message as PlayerRespawnMessage);
                    break;
            }
        }
        
        private void HandleConnectAccept(ConnectAcceptMessage message)
        {
            clientId = message.ClientId;
            state = ClientState.Connected;
            
            // Set our local player entity ID
            gameState.LocalPlayerId = message.PlayerEntityId;
            
            Debug.Log($"Connected to server! Client ID: {clientId}, Entity ID: {message.PlayerEntityId}");
            OnConnected?.Invoke();
        }
        
        private void HandleConnectReject(ConnectRejectMessage message)
        {
            Debug.LogWarning($"Connection rejected: {message.Reason}");
            Disconnect(message.Reason);
            OnConnectionFailed?.Invoke(message.Reason);
        }
        
        private void HandleSnapshot(SnapshotMessage message)
        {
            if (state == ClientState.Connected)
            {
                state = ClientState.InGame;
            }
            
            gameState.ProcessSnapshot(message);
            
            // Reconcile prediction with server state
            if (gameState.LocalPlayerId != 0)
            {
                ClientTank localTank = gameState.GetLocalPlayerTank();
                if (localTank != null)
                {
                    if (prediction.PredictedPosition == Vector3.zero)
                    {
                        // Initialize prediction
                        prediction.Initialize(localTank.ServerPosition, localTank.Rotation);
                        Debug.Log($"Initialized prediction for entity {gameState.LocalPlayerId}");
                    }
                    else
                    {
                        // Reconcile with server state
                        prediction.Reconcile(localTank.ServerPosition, localTank.Rotation, inputSequence);
                    }
                }
            }
        }
        
        private void HandleDeltaUpdate(DeltaUpdateMessage message)
        {
            gameState.ProcessDeltaUpdate(message);
            
            // Reconcile prediction with server state after delta update
            if (gameState.LocalPlayerId != 0)
            {
                ClientTank localTank = gameState.GetLocalPlayerTank();
                if (localTank != null && prediction.PredictedPosition != Vector3.zero)
                {
                    // Reconcile with server state
                    prediction.Reconcile(localTank.ServerPosition, localTank.Rotation, inputSequence);
                }
            }
        }
        
        private void HandleEntitySpawn(EntitySpawnMessage message)
        {
            gameState.SpawnEntity(message);
            
            //Prediction if this is our player spawning
            if (message.EntityId == gameState.LocalPlayerId && message.EntityType == EntityType.Tank)
            {
                prediction.Initialize(
                    new Vector3(message.PosX, message.PosY, message.PosZ),
                    message.RotY
                );
            }
        }
        
        private void HandleEntityDespawn(EntityDespawnMessage message)
        {
            gameState.DespawnEntity(message.EntityId);
        }
        
        private void HandleProjectileSpawn(ProjectileSpawnMessage message)
        {
            gameState.SpawnProjectile(message);
        }
        
        private void HandlePlayerDamage(PlayerDamageMessage message)
        {
            ClientEntity entity = gameState.GetEntity(message.TargetId);
            if (entity != null)
            {
                entity.Health = message.NewHealth;
            }
        }
        
        private void HandlePlayerDeath(PlayerDeathMessage message)
        {
            Debug.Log($"Player {message.PlayerId} killed by {message.KillerId}");
            ClientEntity entity = gameState.GetEntity(message.PlayerId);
            if (entity != null)
            {
                entity.GameObject.SetActive(false);
            }
        }
        
        private void HandlePlayerRespawn(PlayerRespawnMessage message)
        {
            Debug.Log($"Player {message.PlayerId} respawned");
            
            ClientEntity entity = gameState.GetEntity(message.PlayerId);
            if (entity != null)
            {
                entity.GameObject.SetActive(true);
            }
            
            // Reset prediction if it's our player
            if (message.PlayerId == gameState.LocalPlayerId)
            {
                prediction.Initialize(
                    new Vector3(message.PosX, message.PosY, message.PosZ),
                    0
                );
            }
        }
        
        /// <summary>
        /// Send message to server
        /// </summary>
        private void SendMessage(NetworkMessage message)
        {
            if (serverConnection == null)
                return;
            
            // Determine if this message type should use unreliable channel
            // Snapshots and DeltaUpdates don't need reliability (though client rarely sends these)
            bool useUnreliable = message.Type == MessageType.Snapshot || 
                                 message.Type == MessageType.DeltaUpdate;
            
            Packet packet = new Packet
            {
                Sequence = useUnreliable ? 
                    serverConnection.Unreliable.GetNextSequence() : 
                    serverConnection.Reliable.GetNextSequence(),
                Ack = serverConnection.Reliable.Ack,
                AckBitfield = serverConnection.Reliable.GetAckBitfield(),
                PacketType = (byte)message.Type,
                Payload = message.ToBytes()
            };
            
            byte[] data = packet.Serialize();
            
            if (!useUnreliable)
            {
                serverConnection.Reliable.AddPendingPacket(packet.Sequence, data, Time.time);
            }
            
            // Apply packet simulation
            if (packetSimulator.ProcessOutgoing(data, data.Length, serverConnection.RemoteEndPoint,
                out byte[] outData, out int outLength, out IPEndPoint outEndPoint))
            {
                socketManager.Send(outData, outLength, outEndPoint);
            }
        }
        
      
        public string GetConnectionInfo()
        {
            return $"State: {state} | Ping: {Ping:F0}ms";
        }
        
        public ClientTank GetLocalPlayerTank()
        {
            return gameState?.GetLocalPlayerTank();
        }
    }
}

