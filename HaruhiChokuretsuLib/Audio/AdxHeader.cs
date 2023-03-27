using HaruhiChokuretsuLib.Util;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// This code is ported from https://github.com/Isaac-Lozano/radx
namespace HaruhiChokuretsuLib.Audio
{
    public class AdxHeader
    {
        public const ushort ADX_MAGIC = 0x8000; // big endian
        public const int ADX_HEADER_LENGTH = 0x800;

        public AdxEncoding AdxEncoding { get; set; }
        public byte BlockSize { get; set; }
        public byte SampleBitdepth { get; set; }
        public byte ChannelCount { get; set; }
        public uint SampleRate { get; set; }
        public uint TotalSamples { get; set; }
        public ushort HighpassFrequency { get; set; }
        public byte Version { get; set; }
        public byte Flags { get; set; }

        public AdxVersion3LoopInfo LoopInfo = new();
        public int HeaderSize { get; private set; }

        public AdxHeader(IEnumerable<byte> data, ILogger log)
        {
            if (BigEndianIO.ReadUShort(data, 0) != ADX_MAGIC)
            {
                log.LogError("File was not an ADX file.");
                return;
            }

            ushort dataOffset = BigEndianIO.ReadUShort(data, 0x02);
            AdxEncoding = (AdxEncoding)data.ElementAt(0x04);
            BlockSize = data.ElementAt(0x05);
            SampleBitdepth = data.ElementAt(0x06);
            ChannelCount = data.ElementAt(0x07);
            SampleRate = BigEndianIO.ReadUInt(data, 0x08);
            TotalSamples = BigEndianIO.ReadUInt(data, 0x0C);
            HighpassFrequency = BigEndianIO.ReadUShort(data, 0x10);
            Version = data.ElementAt(0x12);
            Flags = data.ElementAt(0x13);

            if (Version == 3 && dataOffset >= 40)
            {
                if (BigEndianIO.ReadUShort(data, 0x22) == 1 && BigEndianIO.ReadUInt(data, 0x24) == 1)
                {
                    LoopInfo = new(data.Skip(0x20).Take(0x18));
                }
                else
                {
                    LoopInfo = new(data.Skip(0x14).Take(0x18));
                }
            }

            if (Encoding.ASCII.GetString(data.Skip(dataOffset - 2).Take(6).ToArray()) != "(c)CRI")
            {
                log.LogError("ADX file had bad copyright string.");
                return;
            }

            HeaderSize = dataOffset + 4;
        }
        public AdxHeader()
        {
        }

        public List<byte> GetBytes(int headerSize)
        {
            List<byte> bytes = new();

            bytes.AddRange(BigEndianIO.GetBytes(ADX_MAGIC));
            bytes.AddRange(BigEndianIO.GetBytes((short)(headerSize - 0x04)));
            bytes.Add((byte)AdxEncoding);
            bytes.Add(BlockSize);
            bytes.Add(SampleBitdepth);
            bytes.Add(ChannelCount);
            bytes.AddRange(BigEndianIO.GetBytes(SampleRate));
            bytes.AddRange(BigEndianIO.GetBytes(TotalSamples));
            bytes.AddRange(BigEndianIO.GetBytes(HighpassFrequency));
            bytes.Add(Version);
            bytes.Add(Flags);
            if (Version == 3)
            {
                bytes.AddRange(LoopInfo.GetBytes());
            }
            bytes.AddRange(new byte[headerSize - bytes.Count - 0x06]);
            bytes.AddRange(Encoding.ASCII.GetBytes("(c)CRI"));

            return bytes;
        }
    }

    public struct AdxVersion3LoopInfo
    {
        public ushort AlignmentSamples { get; set; }
        public ushort EnabledShort { get; set; }
        public uint EnabledInt { get; set; }
        public uint BeginSample { get; set; }
        public uint BeginByte { get; set; }
        public uint EndSample { get; set; }
        public uint EndByte { get; set; }

        public AdxVersion3LoopInfo(IEnumerable<byte> bytes)
        {
            AlignmentSamples = BigEndianIO.ReadUShort(bytes, 0);
            EnabledShort = BigEndianIO.ReadUShort(bytes, 0x02);
            EnabledInt = BigEndianIO.ReadUInt(bytes, 0x04);
            BeginSample = BigEndianIO.ReadUInt(bytes, 0x08);
            BeginByte = BigEndianIO.ReadUInt(bytes, 0x0C);
            EndSample = BigEndianIO.ReadUInt(bytes, 0x10);
            EndByte = BigEndianIO.ReadUInt(bytes, 0x14);
        }

        public List<byte> GetBytes()
        {
            List<byte> bytes = new();

            bytes.AddRange(BigEndianIO.GetBytes(AlignmentSamples));
            bytes.AddRange(BigEndianIO.GetBytes(EnabledShort));
            bytes.AddRange(BigEndianIO.GetBytes(EnabledInt));
            bytes.AddRange(BigEndianIO.GetBytes(BeginSample));
            bytes.AddRange(BigEndianIO.GetBytes(BeginByte));
            bytes.AddRange(BigEndianIO.GetBytes(EndSample));
            bytes.AddRange(BigEndianIO.GetBytes(EndByte));

            return bytes;
        }
    }

    public enum AdxEncoding : byte
    {
        Preset = 0x02,
        Standard = 0x03,
        Exponential = 0x04,
        Ahx10 = 0x10,
        Ahx11 = 0x11,
    }
}
