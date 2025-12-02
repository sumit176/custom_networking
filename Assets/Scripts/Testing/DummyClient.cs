using System;
using System.Net;
using UnityEngine;
using CustomNetworking.Core;
using CustomNetworking.Protocol;
using CustomNetworking.Serialization;

namespace CustomNetworking.Testing
{
    /// <summary>
    /// Dummy client for stress testing - simulates a player without visuals
    /// </summary>
    public class DummyClient
    {
        private enum State
        {
            Disconnected,
            Connecting,
            Connected
        }
        
        private UdpSocketManager socketManager;
        private NetworkConnection serverConnection;
        private State state;
        private uint clientId;
        private string playerName;
        
        private uint inputSequence;
        private float inputTimer;
        private const float INPUT_RATE = 1.0f / 20.0f;
        
        // Random behavior
        private System.Random random;
        private Vector2 currentMoveDir;
        private Vector2 currentAimDir;
        private float behaviorChangeTimer;
        private const float BEHAVIOR_CHANGE_INTERVAL = 2.0f;
        
        public bool IsConnected => state == State.Connected;
        public string Name => playerName;
        public float Rtt => serverConnection?.Rtt ?? 0;
        
        public DummyClient(string name, int seed)
        {
            playerName = name;
            random = new System.Random(seed);
            state = State.Disconnected;
            socketManager = new UdpSocketManager();
            
            // Random initial behavior
            RandomizeBehavior();
        }
        
        public bool Connect(string address, int port)
        {
            if (state != State.Disconnected)
                return false;
            
            if (!socketManager.Connect(address, port))
                return false;
            
            IPAddress ipAddress;
            if (!IPAddress.TryParse(address, out ipAddress))
            {
                // Try DNS resolution
                try
                {
                    IPAddress[] addresses = Dns.GetHostAddresses(address);
                    if (addresses.Length > 0)
                    {
                        ipAddress = addresses[0];
                        Debug.Log($"DummyClient '{playerName}': Resolved '{address}' to {ipAddress}");
                    }
                    else
                    {
                        Debug.LogWarning($"DummyClient '{playerName}': Could not resolve hostname '{address}', using loopback");
                        ipAddress = IPAddress.Loopback;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"DummyClient '{playerName}': DNS error for '{address}': {ex.Message}, using loopback");
                    ipAddress = IPAddress.Loopback;
                }
            }
            
            IPEndPoint serverEndPoint = new IPEndPoint(ipAddress, port);
            serverConnection = new NetworkConnection(serverEndPoint);
            
            state = State.Connecting;
            
            // Send connect request
            SendMessage(new ConnectRequestMessage
            {
                PlayerName = playerName,
                ProtocolVersion = 1
            });
            
            return true;
        }
        
        public void Disconnect()
        {
            if (state != State.Disconnected)
            {
                if (state == State.Connected)
                {
                    SendMessage(new DisconnectMessage { Reason = "Dummy client disconnect" });
                }
                
                socketManager.Stop();
                state = State.Disconnected;
            }
        }
        
        public void Update(float deltaTime)
        {
            if (state == State.Disconnected)
                return;
            
            // Update connection
            if (serverConnection != null)
            {
                serverConnection.Update(Time.time);
                
                if (!serverConnection.IsConnected && state != State.Disconnected)
                {
                    Disconnect();
                    return;
                }
            }
            
            // Process incoming packets
            ProcessIncomingPackets();
            
            // Send input if connected
            if (state == State.Connected)
            {
                inputTimer += deltaTime;
                if (inputTimer >= INPUT_RATE)
                {
                    inputTimer -= INPUT_RATE;
                    SendInput();
                }
                
                // Change behavior periodically
                behaviorChangeTimer += deltaTime;
                if (behaviorChangeTimer >= BEHAVIOR_CHANGE_INTERVAL)
                {
                    behaviorChangeTimer -= BEHAVIOR_CHANGE_INTERVAL;
                    RandomizeBehavior();
                }
            }
            
            // Process send queue
            socketManager.ProcessSendQueue();
        }
        
        private void RandomizeBehavior()
        {
            // Random movement
            currentMoveDir.x = (float)(random.NextDouble() * 2.0 - 1.0);
            currentMoveDir.y = (float)(random.NextDouble() * 2.0 - 1.0);
            
            if (currentMoveDir.sqrMagnitude > 1)
            {
                currentMoveDir.Normalize();
            }
            
            // Random aim
            float angle = (float)(random.NextDouble() * Mathf.PI * 2.0);
            currentAimDir.x = Mathf.Cos(angle);
            currentAimDir.y = Mathf.Sin(angle);
        }
        
        private void SendInput()
        {
            inputSequence++;
            
            PlayerInputMessage input = new PlayerInputMessage
            {
                InputSequence = inputSequence,
                ServerTick = 0,
                MoveX = currentMoveDir.x,
                MoveY = currentMoveDir.y,
                AimX = currentAimDir.x,
                AimY = currentAimDir.y,
                Shoot = random.NextDouble() < 0.1 // 10% chance to shoot each input
            };
            
            SendMessage(input);
        }
        
        private void ProcessIncomingPackets()
        {
            while (socketManager.TryReceive(out UdpSocketManager.ReceivedPacket receivedPacket))
            {
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
                    var accept = message as ConnectAcceptMessage;
                    clientId = accept.ClientId;
                    state = State.Connected;
                    break;
                
                case MessageType.ConnectReject:
                    Disconnect();
                    break;
                
                // Ignore other messages for dummy client (no need to process game state)
            }
        }
        
        private void SendMessage(NetworkMessage message)
        {
            if (serverConnection == null)
                return;
            
            // Determine if this message type should use unreliable channel
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
            socketManager.Send(data, data.Length, serverConnection.RemoteEndPoint);
            
            // Only track for reliability if using reliable channel
            if (!useUnreliable)
            {
                serverConnection.Reliable.AddPendingPacket(packet.Sequence, data, Time.time);
            }
        }
    }
}

