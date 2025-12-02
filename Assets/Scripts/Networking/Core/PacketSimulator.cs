using System;
using System.Collections.Generic;

namespace CustomNetworking.Core
{
    /// <summary>
    /// Simulates packet loss and latency for testing
    /// </summary>
    public class PacketSimulator
    {
        public bool Enabled { get; set; }
        public float PacketLossPercent { get; set; } // 0-100
        public float LatencyMs { get; set; } // Milliseconds
        public float JitterMs { get; set; } // Random variation in latency

        private Queue<DelayedPacket> delayedPackets;
        private Random random;

        private struct DelayedPacket
        {
            public byte[] Data;
            public int Length;
            public System.Net.IPEndPoint EndPoint;
            public float DeliveryTime;
        }

        public PacketSimulator()
        {
            Enabled = false;
            PacketLossPercent = 0;
            LatencyMs = 0;
            JitterMs = 0;
            delayedPackets = new Queue<DelayedPacket>();
            random = new Random();
        }

        /// <summary>
        /// Process outgoing packet through simulator
        /// Returns true if packet should be sent, false if dropped
        /// </summary>
        public bool ProcessOutgoing(byte[] data, int length, System.Net.IPEndPoint endPoint, 
            out byte[] outData, out int outLength, out System.Net.IPEndPoint outEndPoint)
        {
            outData = data;
            outLength = length;
            outEndPoint = endPoint;

            if (!Enabled)
                return true;

            // Simulate packet loss
            if (PacketLossPercent > 0 && random.NextDouble() * 100.0 < PacketLossPercent)
            {
                return false; // Drop packet
            }

            // Simulate latency
            if (LatencyMs > 0)
            {
                float delay = LatencyMs;
                
                // Add jitter
                if (JitterMs > 0)
                {
                    delay += (float)(random.NextDouble() * 2.0 - 1.0) * JitterMs;
                    delay = Math.Max(0, delay);
                }

                float deliveryTime = UnityEngine.Time.time + delay / 1000.0f;
                
                // Queue for delayed delivery
                delayedPackets.Enqueue(new DelayedPacket
                {
                    Data = data,
                    Length = length,
                    EndPoint = endPoint,
                    DeliveryTime = deliveryTime
                });
                
                return false; // Don't send immediately
            }

            return true; // Send immediately
        }

        /// <summary>
        /// Process incoming packet through simulator
        /// Returns true if packet should be delivered, false if dropped
        /// </summary>
        public bool ProcessIncoming(byte[] data, int length)
        {
            if (!Enabled)
                return true;

            // Simulate packet loss on incoming
            if (PacketLossPercent > 0 && random.NextDouble() * 100.0 < PacketLossPercent)
            {
                return false; // Drop packet
            }

            return true;
        }

        /// <summary>
        /// Get delayed packets that are ready to send
        /// </summary>
        public List<(byte[] data, int length, System.Net.IPEndPoint endPoint)> GetDelayedPackets(float currentTime)
        {
            List<(byte[], int, System.Net.IPEndPoint)> readyPackets = 
                new List<(byte[], int, System.Net.IPEndPoint)>();

            int count = delayedPackets.Count;
            for (int i = 0; i < count; i++)
            {
                DelayedPacket packet = delayedPackets.Dequeue();
                
                if (packet.DeliveryTime <= currentTime)
                {
                    readyPackets.Add((packet.Data, packet.Length, packet.EndPoint));
                }
                else
                {
                    // Re-queue if not ready
                    delayedPackets.Enqueue(packet);
                }
            }

            return readyPackets;
        }

        /// <summary>
        /// Clear all delayed packets
        /// </summary>
        public void Clear()
        {
            delayedPackets.Clear();
        }
    }
}

