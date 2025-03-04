using HaruhiChokuretsuLib.Util;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// This code is ported from https://github.com/Isaac-Lozano/radx
namespace HaruhiChokuretsuLib.Audio.ADX;

/// <summary>
/// Header of ADX file
/// </summary>
public class AdxHeader
{
    /// <summary>
    /// ADX magic code
    /// </summary>
    public const ushort ADX_MAGIC = 0x8000; // big endian
    /// <summary>
    /// ADX header length in bytes
    /// </summary>
    public const int ADX_HEADER_LENGTH = 0x800;

    /// <summary>
    /// The ADX encoding type
    /// </summary>
    public AdxEncoding AdxEncoding { get; set; }
    /// <summary>
    /// Block size in bytes
    /// </summary>
    public byte BlockSize { get; set; }
    /// <summary>
    /// Bitdepth of the samples
    /// </summary>
    public byte SampleBitdepth { get; set; }
    /// <summary>
    /// Number of audio channels
    /// </summary>
    public byte ChannelCount { get; set; }
    /// <summary>
    /// Audio sample rate
    /// </summary>
    public uint SampleRate { get; set; }
    /// <summary>
    /// Total number of audio samples
    /// </summary>
    public uint TotalSamples { get; set; }
    /// <summary>
    /// The highpass frequency
    /// </summary>
    public ushort HighpassFrequency { get; set; }
    /// <summary>
    /// ADX version
    /// </summary>
    public byte Version { get; set; }
    /// <summary>
    /// Miscellaneous flags
    /// </summary>
    public byte Flags { get; set; }

    /// <summary>
    /// Loop info specific to ADX format
    /// </summary>
    public AdxVersion3LoopInfo LoopInfo = new();
    /// <summary>
    /// The header size
    /// </summary>
    public int HeaderSize { get; private set; }

    /// <summary>
    /// Creates an ADX header from data
    /// </summary>
    /// <param name="data">File data</param>
    /// <param name="log">ILogger instance for logging</param>
    public AdxHeader(byte[] data, ILogger log)
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

        LoopInfo = Version switch
        {
            3 when dataOffset >= 40 => new(data[0x14..0x2C]),
            4 when dataOffset >= 52 => new(data[0x20..0x38]),
            _ => LoopInfo
        };

        if (Encoding.ASCII.GetString(data.Skip(dataOffset - 2).Take(6).ToArray()) != "(c)CRI")
        {
            log.LogError("ADX file had bad copyright string.");
            return;
        }

        HeaderSize = dataOffset + 4;
    }
    /// <summary>
    /// Creates a blank ADX header
    /// </summary>
    public AdxHeader()
    {
    }

    /// <summary>
    /// Gets ADX header bytes
    /// </summary>
    /// <param name="headerSize">The size of the header in bytes</param>
    /// <returns>A list of bytes representing the ADX header in binary data</returns>
    public List<byte> GetBytes(int headerSize)
    {
        List<byte> bytes =
        [
            .. BigEndianIO.GetBytes(ADX_MAGIC),
            .. BigEndianIO.GetBytes((short)(headerSize - 0x04)),
            (byte)AdxEncoding,
            BlockSize,
            SampleBitdepth,
            ChannelCount,
            .. BigEndianIO.GetBytes(SampleRate),
            .. BigEndianIO.GetBytes(TotalSamples),
            .. BigEndianIO.GetBytes(HighpassFrequency),
            Version,
            Flags,
        ];
        if (Version == 3)
        {
            bytes.AddRange(LoopInfo.GetBytes());
        }
        bytes.AddRange(new byte[headerSize - bytes.Count - 0x06]);
        bytes.AddRange("(c)CRI"u8.ToArray());

        return bytes;
    }
}

/// <summary>
/// Loop info struct for ADX
/// </summary>
public struct AdxVersion3LoopInfo(byte[] bytes)
{
    /// <summary>
    /// Alignment samples
    /// </summary>
    public ushort AlignmentSamples { get; set; } = BigEndianIO.ReadUShort(bytes, 0);
    /// <summary>
    /// Whether looping is enabled (as a short)
    /// </summary>
    public ushort EnabledShort { get; set; } = BigEndianIO.ReadUShort(bytes, 0x02);
    /// <summary>
    /// Whether looping is enabled (as an int)
    /// </summary>
    public uint EnabledInt { get; set; } = BigEndianIO.ReadUInt(bytes, 0x04);
    /// <summary>
    /// The sample where the loop starts
    /// </summary>
    public uint BeginSample { get; set; } = BigEndianIO.ReadUInt(bytes, 0x08);
    /// <summary>
    /// The byte where the loop starts
    /// </summary>
    public uint BeginByte { get; set; } = BigEndianIO.ReadUInt(bytes, 0x0C);
    /// <summary>
    /// The sample where the loop ends
    /// </summary>
    public uint EndSample { get; set; } = BigEndianIO.ReadUInt(bytes, 0x10);
    /// <summary>
    /// The byte where the loop ends
    /// </summary>
    public uint EndByte { get; set; } = BigEndianIO.ReadUInt(bytes, 0x14);

    /// <summary>
    /// Gets binary data for loop info
    /// </summary>
    /// <returns>Loop info binary data</returns>
    public readonly List<byte> GetBytes()
    {
        List<byte> bytes =
        [
            .. BigEndianIO.GetBytes(AlignmentSamples),
            .. BigEndianIO.GetBytes(EnabledShort),
            .. BigEndianIO.GetBytes(EnabledInt),
            .. BigEndianIO.GetBytes(BeginSample),
            .. BigEndianIO.GetBytes(BeginByte),
            .. BigEndianIO.GetBytes(EndSample),
            .. BigEndianIO.GetBytes(EndByte),
        ];

        return bytes;
    }
}

/// <summary>
/// ADX encoding type
/// </summary>
public enum AdxEncoding : byte
{
    /// <summary>
    /// Preset
    /// </summary>
    Preset = 0x02,
    /// <summary>
    /// Standard
    /// </summary>
    Standard = 0x03,
    /// <summary>
    /// Exponential
    /// </summary>
    Exponential = 0x04,
    /// <summary>
    /// AHX 10
    /// </summary>
    Ahx10 = 0x10,
    /// <summary>
    /// AHX 11
    /// </summary>
    Ahx11 = 0x11,
}