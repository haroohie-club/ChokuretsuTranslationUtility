﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HaruhiChokuretsuLib.Util
{
    public static class IO
    {
        public static int ReadInt(IEnumerable<byte> data, int offset)
        {
            return BitConverter.ToInt32(data.Skip(offset).Take(4).ToArray());
        }

        public static uint ReadUInt(IEnumerable<byte> data, int offset)
        {
            return BitConverter.ToUInt32(data.Skip(offset).Take(4).ToArray());
        }

        public static short ReadShort(IEnumerable<byte> data, int offset)
        {
            return BitConverter.ToInt16(data.Skip(offset).Take(2).ToArray());
        }

        public static ushort ReadUShort(IEnumerable<byte> data, int offset)
        {
            return BitConverter.ToUInt16(data.Skip(offset).Take(2).ToArray());
        }

        public static string ReadShiftJisString(IEnumerable<byte> data, int offset)
        {
            return Encoding.GetEncoding("Shift-JIS").GetString(data.Skip(offset).TakeWhile(b => b != 0x00).ToArray());
        }

        public static string ReadAsciiString(IEnumerable<byte> data, int offset)
        {
            return Encoding.ASCII.GetString(data.Skip(offset).TakeWhile(b => b != 0x00).ToArray());
        }
    }

    public static class BigEndianIO
    {
        public static int ReadInt(IEnumerable<byte> data, int offset)
        {
            return BitConverter.ToInt32(data.Skip(offset).Take(4).Reverse().ToArray());
        }

        public static uint ReadUInt(IEnumerable<byte> data, int offset)
        {
            return BitConverter.ToUInt32(data.Skip(offset).Take(4).Reverse().ToArray());
        }

        public static short ReadShort(IEnumerable<byte> data, int offset)
        {
            return BitConverter.ToInt16(data.Skip(offset).Take(2).Reverse().ToArray());
        }

        public static ushort ReadUShort(IEnumerable<byte> data, int offset)
        {
            return BitConverter.ToUInt16(data.Skip(offset).Take(2).Reverse().ToArray());
        }

        public static uint ReadBits(IEnumerable<byte> data, int offset, int bitOffset, int numBits)
        {
            if (numBits > 32)
            {
                return 0;
            }

            offset += bitOffset / 8;
            bitOffset %= 8;

            uint result = 0;

            while (numBits > 0)
            {
                byte currentBit = (byte)((data.ElementAt(offset) & (1 << (7 - bitOffset))) >> (7 - bitOffset));
                result |= (uint)currentBit << (numBits - 1);
                numBits--;
                bitOffset++;
                if (bitOffset == 8)
                {
                    bitOffset = 0;
                    offset++;
                }
            }

            return result;
        }

        public static IEnumerable<byte> GetBytes(int int32)
        {
            return BitConverter.GetBytes(int32).Reverse();
        }

        public static IEnumerable<byte> GetBytes(uint uint32)
        {
            return BitConverter.GetBytes(uint32).Reverse();
        }

        public static IEnumerable<byte> GetBytes(short int16)
        {
            return BitConverter.GetBytes(int16).Reverse();
        }

        public static IEnumerable<byte> GetBytes(ushort uint16)
        {
            return BitConverter.GetBytes(uint16).Reverse();
        }
    }
}
