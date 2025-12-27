using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
// ReSharper disable InconsistentNaming

// This code is ported from https://github.com/Isaac-Lozano/radx
namespace HaruhiChokuretsuLib.Audio.ADX;

/// <summary>
/// ADX encoder for the AHX subtype
/// </summary>
public class AhxEncoder : IAdxEncoder
{
    internal BitWriter Writer { get; set; }
    /// <summary>
    /// ADX specification data
    /// </summary>
    public AdxSpec Spec { get; set; }
    internal Window Win { get; set; }
    internal uint SamplesEncoded { get; set; }
    internal short[] Buffer { get; set; }
    internal uint BufferIndex { get; set; }

    private long[][] _n { get; set; }

    /// <summary>
    /// Create an AHX encoder from a BinaryWriter and a spec
    /// </summary>
    /// <param name="writer">BinaryWriter to use to write the file</param>
    /// <param name="spec">AHX spec</param>
    public AhxEncoder(BinaryWriter writer, AdxSpec spec)
    {
        writer.Seek(0x24, SeekOrigin.Begin);

        Writer = new(writer);
        Spec = spec;
        Win = new();
        SamplesEncoded = 0;
        Buffer = new short[1152];
        BufferIndex = 0;

        _n = new long[64][];
        for (int i = 0; i < _n.Length; i++)
        {
            _n[i] = new long[32];
            for (int j = 0; j < _n[i].Length; j++)
            {
                _n[i][j] = (long)(Math.Cos((16 + i) * ((j << 1) + 1) * 0.0490873852123405) * 268435456.0);
            }
        }
    }

