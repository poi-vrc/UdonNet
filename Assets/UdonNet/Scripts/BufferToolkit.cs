
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace UdonNet
{
    /// <summary>
    /// A class that reads or writes supported types of data directly from or to a byte array for message delivery
    /// </summary>
    public class BufferToolkit : UdonSharpBehaviour
    {

        #region Standard Types
        public bool ReadBoolean(byte[] buffer, int offset)
        {
            return buffer[offset] == byte.MaxValue;
        }

        public int WriteBoolean(byte[] buffer, int offset, bool b)
        {
            buffer[offset] = b ? byte.MaxValue : byte.MinValue;
            return 1;
        }

        public byte ReadByte(byte[] buffer, int offset)
        {
            return buffer[offset];
        }

        public int WriteByte(byte[] buffer, int offset, byte b)
        {
            buffer[offset] = b;
            return 1;
        }

        public byte[] ReadBytes(byte[] buffer, int offset, int count)
        {
            byte[] output = new byte[count];
            for (int i = 0; i < count; i++)
            {
                output[i] = buffer[offset + i];
            }
            return output;
        }

        public int WriteBytes(byte[] buffer, int offset, byte[] bytes, int index, int count)
        {
            int n = 0;
            while (n < count)
            {
                buffer[offset + n] = bytes[index + n];
                n++;
            }
            return count;
        }

        public char ReadChar(byte[] buffer, int offset)
        {
            char output = '\0';
            output |= (char)(buffer[offset] << 8);
            output |= (char)buffer[offset + 1];
            return output;
        }

        public int WriteChar(byte[] buffer, int offset, char c)
        {
            buffer[offset++] = (byte)(c >> 8);
            buffer[offset++] = (byte)c;
            return 2;
        }

        public decimal ReadDecimal(byte[] buffer, int offset)
        {
            int[] bits = new int[4];
            for (int i = 0; i < 4; i++)
            {
                bits[i] = ReadInt32(buffer, offset);
                offset += 4;
            }
            return new decimal(bits);
        }

        public int WriteDecimal(byte[] buffer, int offset, decimal f)
        {
            int[] bits = decimal.GetBits(f);
            int c = 0;
            for (int i = 0; i < bits.Length; i++)
            {
                c += WriteInt32(buffer, offset + c, bits[i]);
            }
            return c;
        }

        /*
        public int WriteDouble(byte[] buffer, int offset, double d)
        {
            return -1;
        }
        */

        public short ReadInt16(byte[] buffer, int offset)
        {
            short output = 0;
            output |= (short)(buffer[offset] << 8);
#pragma warning disable CS0675 // Bitwise-or operator used on a sign-extended operand
            output |= buffer[offset + 1];
#pragma warning restore CS0675 // Bitwise-or operator used on a sign-extended operand
            return output;
        }

        public int WriteInt16(byte[] buffer, int offset, short s)
        {
            short bits = 8;
            buffer[offset++] = (byte)(s >> bits);
            buffer[offset++] = (byte)s;
            return 2;
        }

        public ushort ReadUInt16(byte[] buffer, int offset)
        {
            ushort output = 0;
            output |= (ushort)(buffer[offset] << 8);
            output |= buffer[offset + 1];
            return output;
        }

        public int WriteUInt16(byte[] buffer, int offset, ushort u)
        {
            ushort bits = 8;
            buffer[offset++] = (byte)(u >> bits);
            buffer[offset++] = (byte)u;
            return 2;
        }

        public int ReadInt32(byte[] buffer, int offset)
        {
            int output = 0;
            output |= buffer[offset] << 24;
            output |= buffer[offset + 1] << 16;
            output |= buffer[offset + 2] << 8;
            output |= buffer[offset + 3];
            return output;
        }

        public int WriteInt32(byte[] buffer, int offset, int i)
        {
            buffer[offset++] = (byte)(i >> 24);
            buffer[offset++] = (byte)(i >> 16);
            buffer[offset++] = (byte)(i >> 8);
            buffer[offset++] = (byte)i;
            return 4;
        }

        public uint ReadUInt32(byte[] buffer, int offset)
        {
            uint output = 0;
            output |= (uint)buffer[offset] << 24;
            output |= (uint)buffer[offset + 1] << 16;
            output |= (uint)buffer[offset + 2] << 8;
            output |= buffer[offset + 3];
            return output;
        }

        public int WriteUInt32(byte[] buffer, int offset, uint u)
        {
            buffer[offset++] = (byte)(u >> 24);
            buffer[offset++] = (byte)(u >> 16);
            buffer[offset++] = (byte)(u >> 8);
            buffer[offset++] = (byte)u;
            return 4;
        }

        public long ReadInt64(byte[] buffer, int offset)
        {
            long output = 0;
            output |= (long)buffer[offset] << 56;
            output |= (long)buffer[offset + 1] << 48;
            output |= (long)buffer[offset + 2] << 40;
            output |= (long)buffer[offset + 3] << 24;
            output |= (long)buffer[offset + 4] << 16;
            output |= (long)buffer[offset + 5] << 8;
            output |= buffer[offset + 6];
            return output;
        }

        public int WriteInt64(byte[] buffer, int offset, long i)
        {
            buffer[offset++] = (byte)(i >> 56);
            buffer[offset++] = (byte)(i >> 48);
            buffer[offset++] = (byte)(i >> 40);
            buffer[offset++] = (byte)(i >> 32);
            buffer[offset++] = (byte)(i >> 24);
            buffer[offset++] = (byte)(i >> 16);
            buffer[offset++] = (byte)(i >> 8);
            buffer[offset++] = (byte)i;
            return 8;
        }

        public ulong ReadUInt64(byte[] buffer, int offset)
        {
            ulong output = 0;
            output |= (ulong)buffer[offset] << 56;
            output |= (ulong)buffer[offset + 1] << 48;
            output |= (ulong)buffer[offset + 2] << 40;
            output |= (ulong)buffer[offset + 3] << 24;
            output |= (ulong)buffer[offset + 4] << 16;
            output |= (ulong)buffer[offset + 5] << 8;
            output |= buffer[offset + 6];
            return output;
        }

        public int WriteUInt64(byte[] buffer, int offset, ulong u)
        {
            buffer[offset++] = (byte)(u >> 56);
            buffer[offset++] = (byte)(u >> 48);
            buffer[offset++] = (byte)(u >> 40);
            buffer[offset++] = (byte)(u >> 32);
            buffer[offset++] = (byte)(u >> 24);
            buffer[offset++] = (byte)(u >> 16);
            buffer[offset++] = (byte)(u >> 8);
            buffer[offset++] = (byte)u;
            return 8;
        }

        public sbyte ReadSByte(byte[] buffer, int offset)
        {
            return (sbyte)ReadByte(buffer, offset);
        }

        public int WriteSByte(byte[] buffer, int offset, sbyte s)
        {
            return WriteByte(buffer, offset, (byte)s);
        }

        public float ReadSingle(byte[] buffer, int offset)
        {
            return -1;
        }

        public int WriteSingle(byte[] buffer, int offset, float f)
        {
            //TODO: it's incorrect, this is WriteHalf
            return WriteUInt16(buffer, offset, Mathf.FloatToHalf(f));
        }

        public string ReadStringAscii(byte[] bytes, int offset, int count)
        {
            string output = "";
            int c = 0;
            for (int i = offset; i < bytes.Length; i++)
            {
                byte b = bytes[i];
                if (b == 0x00 || c == count) //null
                {
                    break;
                }
                output += Convert.ToChar(b);
                c++;
            }
            return output;
        }

        public int WriteStringAscii(byte[] buffer, int offset, string str)
        {
            //This only encodes ASCII
            char[] arr = str.ToCharArray();
            byte[] output = new byte[arr.Length];
            for (int i = 0; i < arr.Length; i++)
            {
                output[i] = Convert.ToByte(arr[i]);
            }
            return arr.Length;
        }
        #endregion
    }
}
