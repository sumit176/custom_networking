using System.Collections.Generic;
using CustomNetworking.Serialization;

namespace CustomNetworking.Core
{
    public class ReliableChannel
    {
        private const int SEQUENCE_BUFFER_SIZE = 1024;
        private const float RESEND_TIMEOUT = 0.3f; // 300ms

        private uint localSequence;
        private uint remoteSequence;
        private uint[] receivedPackets; // Bitarray for received packets
        
        private Dictionary<uint, PendingPacket> pendingPackets;

        private class PendingPacket
        {
            public byte[] Data;
            public float SendTime;
            public int RetransmitCount;
        }

        public uint LocalSequence => localSequence;
        public uint RemoteSequence => remoteSequence;
        public uint Ack => remoteSequence;
        
        public ReliableChannel()
        {
            localSequence = 0;
            remoteSequence = 0;
            receivedPackets = new uint[SEQUENCE_BUFFER_SIZE / 32];
            pendingPackets = new Dictionary<uint, PendingPacket>();
            new Queue<uint>();
        }

        public uint GetNextSequence()
        {
            return localSequence++;
        }

        public void AddPendingPacket(uint sequence, byte[] data, float currentTime)
        {
            if (pendingPackets.ContainsKey(sequence))
                return;

            pendingPackets[sequence] = new PendingPacket
            {
                Data = data,
                SendTime = currentTime,
                RetransmitCount = 0
            };
        }

        public float? ProcessAck(uint ack, uint ackBitfield, float currentTime)
        {
            float? sendTime = null;
            
            if (pendingPackets.ContainsKey(ack))
            {
                sendTime = pendingPackets[ack].SendTime;
                pendingPackets.Remove(ack);
            }

            // Process ACK bitfield (32 previous packets)
            for (int i = 0; i < 32; i++)
            {
                if ((ackBitfield & (1u << i)) != 0)
                {
                    uint sequence = ack - (uint)(i + 1);
                    if (pendingPackets.ContainsKey(sequence))
                    {
                        // Use the most recent ACK for RTT if we haven't found one yet
                        if (!sendTime.HasValue)
                        {
                            sendTime = pendingPackets[sequence].SendTime;
                        }
                        pendingPackets.Remove(sequence);
                    }
                }
            }
            
            return sendTime;
        }

        public List<(uint sequence, byte[] data)> GetPacketsToResend(float currentTime)
        {
            List<(uint, byte[])> toResend = new List<(uint, byte[])>();

            foreach (var kvp in pendingPackets)
            {
                if (currentTime - kvp.Value.SendTime > RESEND_TIMEOUT)
                {
                    kvp.Value.SendTime = currentTime;
                    kvp.Value.RetransmitCount++;
                    
                    // Give up after too many retries
                    if (kvp.Value.RetransmitCount > 10)
                    {
                        continue;
                    }
                    
                    toResend.Add((kvp.Key, kvp.Value.Data));
                }
            }

            return toResend;
        }

        public bool ProcessReceivedPacket(uint sequence)
        {
            // Check if this is a newer packet
            if (Packet.IsSequenceNewer(sequence, remoteSequence))
            {
                remoteSequence = sequence;
            }

            // Check if we've already received this packet
            if (IsPacketReceived(sequence))
            {
                return false; // Duplicate
            }

            // Mark as received
            MarkPacketReceived(sequence);
            return true;
        }

        public uint GetAckBitfield()
        {
            uint bitfield = 0;

            for (int i = 1; i <= 32; i++)
            {
                uint sequence = remoteSequence - (uint)i;
                if (IsPacketReceived(sequence))
                {
                    bitfield |= (1u << (i - 1));
                }
            }

            return bitfield;
        }

        private bool IsPacketReceived(uint sequence)
        {
            int index = (int)(sequence % SEQUENCE_BUFFER_SIZE);
            int arrayIndex = index / 32;
            int bitIndex = index % 32;
            return (receivedPackets[arrayIndex] & (1u << bitIndex)) != 0;
        }

        private void MarkPacketReceived(uint sequence)
        {
            int index = (int)(sequence % SEQUENCE_BUFFER_SIZE);
            int arrayIndex = index / 32;
            int bitIndex = index % 32;
            receivedPackets[arrayIndex] |= (1u << bitIndex);
        }

        public void CleanupOldPackets(float currentTime)
        {
            List<uint> toRemove = new List<uint>();
            
            foreach (var kvp in pendingPackets)
            {
                if (kvp.Value.RetransmitCount > 10 || currentTime - kvp.Value.SendTime > 5.0f)
                {
                    toRemove.Add(kvp.Key);
                }
            }
            
            foreach (uint seq in toRemove)
            {
                pendingPackets.Remove(seq);
            }
        }
    }
}

