using System;

namespace CustomNetworking.Serialization
{
    /// <summary>
    /// Represents a network packet with header and payload
    /// Header format (19 bytes):
    /// - Protocol ID: 4 bytes (magic number for validation)
    /// - Sequence: 4 bytes (packet sequence number)
    /// - ACK: 4 bytes (last received sequence from remote)
    /// - ACK Bitfield: 4 bytes (32 bits for previous packets)
    /// - Packet Type: 1 byte (message type identifier)
    /// - Payload Length: 2 bytes (length of payload data)
    /// </summary>
    public class Packet
    {
        public const uint PROTOCOL_ID = 0x12345678; // Magic number for validation
        public const int HEADER_SIZE = 19;
        public const int MAX_PACKET_SIZE = 1200; // Safe MTU size
        public const int MAX_PAYLOAD_SIZE = MAX_PACKET_SIZE - HEADER_SIZE;

        public uint ProtocolId { get; set; }
        public uint Sequence { get; set; }
        public uint Ack { get; set; }
        public uint AckBitfield { get; set; }
        public byte PacketType { get; set; }
        public byte[] Payload { get; set; }

        public Packet()
        {
            ProtocolId = PROTOCOL_ID;
            Payload = new byte[0];
        }

        /// <summary>
        /// Serialize packet to byte array
        /// </summary>
        public byte[] Serialize()
        {
            PacketWriter writer = new PacketWriter(HEADER_SIZE + Payload.Length);
            
            // Write header
            writer.WriteUInt(ProtocolId);
            writer.WriteUInt(Sequence);
            writer.WriteUInt(Ack);
            writer.WriteUInt(AckBitfield);
            writer.WriteByte(PacketType);
            writer.WriteUShort((ushort)Payload.Length);
            
            // Write payload
            if (Payload.Length > 0)
            {
                writer.WriteBytes(Payload, 0, Payload.Length);
            }
            
            return writer.ToArray();
        }

        /// <summary>
        /// Deserialize packet from byte array
        /// </summary>
        public static Packet Deserialize(byte[] data, int offset, int length)
        {
            if (length < HEADER_SIZE)
                return null;

            PacketReader reader = new PacketReader(data, offset, length);
            
            Packet packet = new Packet();
            
            // Read header
            packet.ProtocolId = reader.ReadUInt();
            
            // Validate protocol ID
            if (packet.ProtocolId != PROTOCOL_ID)
                return null;
            
            packet.Sequence = reader.ReadUInt();
            packet.Ack = reader.ReadUInt();
            packet.AckBitfield = reader.ReadUInt();
            packet.PacketType = reader.ReadByte();
            ushort payloadLength = reader.ReadUShort();
            
            // Validate payload length
            if (payloadLength > MAX_PAYLOAD_SIZE || reader.Remaining < payloadLength)
                return null;
            
            // Read payload
            if (payloadLength > 0)
            {
                packet.Payload = reader.ReadBytes(payloadLength);
            }
            
            return packet;
        }

        /// <summary>
        /// Check if a sequence number is more recent than another (handles wraparound)
        /// </summary>
        public static bool IsSequenceNewer(uint s1, uint s2)
        {
            return ((s1 > s2) && (s1 - s2 <= 0x7FFFFFFF)) ||
                   ((s1 < s2) && (s2 - s1 > 0x7FFFFFFF));
        }
    }
}

