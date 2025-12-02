using System;
using System.Text;

namespace CustomNetworking.Serialization
{
    /// <summary>
    /// Utility for reading binary data from a byte buffer
    /// </summary>
    public class PacketReader
    {
        private byte[] buffer;
        private int position;
        private int length;

        public PacketReader(byte[] buffer, int offset, int length)
        {
            this.buffer = buffer;
            this.position = offset;
            this.length = offset + length;
        }

        public int Remaining => length - position;
        public bool CanRead(int bytes) => position + bytes <= length;

        public byte ReadByte()
        {
            if (position >= length)
                throw new IndexOutOfRangeException("PacketReader: Cannot read beyond buffer");
            return buffer[position++];
        }

        public ushort ReadUShort()
        {
            if (position + 2 > length)
                throw new IndexOutOfRangeException("PacketReader: Cannot read beyond buffer");
            ushort value = (ushort)((buffer[position] << 8) | buffer[position + 1]);
            position += 2;
            return value;
        }

        public uint ReadUInt()
        {
            if (position + 4 > length)
                throw new IndexOutOfRangeException("PacketReader: Cannot read beyond buffer");
            uint value = (uint)((buffer[position] << 24) | (buffer[position + 1] << 16) |
                               (buffer[position + 2] << 8) | buffer[position + 3]);
            position += 4;
            return value;
        }

        public int ReadInt()
        {
            return (int)ReadUInt();
        }

        public float ReadFloat()
        {
            if (position + 4 > length)
                throw new IndexOutOfRangeException("PacketReader: Cannot read beyond buffer");
            
            byte[] bytes = new byte[4];
            Array.Copy(buffer, position, bytes, 0, 4);
            
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }
            
            position += 4;
            return BitConverter.ToSingle(bytes, 0);
        }

        public bool ReadBool()
        {
            return ReadByte() != 0;
        }

        public string ReadString()
        {
            ushort length = ReadUShort();
            if (length == 0)
                return string.Empty;

            if (position + length > this.length)
                throw new IndexOutOfRangeException("PacketReader: Cannot read beyond buffer");

            string value = Encoding.UTF8.GetString(buffer, position, length);
            position += length;
            return value;
        }

        public byte[] ReadBytes(int count)
        {
            if (position + count > length)
                throw new IndexOutOfRangeException("PacketReader: Cannot read beyond buffer");

            byte[] bytes = new byte[count];
            Array.Copy(buffer, position, bytes, 0, count);
            position += count;
            return bytes;
        }

        public void ReadVector2(out float x, out float y)
        {
            x = ReadFloat();
            y = ReadFloat();
        }

        public void ReadVector3(out float x, out float y, out float z)
        {
            x = ReadFloat();
            y = ReadFloat();
            z = ReadFloat();
        }
    }
}

