using System.Collections.Generic;
using CustomNetworking.Core;
using CustomNetworking.Protocol;

namespace CustomNetworking.Server
{
    public class ServerPlayer
    {
        public uint ClientId { get; set; }
        public string PlayerName { get; set; }
        public NetworkConnection Connection { get; set; }
        
        // Entity reference
        public uint EntityId { get; set; }
        
        // Player state
        public bool IsAlive { get; set; }
        public float RespawnTimer { get; set; }
        private const float RESPAWN_TIME = 3.0f;
        
        // Input buffering
        private Queue<PlayerInputMessage> inputBuffer;
        private uint LastProcessedInputSequence { get; set; }
        
        // Statistics
        public int Kills { get; set; }
        public int Deaths { get; set; }
        
        public ServerPlayer(uint clientId, string playerName, NetworkConnection connection)
        {
            ClientId = clientId;
            PlayerName = playerName;
            Connection = connection;
            IsAlive = false;
            RespawnTimer = 0;
            inputBuffer = new Queue<PlayerInputMessage>();
            LastProcessedInputSequence = 0;
            Kills = 0;
            Deaths = 0;
        }
        
        /// <summary>
        /// Add input to buffer
        /// </summary>
        public void AddInput(PlayerInputMessage input)
        {
            // Only buffer if input is newer than last processed
            if (input.InputSequence > LastProcessedInputSequence)
            {
                inputBuffer.Enqueue(input);
                
                // Limit buffer size
                while (inputBuffer.Count > 60) // Max 3 seconds at 20Hz
                {
                    inputBuffer.Dequeue();
                }
            }
        }
        
        public PlayerInputMessage GetNextInput()
        {
            if (inputBuffer.Count > 0)
            {
                PlayerInputMessage input = inputBuffer.Dequeue();
                LastProcessedInputSequence = input.InputSequence;
                return input;
            }
            return null;
        }
        
        public void ClearInputs()
        {
            inputBuffer.Clear();
        }
        
        public void UpdateRespawn(float deltaTime)
        {
            if (!IsAlive && RespawnTimer > 0)
            {
                RespawnTimer -= deltaTime;
                if (RespawnTimer <= 0)
                {
                    RespawnTimer = 0;
                }
            }
        }
        
        public void Die()
        {
            IsAlive = false;
            RespawnTimer = RESPAWN_TIME;
            Deaths++;
        }
        
        public void Respawn()
        {
            IsAlive = true;
            RespawnTimer = 0;
        }
    }
}

