using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HaruhiChokuretsuLib.Util
{
    internal static class IO
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
    }
}
