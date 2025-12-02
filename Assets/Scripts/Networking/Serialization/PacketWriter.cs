using System;
using System.Text;

namespace CustomNetworking.Serialization
{
    /// <summary>
    /// Utility for writing binary data to a byte buffer
    /// </summary>
    public class PacketWriter
    {
        private byte[] buffer;
        private int position;

        public PacketWriter(int capacity = 1200)
        {
            buffer = new byte[capacity];
            position = 0;
        }

        public PacketWriter(byte[] existingBuffer)
        {
            buffer = existingBuffer;
            position = 0;
        }

        public int Position => position;
        public int Capacity => buffer.Length;
        public byte[] Buffer => buffer;

        public void Reset()
        {
            position = 0;
        }

        public byte[] ToArray()
        {
            byte[] result = new byte[position];
            Array.Copy(buffer, 0, result, 0, position);
            return result;
        }

        public void WriteByte(byte value)
        {
            EnsureCapacity(1);
            buffer[position++] = value;
        }

        public void WriteUShort(ushort value)
        {
            EnsureCapacity(2);
            buffer[position++] = (byte)(value >> 8);
            buffer[position++] = (byte)value;
        }

        public void WriteUInt(uint value)
        {
            EnsureCapacity(4);
            buffer[position++] = (byte)(value >> 24);
            buffer[position++] = (byte)(value >> 16);
            buffer[position++] = (byte)(value >> 8);
            buffer[position++] = (byte)value;
        }

        public void WriteInt(int value)
        {
            WriteUInt((uint)value);
        }

        public void WriteFloat(float value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }
            EnsureCapacity(4);
            Array.Copy(bytes, 0, buffer, position, 4);
            position += 4;
        }

        public void WriteBool(bool value)
        {
            WriteByte((byte)(value ? 1 : 0));
        }

        public void WriteString(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                WriteUShort(0);
                return;
            }

            byte[] bytes = Encoding.UTF8.GetBytes(value);
            ushort length = (ushort)Math.Min(bytes.Length, ushort.MaxValue);
            WriteUShort(length);
            EnsureCapacity(length);
            Array.Copy(bytes, 0, buffer, position, length);
            position += length;
        }

        public void WriteBytes(byte[] bytes, int offset, int count)
        {
            EnsureCapacity(count);
            Array.Copy(bytes, offset, buffer, position, count);
            position += count;
        }

        public void WriteVector2(float x, float y)
        {
            WriteFloat(x);
            WriteFloat(y);
        }

        public void WriteVector3(float x, float y, float z)
        {
            WriteFloat(x);
            WriteFloat(y);
            WriteFloat(z);
        }

        private void EnsureCapacity(int additionalBytes)
        {
            if (position + additionalBytes > buffer.Length)
            {
                // Resize buffer
                int newCapacity = Math.Max(buffer.Length * 2, position + additionalBytes);
                byte[] newBuffer = new byte[newCapacity];
                Array.Copy(buffer, 0, newBuffer, 0, position);
                buffer = newBuffer;
            }
        }
    }
}