    private void EncodeFrame()
    {
        Writer.Reset();

        // Write frame header
        Writer.Write(0xFFF5E0C0, 32);

        // Write bit allocations
        for (int i = 0; i < 4; i++)
        {
            Writer.Write(6, 4);
        }
        for (int i = 0; i < 2; i++)
        {
            Writer.Write(4, 3);
        }
        for (int i = 0; i < 5; i++)
        {
            Writer.Write(3, 3);
        }
        Writer.Write(3, 2);
        for (int i = 0; i < 18; i++)
        {
            Writer.Write(1, 2);
        }

        // 1 scfsi per subband
        int[] scfsi = new int[30];

        // 3 parts with 30 subbands
        int[][] scaleFactors = new int[3][];
        for (int i = 0; i < scaleFactors.Length; i++)
        {
            scaleFactors[i] = new int[30];
        }

        // 3 parts with 4 granules with 32 subbands with 3 samples
        long[][][][] polyphasedSamples = new long[3][][][];
        for (int i = 0; i < polyphasedSamples.Length; i++)
        {
            polyphasedSamples[i] = new long[4][][];
            for (int j = 0; j < polyphasedSamples[i].Length; j++)
            {
                polyphasedSamples[i][j] = new long[32][];
                for (int k = 0; k < polyphasedSamples[i][j].Length; k++)
                {
                    polyphasedSamples[i][j][k] = new long[3];
                }
            }
        }

        int sampleIndex = 0;

        for (int part = 0; part < 3; part++)
        {
            // Read in samples
            for (int gr = 0; gr < 4; gr++)
            {
                for (int s = 0; s < 3; s++)
                {
                    Win.AddSamples(Buffer[sampleIndex..(sampleIndex + 32)]);
                    long[] polyphased = Win.Polyphase(_n);
                    sampleIndex += 32;

                    for (int sb = 0; sb < 32; sb++)
                    {
                        polyphasedSamples[part][gr][sb][s] = polyphased[sb];
                    }
                }
            }

            // Analyze samples for scale factors
            for (int sb = 0; sb < 30; sb++)
            {
                long maxSample = 0;
                for (int gr = 0; gr < 4; gr++)
                {
                    for (int s = 0; s < 3; s++)
                    {
                        if (Math.Abs(polyphasedSamples[part][gr][sb][s]) > maxSample)
                        {
                            maxSample = Math.Abs(polyphasedSamples[part][gr][sb][s]);
                        }
                    }
                }

                // Find best scale factor
                int sfIndex = 0;
                for (int i = 0; i < 63; i++)
                {
                    sfIndex = 62 - i;
                    if (maxSample < SF_TABLE[sfIndex])
                    {
                        break;
                    }
                }

                scaleFactors[part][sb] = sfIndex;
            }
        }

        // Analyze scfsi info
        for (int sb = 0; sb < 30; sb++)
        {
            if (scaleFactors[0][sb] == scaleFactors[1][sb])
            {
                if (scaleFactors[1][sb] == scaleFactors[2][sb])
                {
                    // All scale factors the same
                    scfsi[sb] = 2;
                }
                else
                {
                    // First two same, last different
                    scfsi[sb] = 1;
                }
            }
            else
            {
                if (scaleFactors[1][sb] == scaleFactors[2][sb])
                {
                    // Last two same, first different
                    scfsi[sb] = 3;
                }
                else
                {
                    // None same
                    scfsi[sb] = 0;
                }
            }
        }

        // Write scfsi information
        for (int sb = 0; sb < 30; sb++)
        {
            Writer.Write((uint)scfsi[sb], 2);
        }

        // Write scale factor information
        for (int sb = 0; sb < 30; sb++)
        {
            switch (scfsi[sb])
            {
                case 0:
                    // None the same, write all three scalefactors
                    Writer.Write((uint)scaleFactors[0][sb], 6);
                    Writer.Write((uint)scaleFactors[1][sb], 6);
                    Writer.Write((uint)scaleFactors[2][sb], 6);
                    break;

                case 1:
                case 3:
                    Writer.Write((uint)scaleFactors[0][sb], 6);
                    Writer.Write((uint)scaleFactors[2][sb], 6);
                    break;

                case 2:
                    // All scalefactors the same, write the first
                    Writer.Write((uint)scaleFactors[0][sb], 6);
                    break;
            }
        }

        // Write sample data
        for (int part = 0; part < 3; part++)
        {
            for (int gr = 0; gr < 4; gr++)
            {
                for (int sb = 0; sb < 30; sb++)
                {
                    QuantSpec quant = QUANT_TABLE[sb];

                    long[] quantizedSamples = new long[3];

                    for (int s = 0; s < 3; s++)
                    {
                        long scaled = polyphasedSamples[part][gr][sb][s] * ISF_TABLE[scaleFactors[part][sb]] >> 28;
                        long transformed = (scaled * quant.A >> 28) + quant.B;
                        long quantized = transformed >> (int)(28 - (quant.NumBits - 1));
                        long formatted = quantized & (1 << (int)quant.NumBits) - 1 ^ 1 << (int)quant.NumBits - 1;
                        quantizedSamples[s] = formatted;
                    }

                    GroupSpec? groupSpec = quant.GroupSpecifier;
                    if (groupSpec.HasValue)
                    {
                        long grouped = quantizedSamples[0] + quantizedSamples[1] * groupSpec.Value.NLevels + quantizedSamples[2] * (long)Math.Pow(groupSpec.Value.NLevels, 2);
                        Writer.Write((uint)grouped, (int)groupSpec.Value.GroupBits);
                    }
                    else
                    {
                        for (int s = 0; s < 3; s++)
                        {
                            Writer.Write((uint)quantizedSamples[s], (int)quant.NumBits);
                        }
                    }
                }
            }
        }
    }

