
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace UdonNet
{
    /// <summary>
    /// A class that writes supported types of data directly to a byte array for message delivery
    /// </summary>
    public class BufferedWriter : UdonSharpBehaviour
    {
        public NetworkedUNPlayer networkedUNPlayer;

        private byte[] buffer = null;

        private int offset = -1;

        private int count = -1;

        private int written = 0;

        void Start()
        {

        }

        void Begin(byte[] buffer, int offset, int count)
        {
            this.buffer = buffer;
            this.offset = offset;
            this.count = count;
            written = 0;
        }

        public void WriteBoolean(bool b)
        {
            buffer[offset++] = b ? byte.MaxValue : byte.MinValue;
        }

        public void WriteByte(byte b)
        {
            buffer[offset++] = b;
        }

        public void WriteBytes(byte[] buffer, int index, int count)
        {
            int n = 0;
            while (n < count)
            {
                this.buffer[offset + n] = buffer[index + n];
                n++;
            }
        }

        public void WriteChar(char c)
        {
            buffer[offset++] = (byte) (c >> 8);
            buffer[offset++] = (byte) c;
        }

        public void WriteChars(char[] chars)
        {
            for (int i = 0; i < chars.Length; i++)
            {
                WriteChar(chars[i]);
            }
        }
        
        public void WriteDecimal(decimal f)
        {
            int[] bits = decimal.GetBits(f);
            for (int i = 0; i < bits.Length; i++)
            {
                WriteInt32(bits[i]);
            }
        }

        public void WriteDouble(double d)
        {

        }

        public void WriteInt16(short s)
        {

        }

        public void WriteInt32(int i)
        {

        }

        public void WriteInt64(long i)
        {

        }

        public void WriteSByte(sbyte s)
        {

        }

        public void WriteSingle(float f)
        {

        }

        public void WriteStringAscii(string str)
        {

        }

        public void WriteUInt16(ushort u)
        {

        }

        public void WriteUInt32(uint u)
        {

        }

        public void WriteUInt64(uint u)
        {

        }

        public bool IsNotReady()
        {
            return buffer == null || offset == -1 || count == -1;
        }

        public bool IsFull()
        {
            return offset == count;
        }

        public int GetOffset()
        {
            return offset;
        }

        public int GetCount()
        {
            return count;
        }

        public byte[] GetByteArray()
        {
            return buffer;
        }

        public void Flush()
        {
            
        }

        public void Write()
        {

        }
    }
}
