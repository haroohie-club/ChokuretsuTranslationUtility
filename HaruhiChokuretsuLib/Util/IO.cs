using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HaruhiChokuretsuLib.Util
{
    /// <summary>
    /// Simple helper class for little-endian binary reading
    /// </summary>
    public static class IO
    {
        /// <summary>
        /// Reads a signed 32-bit integer (int) from binary data given an offset
        /// </summary>
        /// <param name="data">Binary data to read from</param>
        /// <param name="offset">Offset into the binary data to start reading from</param>
        /// <returns>Signed integer representation of the binary data read</returns>
        public static int ReadInt(IEnumerable<byte> data, int offset)
        {
            return BitConverter.ToInt32(data.Skip(offset).Take(4).ToArray());
        }

        /// <summary>
        /// Reads an unsigned 32-bit integer (uint) from binary data given an offset
        /// </summary>
        /// <param name="data">Binary data to read from</param>
        /// <param name="offset">Offset into the binary data to start reading from</param>
        /// <returns>Unsigned integer representation of the binary data read</returns>
        public static uint ReadUInt(IEnumerable<byte> data, int offset)
        {
            return BitConverter.ToUInt32(data.Skip(offset).Take(4).ToArray());
        }

        /// <summary>
        /// Reads a signed 16-bit integer (short) from binary data given an offset
        /// </summary>
        /// <param name="data">Binary data to read from</param>
        /// <param name="offset">Offset into the binary data to start reading from</param>
        /// <returns>Signed short representation of the binary data read</returns>
        public static short ReadShort(IEnumerable<byte> data, int offset)
        {
            return BitConverter.ToInt16(data.Skip(offset).Take(2).ToArray());
        }

        /// <summary>
        /// Reads an unsigned 16-bit integer (ushort) from binary data given an offset
        /// </summary>
        /// <param name="data">Binary data to read from</param>
        /// <param name="offset">Offset into the binary data to start reading from</param>
        /// <returns>Unsigned short representation of the binary data read</returns>
        public static ushort ReadUShort(IEnumerable<byte> data, int offset)
        {
            return BitConverter.ToUInt16(data.Skip(offset).Take(2).ToArray());
        }

        /// <summary>
        /// Reads a NULL (0x00) terminated Shift-JIS encoded string from binary data at an offset
        /// </summary>
        /// <param name="data">Binary data to read from</param>
        /// <param name="offset">Offset into the binary data to start reading from</param>
        /// <returns>A standard string containing the Shift-JIS encoded text read from the data</returns>
        public static string ReadShiftJisString(IEnumerable<byte> data, int offset)
        {
            return Encoding.GetEncoding("Shift-JIS").GetString(data.Skip(offset).TakeWhile(b => b != 0x00).ToArray());
        }

        /// <summary>
        /// Reads a NULL (0x00) terminated ASCII encoded string from binary data at an offset
        /// </summary>
        /// <param name="data">Binary data to read from</param>
        /// <param name="offset">Offset into the binary data to start reading from</param>
        /// <returns>A standard string containing the ASCII encoded text read from the data</returns>
        public static string ReadAsciiString(IEnumerable<byte> data, int offset)
        {
            return Encoding.ASCII.GetString(data.Skip(offset).TakeWhile(b => b != 0x00).ToArray());
        }
    }

    /// <summary>
    /// Same as the IO class but uses big-endian rather than little-endian encoding for integers
    /// </summary>
    public static class BigEndianIO
    {
        /// <summary>
        /// Reads a signed 32-bit integer (int) from binary data given an offset
        /// </summary>
        /// <param name="data">Binary data to read from</param>
        /// <param name="offset">Offset into the binary data to start reading from</param>
        /// <returns>Signed integer representation of the binary data read</returns>
        public static int ReadInt(IEnumerable<byte> data, int offset)
        {
            return BitConverter.ToInt32(data.Skip(offset).Take(4).Reverse().ToArray());
        }

        /// <summary>
        /// Reads an unsigned 32-bit integer (uint) from binary data given an offset
        /// </summary>
        /// <param name="data">Binary data to read from</param>
        /// <param name="offset">Offset into the binary data to start reading from</param>
        /// <returns>Unsigned integer representation of the binary data read</returns>
        public static uint ReadUInt(IEnumerable<byte> data, int offset)
        {
            return BitConverter.ToUInt32(data.Skip(offset).Take(4).Reverse().ToArray());
        }

        /// <summary>
        /// Reads a signed 16-bit integer (short) from binary data given an offset
        /// </summary>
        /// <param name="data">Binary data to read from</param>
        /// <param name="offset">Offset into the binary data to start reading from</param>
        /// <returns>Signed short representation of the binary data read</returns>
        public static short ReadShort(IEnumerable<byte> data, int offset)
        {
            return BitConverter.ToInt16(data.Skip(offset).Take(2).Reverse().ToArray());
        }

        /// <summary>
        /// Reads an unsigned 16-bit integer (ushort) from binary data given an offset
        /// </summary>
        /// <param name="data">Binary data to read from</param>
        /// <param name="offset">Offset into the binary data to start reading from</param>
        /// <returns>Unsigned short representation of the binary data read</returns>
        public static ushort ReadUShort(IEnumerable<byte> data, int offset)
        {
            return BitConverter.ToUInt16(data.Skip(offset).Take(2).Reverse().ToArray());
        }

        /// <summary>
        /// Reads an arbitrary number of bits from big-endian binary data and interprets them as integer data
        /// </summary>
        /// <param name="data">The binary data to read from</param>
        /// <param name="offset">The byte-offset to start reading from</param>
        /// <param name="bitOffset">The bit-offset (within the byte specified by the byte-offset) to start reading from</param>
        /// <param name="numBits">The number of bits to read</param>
        /// <returns>An unsigned integer representation of the bits read</returns>
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

        /// <summary>
        /// Returns the big-endian binary representation of a 32-bit signed integer
        /// </summary>
        /// <param name="int32">The 32-bit signed integer to encode</param>
        /// <returns>The big-endian binary representation of that integer</returns>
        public static IEnumerable<byte> GetBytes(int int32)
        {
            return BitConverter.GetBytes(int32).Reverse();
        }

        /// <summary>
        /// Returns the big-endian binary representation of a 32-bit unsigned integer
        /// </summary>
        /// <param name="uint32">The 32-bit unsigned integer to encode</param>
        /// <returns>The big-endian binary representation of that integer</returns>
        public static IEnumerable<byte> GetBytes(uint uint32)
        {
            return BitConverter.GetBytes(uint32).Reverse();
        }

        /// <summary>
        /// Returns the big-endian binary representation of a 16-bit signed integer
        /// </summary>
        /// <param name="int16">The 16-bit signed integer to encode</param>
        /// <returns>The big-endian binary representation of that short</returns>
        public static IEnumerable<byte> GetBytes(short int16)
        {
            return BitConverter.GetBytes(int16).Reverse();
        }

        /// <summary>
        /// Returns the big-endian binary representation of a 16-bit unsigned integer
        /// </summary>
        /// <param name="uint16">The 16-bit unsigned integer to encode</param>
        /// <returns>The big-endian binary representation of that short</returns>
        public static IEnumerable<byte> GetBytes(ushort uint16)
        {
            return BitConverter.GetBytes(uint16).Reverse();
        }
    }
}
