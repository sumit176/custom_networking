using System;
using System.Net;
using CustomNetworking.Serialization;

namespace CustomNetworking.Core
{
    public class NetworkConnection
    {
        private const float TIMEOUT_DURATION = 10.0f; // 10 seconds without packets = disconnect
        private const int RTT_SAMPLE_SIZE = 10;

        public IPEndPoint RemoteEndPoint { get; private set; }
        public bool IsConnected { get; private set; }
        public float LastReceiveTime { get; private set; }
        public float Rtt { get; private set; } // Round-trip time in milliseconds
        public float PacketLoss { get; private set; } // Percentage

        private ReliableChannel reliableChannel;
        private UnreliableChannel unreliableChannel;
        
        private float[] rttSamples;
        private int rttSampleIndex;
        private int totalPacketsSent;
        private int totalPacketsLost;
        
        public ReliableChannel Reliable => reliableChannel;
        public UnreliableChannel Unreliable => unreliableChannel;

        public NetworkConnection(IPEndPoint remoteEndPoint)
        {
            RemoteEndPoint = remoteEndPoint;
            IsConnected = true;
            LastReceiveTime = UnityEngine.Time.time;
            
            reliableChannel = new ReliableChannel();
            unreliableChannel = new UnreliableChannel();
            
            rttSamples = new float[RTT_SAMPLE_SIZE];
            rttSampleIndex = 0;
            Rtt = 0;
            PacketLoss = 0;
        }

        public void Update(float currentTime)
        {
            // Check for timeout
            if (IsConnected && currentTime - LastReceiveTime > TIMEOUT_DURATION)
            {
                IsConnected = false;
                UnityEngine.Debug.Log($"Connection timed out: {RemoteEndPoint}");
            }

            // Cleanup old reliable packets
            reliableChannel.CleanupOldPackets(currentTime);
        }

        public void OnPacketReceived(float currentTime)
        {
            LastReceiveTime = currentTime;
        }

        public void UpdateRtt(float sendTime, float currentTime)
        {
            float sample = (currentTime - sendTime) * 1000.0f; // Convert to milliseconds
            
            rttSamples[rttSampleIndex] = sample;
            rttSampleIndex = (rttSampleIndex + 1) % RTT_SAMPLE_SIZE;
            
            // Calculate average RTT
            float sum = 0;
            int count = 0;
            for (int i = 0; i < RTT_SAMPLE_SIZE; i++)
            {
                if (rttSamples[i] > 0)
                {
                    sum += rttSamples[i];
                    count++;
                }
            }
            
            if (count > 0)
            {
                Rtt = sum / count;
            }
        }

        public void UpdatePacketLoss(int sent, int lost)
        {
            totalPacketsSent += sent;
            totalPacketsLost += lost;
            
            if (totalPacketsSent > 0)
            {
                PacketLoss = (totalPacketsLost / (float)totalPacketsSent) * 100.0f;
            }
        }

        public void Disconnect()
        {
            IsConnected = false;
        }

        public string GetConnectionInfo()
        {
            return $"{RemoteEndPoint} - RTT: {Rtt:F1}ms, Loss: {PacketLoss:F1}%, Connected: {IsConnected}";
        }
    }
}