    /// <inheritdoc/>
    public void EncodeData(IEnumerable<Sample> samples, CancellationToken cancellationToken)
    {
        foreach (Sample sample in samples)
        {
            foreach (short channelSample in sample)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                Buffer[BufferIndex++] = channelSample;

                if (BufferIndex == 1152)
                {
                    EncodeFrame();
                    BufferIndex = 0;
                }
                SamplesEncoded++;
            }
        }
    }

    /// <inheritdoc/>
    public void Finish()
    {
        if (BufferIndex != 0)
        {
            for (uint i = BufferIndex; i < Buffer.Length; i++)
            {
                Buffer[i] = 0;
            }
            EncodeFrame();
        }

        BinaryWriter w = Writer.Inner();
        w.Write("\x00\x80\x01\x00\x00" + "AHXE(c)CRI" + "\x00\x00");

        AdxHeader header = new()
        {
            AdxEncoding = AdxEncoding.Ahx10,
            BlockSize = 0,
            SampleBitdepth = 0,
            ChannelCount = (byte)Spec.Channels,
            SampleRate = Spec.SampleRate,
            TotalSamples = SamplesEncoded,
            HighpassFrequency = 0,
            Version = 6,
            Flags = 0,
        };

        w.Seek(0, SeekOrigin.Begin);
        w.Write(header.GetBytes(0x24).ToArray());
        w.Flush();
    }

    internal class Window
    {
        public short[] Values { get; set; }
        public uint Index { get; set; }

        public Window()
        {
            Values = new short[512];
            Index = 0;
        }

        public short this[int index]
        {
            get => Values[(Index + index) % 512];
            set => Values[(Index + index) % 512] = value;
        }

        public void AddSamples(short[] samples)
        {
            samples.CopyTo(Values, Index);

            Index += 32;
            Index %= 512;
        }

        public long[] Polyphase(long[][] n)
        {
            long[] polyphased = new long[32];

            long[] y = new long[64];

            // Precompute Y since it doesn't rely on subband
            for (int i = 0; i < 64; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    // Window the sample
                    // (15b * 28b) >> 15 = 28b
                    y[i] += this[i + 64 * j] * ENWINDOW[i + 64 * j] >> 15;
                }
            }

            // Now do polyphase filter
            for (int sb = 0; sb < 32; sb++)
            {
                for (int i = 0; i < 64; i++)
                {
                    polyphased[sb] += n[i][sb] * y[i] >> 28;
                }
            }

            return polyphased;
        }
    }

    private static readonly long[] ENWINDOW =
    [
        0x000000,-0x000080,-0x000080,-0x000080,-0x000080,-0x000080,-0x000080,-0x000100,-0x000100,-0x000100,-0x000100,-0x000180,-0x000180,-0x000200,-0x000200,-0x000280,
        -0x000280,-0x000300,-0x000380,-0x000380,-0x000400,-0x000480,-0x000500,-0x000580,-0x000680,-0x000700,-0x000800,-0x000880,-0x000980,-0x000A80,-0x000C00,-0x000D00,
        -0x000E80,-0x000F80,-0x001180,-0x001300,-0x001480,-0x001680,-0x001880,-0x001A80,-0x001D00,-0x001F80,-0x002200,-0x002480,-0x002780,-0x002A80,-0x002D80,-0x003080,
        -0x003400,-0x003780,-0x003A80,-0x003E80,-0x004200,-0x004580,-0x004980,-0x004D00,-0x005080,-0x005480,-0x005800,-0x005B80,-0x005F00,-0x006200,-0x006500,-0x006800,
        0x006A80, 0x006D00, 0x006F00, 0x007080, 0x007180, 0x007200, 0x007200, 0x007180, 0x007000, 0x006E80, 0x006B80, 0x006800, 0x006400, 0x005E80, 0x005880, 0x005180,
        0x004900, 0x003F80, 0x003500, 0x002980, 0x001C80, 0x000E80,-0x000100,-0x001200,-0x002400,-0x003780,-0x004C80,-0x006280,-0x007A00,-0x009300,-0x00AD80,-0x00C880,
        -0x00E580,-0x010380,-0x012280,-0x014280,-0x016380,-0x018580,-0x01A800,-0x01CB80,-0x01EF80,-0x021400,-0x023880,-0x025D00,-0x028180,-0x02A600,-0x02CA00,-0x02ED00,
        -0x030F80,-0x033100,-0x035100,-0x036F80,-0x038C80,-0x03A700,-0x03BF80,-0x03D500,-0x03E880,-0x03F800,-0x040480,-0x040D80,-0x041280,-0x041380,-0x041000,-0x040780,
        0x03FA80, 0x03E800, 0x03D000, 0x03B280, 0x038F00, 0x036580, 0x033600, 0x02FF80, 0x02C300, 0x028000, 0x023580, 0x01E500, 0x018D00, 0x012E80, 0x00C900, 0x005C80,
        -0x001680,-0x009000,-0x011080,-0x019700,-0x022380,-0x02B600,-0x034E00,-0x03EB00,-0x048D00,-0x053380,-0x05DE00,-0x068B80,-0x073C80,-0x07EF80,-0x08A480,-0x095A00,
        -0x0A1080,-0x0AC680,-0x0B7B80,-0x0C2E80,-0x0CDE80,-0x0D8B80,-0x0E3380,-0x0ED680,-0x0F7300,-0x100880,-0x109580,-0x111980,-0x119300,-0x120180,-0x126400,-0x12B880,
        -0x12FF80,-0x133700,-0x135E00,-0x137380,-0x137700,-0x136780,-0x134380,-0x130B00,-0x12BC00,-0x125680,-0x11D980,-0x114400,-0x109600,-0x0FCE00,-0x0EEC00,-0x0DEF00,
        0x0CD700, 0x0BA380, 0x0A5400, 0x08E880, 0x076000, 0x05BB80, 0x03FA80, 0x021D00, 0x002300,-0x01F300,-0x042500,-0x067200,-0x08DA80,-0x0B5D00,-0x0DF900,-0x10AE00,
        -0x137B80,-0x165F80,-0x195A00,-0x1C6A00,-0x1F8D80,-0x22C380,-0x260B00,-0x296280,-0x2CC880,-0x303B00,-0x33B900,-0x374080,-0x3AD000,-0x3E6580,-0x41FF80,-0x459C00,
        -0x493880,-0x4CD400,-0x506C00,-0x53FF00,-0x578A80,-0x5B0C80,-0x5E8300,-0x61EC80,-0x654680,-0x688F00,-0x6BC500,-0x6EE500,-0x71EE80,-0x74DF00,-0x77B480,-0x7A6E00,
        -0x7D0980,-0x7F8500,-0x81DF00,-0x841680,-0x862A00,-0x881780,-0x89DF00,-0x8B7E00,-0x8CF480,-0x8E4180,-0x8F6380,-0x905A00,-0x912480,-0x91C300,-0x923400,-0x927800,
        0x928F00, 0x927800, 0x923400, 0x91C300, 0x912480, 0x905A00, 0x8F6380, 0x8E4180, 0x8CF480, 0x8B7E00, 0x89DF00, 0x881780, 0x862A00, 0x841680, 0x81DF00, 0x7F8500,
        0x7D0980, 0x7A6E00, 0x77B480, 0x74DF00, 0x71EE80, 0x6EE500, 0x6BC500, 0x688F00, 0x654680, 0x61EC80, 0x5E8300, 0x5B0C80, 0x578A80, 0x53FF00, 0x506C00, 0x4CD400,
        0x493880, 0x459C00, 0x41FF80, 0x3E6580, 0x3AD000, 0x374080, 0x33B900, 0x303B00, 0x2CC880, 0x296280, 0x260B00, 0x22C380, 0x1F8D80, 0x1C6A00, 0x195A00, 0x165F80,
        0x137B80, 0x10AE00, 0x0DF900, 0x0B5D00, 0x08DA80, 0x067200, 0x042500, 0x01F300,-0x002300,-0x021D00,-0x03FA80,-0x05BB80,-0x076000,-0x08E880,-0x0A5400,-0x0BA380,
        0x0CD700, 0x0DEF00, 0x0EEC00, 0x0FCE00, 0x109600, 0x114400, 0x11D980, 0x125680, 0x12BC00, 0x130B00, 0x134380, 0x136780, 0x137700, 0x137380, 0x135E00, 0x133700,
        0x12FF80, 0x12B880, 0x126400, 0x120180, 0x119300, 0x111980, 0x109580, 0x100880, 0x0F7300, 0x0ED680, 0x0E3380, 0x0D8B80, 0x0CDE80, 0x0C2E80, 0x0B7B80, 0x0AC680,
        0x0A1080, 0x095A00, 0x08A480, 0x07EF80, 0x073C80, 0x068B80, 0x05DE00, 0x053380, 0x048D00, 0x03EB00, 0x034E00, 0x02B600, 0x022380, 0x019700, 0x011080, 0x009000,
        0x001680,-0x005C80,-0x00C900,-0x012E80,-0x018D00,-0x01E500,-0x023580,-0x028000,-0x02C300,-0x02FF80,-0x033600,-0x036580,-0x038F00,-0x03B280,-0x03D000,-0x03E800,
        0x03FA80, 0x040780, 0x041000, 0x041380, 0x041280, 0x040D80, 0x040480, 0x03F800, 0x03E880, 0x03D500, 0x03BF80, 0x03A700, 0x038C80, 0x036F80, 0x035100, 0x033100,
        0x030F80, 0x02ED00, 0x02CA00, 0x02A600, 0x028180, 0x025D00, 0x023880, 0x021400, 0x01EF80, 0x01CB80, 0x01A800, 0x018580, 0x016380, 0x014280, 0x012280, 0x010380,
        0x00E580, 0x00C880, 0x00AD80, 0x009300, 0x007A00, 0x006280, 0x004C80, 0x003780, 0x002400, 0x001200, 0x000100,-0x000E80,-0x001C80,-0x002980,-0x003500,-0x003F80,
        -0x004900,-0x005180,-0x005880,-0x005E80,-0x006400,-0x006800,-0x006B80,-0x006E80,-0x007000,-0x007180,-0x007200,-0x007200,-0x007180,-0x007080,-0x006F00,-0x006D00,
        0x006A80, 0x006800, 0x006500, 0x006200, 0x005F00, 0x005B80, 0x005800, 0x005480, 0x005080, 0x004D00, 0x004980, 0x004580, 0x004200, 0x003E80, 0x003A80, 0x003780,
        0x003400, 0x003080, 0x002D80, 0x002A80, 0x002780, 0x002480, 0x002200, 0x001F80, 0x001D00, 0x001A80, 0x001880, 0x001680, 0x001480, 0x001300, 0x001180, 0x000F80,
        0x000E80, 0x000D00, 0x000C00, 0x000A80, 0x000980, 0x000880, 0x000800, 0x000700, 0x000680, 0x000580, 0x000500, 0x000480, 0x000400, 0x000380, 0x000380, 0x000300,
        0x000280, 0x000280, 0x000200, 0x000200, 0x000180, 0x000180, 0x000100, 0x000100, 0x000100, 0x000100, 0x000080, 0x000080, 0x000080, 0x000080, 0x000080, 0x000080,
    ];

    private static readonly long[] SF_TABLE =
    [
        0x20000000,
        0x1965fea5,
        0x1428a2fa,
        0x10000000,
        0x0cb2ff53,
        0x0a14517d,
        0x08000000,
        0x06597fa9,
        0x050a28be,
        0x04000000,
        0x032cbfd5,
        0x0285145f,
        0x02000000,
        0x01965fea,
        0x01428a30,
        0x01000000,
        0x00cb2ff5,
        0x00a14518,
        0x00800000,
        0x006597fb,
        0x0050a28c,
        0x00400000,
        0x0032cbfd,
        0x00285146,
        0x00200000,
        0x001965ff,
        0x001428a3,
        0x00100000,
        0x000cb2ff,
        0x000a1451,
        0x00080000,
        0x00065980,
        0x00050a29,
        0x00040000,
        0x00032cc0,
        0x00028514,
        0x00020000,
        0x00019660,
        0x0001428a,
        0x00010000,
        0x0000cb30,
        0x0000a145,
        0x00008000,
        0x00006598,
        0x000050a3,
        0x00004000,
        0x000032cc,
        0x00002851,
        0x00002000,
        0x00001966,
        0x00001429,
        0x00001000,
        0x00000cb3,
        0x00000a14,
        0x00000800,
        0x00000659,
        0x0000050a,
        0x00000400,
        0x0000032d,
        0x00000285,
        0x00000200,
        0x00000196,
        0x00000143,
    ];

    private static readonly long[] ISF_TABLE =
    [
        0x00000008000000,
        0x0000000A14517C,
        0x0000000CB2FF52,
        0x00000010000000,
        0x0000001428A2F8,
        0x0000001965FEA4,
        0x00000020000000,
        0x000000285145F5,
        0x00000032CBFD4E,
        0x00000040000000,
        0x00000050A28BDD,
        0x0000006597FA9C,
        0x00000080000000,
        0x000000A14517ED,
        0x000000CB2FF4E8,
        0x00000100000000,
        0x000001428A2FDB,
        0x000001965FE9D1,
        0x00000200000000,
        0x00000285145C8A,
        0x0000032CBFD3A3,
        0x00000400000000,
        0x0000050A28C5C7,
        0x000006597FA747,
        0x00000800000000,
        0x00000A145158C2,
        0x00000CB2FF4E8E,
        0x00001000000000,
        0x00001428A37CB4,
        0x00001965FFDFA8,
        0x00002000000000,
        0x0000285143CCA8,
        0x000032CBFAB527,
        0x00004000000000,
        0x000050A2879951,
        0x000065980992F3,
        0x00008000000000,
        0x0000A1450F32A2,
        0x0000CB301325E7,
        0x00010000000000,
        0x0001428A1E6544,
        0x00019660264BCF,
        0x00020000000000,
        0x000285143CCA88,
        0x00032CBB427564,
        0x00040000000000,
        0x00050A28799510,
        0x0006598AAD93B4,
        0x00080000000000,
        0x000A1450F32A20,
        0x000CB2C4B983B2,
        0x00100000000000,
        0x001428A1E65441,
        0x001966CC01966C,
        0x00200000000000,
        0x00285470CC2B7B,
        0x0032CD98032CD9,
        0x00400000000000,
        0x00509C2E9A4AF1,
        0x00659B300659B3,
        0x00800000000000,
        0x00A16B312EA8FC,
        0x00CAE5D85F1BBD,
    ];

    private struct GroupSpec
    {
        public int NLevels { get; set; }
        public uint GroupBits { get; set; }
    }

    private struct QuantSpec
    {
        public long A { get; set; }
        public long B { get; set; }
        public uint NumBits { get; set; }
        public GroupSpec? GroupSpecifier { get; set; }
    }

    private static readonly QuantSpec[] QUANT_TABLE =
    [
        new() { A = 0x0F800000, B = -0x00800000, NumBits = 5, GroupSpecifier = null, },
        new() { A = 0x0F800000, B = -0x00800000, NumBits = 5, GroupSpecifier = null, },
        new() { A = 0x0F800000, B = -0x00800000, NumBits = 5, GroupSpecifier = null, },
        new() { A = 0x0F800000, B = -0x00800000, NumBits = 5, GroupSpecifier = null, },
        new() { A = 0x0F000000, B = -0x01000000, NumBits = 4, GroupSpecifier = null, },
        new() { A = 0x0F000000, B = -0x01000000, NumBits = 4, GroupSpecifier = null, },
        new() { A = 0x09000000, B = -0x07000000, NumBits = 4, GroupSpecifier = new() { NLevels =  9, GroupBits = 10, }, },
        new() { A = 0x09000000, B = -0x07000000, NumBits = 4, GroupSpecifier = new() { NLevels =  9, GroupBits = 10, }, },
        new() { A = 0x09000000, B = -0x07000000, NumBits = 4, GroupSpecifier = new() { NLevels =  9, GroupBits = 10, }, },
        new() { A = 0x09000000, B = -0x07000000, NumBits = 4, GroupSpecifier = new() { NLevels =  9, GroupBits = 10, }, },
        new() { A = 0x09000000, B = -0x07000000, NumBits = 4, GroupSpecifier = new() { NLevels =  9, GroupBits = 10, }, },
        new() { A = 0x09000000, B = -0x07000000, NumBits = 4, GroupSpecifier = new() { NLevels =  9, GroupBits = 10, }, },
        new() { A = 0x0C000000, B = -0x04000000, NumBits = 2, GroupSpecifier = new() { NLevels =  3, GroupBits = 5, }, },
        new() { A = 0x0C000000, B = -0x04000000, NumBits = 2, GroupSpecifier = new() { NLevels =  3, GroupBits = 5, }, },
        new() { A = 0x0C000000, B = -0x04000000, NumBits = 2, GroupSpecifier = new() { NLevels =  3, GroupBits = 5, }, },
        new() { A = 0x0C000000, B = -0x04000000, NumBits = 2, GroupSpecifier = new() { NLevels =  3, GroupBits = 5, }, },
        new() { A = 0x0C000000, B = -0x04000000, NumBits = 2, GroupSpecifier = new() { NLevels =  3, GroupBits = 5, }, },
        new() { A = 0x0C000000, B = -0x04000000, NumBits = 2, GroupSpecifier = new() { NLevels =  3, GroupBits = 5, }, },
        new() { A = 0x0C000000, B = -0x04000000, NumBits = 2, GroupSpecifier = new() { NLevels =  3, GroupBits = 5, }, },
        new() { A = 0x0C000000, B = -0x04000000, NumBits = 2, GroupSpecifier = new() { NLevels =  3, GroupBits = 5, }, },
        new() { A = 0x0C000000, B = -0x04000000, NumBits = 2, GroupSpecifier = new() { NLevels =  3, GroupBits = 5, }, },
        new() { A = 0x0C000000, B = -0x04000000, NumBits = 2, GroupSpecifier = new() { NLevels =  3, GroupBits = 5, }, },
        new() { A = 0x0C000000, B = -0x04000000, NumBits = 2, GroupSpecifier = new() { NLevels =  3, GroupBits = 5, }, },
        new() { A = 0x0C000000, B = -0x04000000, NumBits = 2, GroupSpecifier = new() { NLevels =  3, GroupBits = 5, }, },
        new() { A = 0x0C000000, B = -0x04000000, NumBits = 2, GroupSpecifier = new() { NLevels =  3, GroupBits = 5, }, },
        new() { A = 0x0C000000, B = -0x04000000, NumBits = 2, GroupSpecifier = new() { NLevels =  3, GroupBits = 5, }, },
        new() { A = 0x0C000000, B = -0x04000000, NumBits = 2, GroupSpecifier = new() { NLevels =  3, GroupBits = 5, }, },
        new() { A = 0x0C000000, B = -0x04000000, NumBits = 2, GroupSpecifier = new() { NLevels =  3, GroupBits = 5, }, },
        new() { A = 0x0C000000, B = -0x04000000, NumBits = 2, GroupSpecifier = new() { NLevels =  3, GroupBits = 5, }, },
        new() { A = 0x0C000000, B = -0x04000000, NumBits = 2, GroupSpecifier = new() { NLevels =  3, GroupBits = 5, }, },
    ];
}