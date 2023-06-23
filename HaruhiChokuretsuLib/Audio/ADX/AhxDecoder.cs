using HaruhiChokuretsuLib.Util;
using System;
using System.Collections.Generic;
using System.Linq;

// This code is ported from https://github.com/Isaac-Lozano/radx
// But modified to work with more AHX files and to fix some bugs
namespace HaruhiChokuretsuLib.Audio.ADX
{
    public struct QuantizeSpec
    {
        public long NLevels { get; set; }
        public uint Group { get; set; }
        public uint Bits { get; set; }
        public uint C { get; set; }
        public uint D { get; set; }
    }

    public class AhxDecoder : IAdxDecoder
    {
        public AdxHeader Header { get; set; }
        public List<byte> Data { get; set; }
        public uint VOffset { get; set; }
        public long[] U { get; set; } = new long[512];
        public long[] V { get; set; } = new long[1024];
        public int CurrentSample { get; set; }
        public List<Sample> Samples { get; set; } = new();
        public uint Channels => Header.ChannelCount;
        public uint SampleRate => Header.SampleRate;
        public LoopInfo LoopInfo => null;

        private int _currentOffset = 0;
        private int _currentBit = 0;
        private ILogger _log;

        public AhxDecoder(IEnumerable<byte> data, ILogger log)
        {
            _log = log;
            Header = new(data, log);
            Data = data.ToList();
            _currentOffset = Header.HeaderSize;

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

        private long[] ReadSamples(QuantizeSpec quant)
        {
            long[] samples = new long[3];
            uint numBits;

            if (quant.Group != 0)
            {
                numBits = quant.Group;
                long grouped = BigEndianIO.ReadBits(Data, _currentOffset, _currentBit, (int)quant.Bits);
                _currentBit += (int)quant.Bits;

                for (int index = 0; index < samples.Length; index++)
                {
                    samples[index] = grouped % quant.NLevels;
                    grouped /= quant.NLevels;
                }
            }
            else
            {
                numBits = quant.Bits;

                for (int index = 0; index < samples.Length; index++)
                {
                    samples[index] = BigEndianIO.ReadBits(Data, _currentOffset, _currentBit, (int)numBits);
                    _currentBit += (int)numBits;
                }
            }

            for (int index = 0; index < samples.Length; index++)
            {
                long requantized = samples[index] ^ (uint)(1 << (int)(numBits - 1));
                requantized |= -(requantized & (uint)(1 << (int)(numBits - 1)));

                requantized <<= (int)(FRAC_BITS - (numBits - 1));

                samples[index] = (requantized + quant.D) * quant.C >> FRAC_BITS;
            }

            return samples;
        }

        public Sample[] ReadFrame()
        {
            Sample[] pcm = new Sample[1152];

            uint frameHeader = BigEndianIO.ReadUInt(Data, _currentOffset);
            _currentOffset += 4;
            if (frameHeader == 0x00800100)
            {
                return null;
            }
            else if (frameHeader != 0xfff5e0c0)
            {
                _log.LogError("Bad AHX frame header detected.");
                return null;
            }

            //peak next 10 bytes to see if they're zero; we know to skip this frame if so
            if (Data.Skip(_currentOffset).Take(10).All(b => b == 0))
            {
                _currentOffset += 10;
                return new short[1152].Select(s => new Sample(new short[] { s })).ToArray();
            }

            uint[] allocations = new uint[30];
            for (int sb = 0; sb < allocations.Length; sb++)
            {
                allocations[sb] = BigEndianIO.ReadBits(Data, _currentOffset, _currentBit, _bitAllocTable[sb]);
                _currentBit += _bitAllocTable[sb];
            }

            uint[] scfsi = new uint[30];
            for (int sb = 0; sb < scfsi.Length; sb++)
            {
                if (allocations[sb] != 0)
                {
                    scfsi[sb] = BigEndianIO.ReadBits(Data, _currentOffset, _currentBit, 2);
                    _currentBit += 2;
                }
            }

            uint[][] scaleFactors = new uint[30][];
            for (int sb = 0; sb < scaleFactors.Length; sb++)
            {
                scaleFactors[sb] = new uint[3];
                if (allocations[sb] != 0)
                {
                    switch (scfsi[sb])
                    {
                        case 0:
                            scaleFactors[sb][0] = BigEndianIO.ReadBits(Data, _currentOffset, _currentBit, 6);
                            _currentBit += 6;
                            scaleFactors[sb][1] = BigEndianIO.ReadBits(Data, _currentOffset, _currentBit, 6);
                            _currentBit += 6;
                            scaleFactors[sb][2] = BigEndianIO.ReadBits(Data, _currentOffset, _currentBit, 6);
                            _currentBit += 6;
                            break;

                        case 1:
                            uint temp1 = BigEndianIO.ReadBits(Data, _currentOffset, _currentBit, 6);
                            _currentBit += 6;
                            scaleFactors[sb][0] = temp1;
                            scaleFactors[sb][1] = temp1;
                            scaleFactors[sb][2] = BigEndianIO.ReadBits(Data, _currentOffset, _currentBit, 6);
                            _currentBit += 6;
                            break;

                        case 2:
                            uint temp2 = BigEndianIO.ReadBits(Data, _currentOffset, _currentBit, 6);
                            _currentBit += 6;
                            scaleFactors[sb][0] = temp2;
                            scaleFactors[sb][1] = temp2;
                            scaleFactors[sb][2] = temp2;
                            break;

                        case 3:
                            scaleFactors[sb][0] = BigEndianIO.ReadBits(Data, _currentOffset, _currentBit, 6);
                            _currentBit += 6;
                            uint temp3 = BigEndianIO.ReadBits(Data, _currentOffset, _currentBit, 6);
                            _currentBit += 6;
                            scaleFactors[sb][1] = temp3;
                            scaleFactors[sb][2] = temp3;
                            break;

                        default:
                            _log.LogWarning("Somehow reached an unreachable state during scalefactors allocation in AHX decoding.");
                            break;
                    }
                }
            }

            for (int part = 0; part < 3; part++)
            {
                for (int gr = 0; gr < 4; gr++)
                {
                    long[][] sbSamples = new long[32][];
                    for (int i = 0; i < sbSamples.Length; i++)
                    {
                        sbSamples[i] = new long[3];
                    }

                    for (int sb = 0; sb < allocations.Length; sb++)
                    {
                        if (allocations[sb] != 0)
                        {
                            QuantizeSpec quant = sb < 4 ? _quantTableLow[allocations[sb] - 1] : _quantTableHigh[allocations[sb] - 1];

                            long[] longSamples = ReadSamples(quant);
                            for (int i = 0; i < longSamples.Length; i++)
                            {
                                sbSamples[sb][i] = longSamples[i] * _sfTable[scaleFactors[sb][part]] >> FRAC_BITS;
                            }
                        }
                    }

                    for (int index = 0; index < 3; index++)
                    {
                        uint tableIndex = (VOffset - 64) % 1024;
                        VOffset = tableIndex;

                        for (int i = 0; i < 64; i++)
                        {
                            long sum = 0;
                            for (int j = 0; j < 32; j++)
                            {
                                sum += _n[i][j] * sbSamples[j][index] >> FRAC_BITS;
                            }

                            V[tableIndex + i] = sum;

                            for (int j = 0; j < 8; j++)
                            {
                                for (int sb = 0; sb < 32; sb++)
                                {
                                    U[j * 64 + sb] = V[(tableIndex + j * 128 + sb) % 1024];
                                    U[j * 64 + sb + 32] = V[(tableIndex + j * 128 + sb + 96) % 1024];
                                }
                            }

                            for (int j = 0; j < 512; j++)
                            {
                                U[j] = U[j] * _d[j] >> FRAC_BITS;
                            }

                            for (int sb = 0; sb < 32; sb++)
                            {
                                sum = 0;
                                for (int j = 0; j < 16; j++)
                                {
                                    sum -= U[j * 32 + sb];
                                }

                                sum >>= FRAC_BITS - 15;

                                if (sum > short.MaxValue)
                                {
                                    sum = short.MaxValue;
                                }
                                else if (sum < short.MinValue)
                                {
                                    sum = short.MinValue;
                                }

                                pcm[part * 384 + gr * 96 + index * 32 + sb] = new Sample(new short[] { (short)sum });
                            }
                        }
                    }
                }
            }
            _currentOffset += _currentBit / 8;
            if (_currentBit > 0)
            {
                _currentOffset++;
                _currentBit = 0;
            }

            return pcm;
        }

        public Sample NextSample()
        {
            if (CurrentSample == Samples.Count)
            {
                Sample[] nextFrame = ReadFrame();
                if (nextFrame is not null)
                {
                    Samples.AddRange(nextFrame);
                }
                else
                {
                    return null;
                }
            }

            if (CurrentSample == Header.TotalSamples)
            {
                return null;
            }

            return Samples[CurrentSample++];
        }

        private const int FRAC_BITS = 28;
        private static readonly int[] _bitAllocTable = new int[]
        {
            4, 4, 4, 4,
            3, 3, 3, 3, 3, 3, 3,
            2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2
        };
        private static readonly QuantizeSpec[] _quantTableLow = new QuantizeSpec[16]
        {
            new() { NLevels = 3, Group = 2, Bits = 5, C = 0x15555555, D = 0x08000000 },
            new() { NLevels = 5, Group = 4, Bits = 7, C = 0x1999999A, D = 0x08000000 },
            new() { NLevels = 7, Group = 0, Bits = 3, C = 0x12492492, D = 0x04000000 },
            new() { NLevels = 9, Group = 4, Bits = 10, C = 0x1c71c71c, D = 0x08000000 },
            new() { NLevels = 15, Group = 0, Bits = 4, C = 0x11111111, D = 0x02000000 },
            new() { NLevels = 31, Group = 0, Bits = 5, C = 0x10842108, D = 0x01000000 },
            new() { NLevels = 63, Group = 0, Bits = 6, C = 0x10410410, D = 0x00800000 },
            new() { NLevels = 127, Group = 0, Bits = 7, C = 0x10204081, D = 0x00400000 },
            new() { NLevels = 255, Group = 0, Bits = 8, C = 0x10101010, D = 0x00200000 },
            new() { NLevels = 511, Group = 0, Bits = 9, C = 0x10080402, D = 0x00100000 },
            new() { NLevels = 1023, Group = 0, Bits = 10, C = 0x10040100, D = 0x00080000 },
            new() { NLevels = 2047, Group = 0, Bits = 11, C = 0x10020040, D = 0x00040000 },
            new() { NLevels = 4095, Group = 0, Bits = 12, C = 0x10010010, D = 0x00020000 },
            new() { NLevels = 8191, Group = 0, Bits = 13, C = 0x10008004, D = 0x00010000 },
            new() { NLevels = 16383, Group = 0, Bits = 14, C = 0x10004001, D = 0x00008000 },
            new() { NLevels = 32767, Group = 0, Bits = 15, C = 0x10002000, D = 0x00004000 },
        };
        private static readonly QuantizeSpec[] _quantTableHigh = new QuantizeSpec[16]
        {
            new() { NLevels = 3, Group = 2, Bits = 5, C = 0x15555555, D = 0x08000000 },
            new() { NLevels = 5, Group = 4, Bits = 7, C = 0x1999999A, D = 0x08000000 },
            new() { NLevels = 9, Group = 4, Bits = 10, C = 0x1C71C71C, D = 0x08000000 },
            new() { NLevels = 15, Group = 0, Bits = 4, C = 0x11111111, D = 0x02000000 },
            new() { NLevels = 31, Group = 0, Bits = 5, C = 0x10842108, D = 0x01000000 },
            new() { NLevels = 63, Group = 0, Bits = 6, C = 0x10410410, D = 0x00800000 },
            new() { NLevels = 127, Group = 0, Bits = 7, C = 0x10204081, D = 0x00400000 },
            new() { NLevels = 255, Group = 0, Bits = 8, C = 0x10101010, D = 0x00200000 },
            new() { NLevels = 511, Group = 0, Bits = 9, C = 0x10080402, D = 0x00100000 },
            new() { NLevels = 1023, Group = 0, Bits = 10, C = 0x10040100, D = 0x00080000 },
            new() { NLevels = 2047, Group = 0, Bits = 11, C = 0x10020040, D = 0x00040000 },
            new() { NLevels = 4095, Group = 0, Bits = 12, C = 0x10010010, D = 0x00020000 },
            new() { NLevels = 8191, Group = 0, Bits = 13, C = 0x10008004, D = 0x00010000 },
            new() { NLevels = 16383, Group = 0, Bits = 14, C = 0x10004001, D = 0x00008000 },
            new() { NLevels = 32767, Group = 0, Bits = 15, C = 0x10002000, D = 0x00004000 },
            new() { NLevels = 65535, Group = 0, Bits = 16, C = 0x10001000, D = 0x00002000 },
        };
        private static readonly long[] _sfTable = new long[64]
        {
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
            0,
        };
        private readonly long[][] _n;
        private static readonly long[] _d = new long[512]
        {
             0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000,-0x00001000,
            -0x00001000,-0x00001000,-0x00001000,-0x00002000,-0x00002000,-0x00003000,-0x00003000,-0x00004000,
            -0x00004000,-0x00005000,-0x00006000,-0x00006000,-0x00007000,-0x00008000,-0x00009000,-0x0000A000,
            -0x0000C000,-0x0000D000,-0x0000F000,-0x00010000,-0x00012000,-0x00014000,-0x00017000,-0x00019000,
            -0x0001C000,-0x0001E000,-0x00022000,-0x00025000,-0x00028000,-0x0002C000,-0x00030000,-0x00034000,
            -0x00039000,-0x0003E000,-0x00043000,-0x00048000,-0x0004E000,-0x00054000,-0x0005A000,-0x00060000,
            -0x00067000,-0x0006E000,-0x00074000,-0x0007C000,-0x00083000,-0x0008A000,-0x00092000,-0x00099000,
            -0x000A0000,-0x000A8000,-0x000AF000,-0x000B6000,-0x000BD000,-0x000C3000,-0x000C9000,-0x000CF000,
             0x000D5000, 0x000DA000, 0x000DE000, 0x000E1000, 0x000E3000, 0x000E4000, 0x000E4000, 0x000E3000,
             0x000E0000, 0x000DD000, 0x000D7000, 0x000D0000, 0x000C8000, 0x000BD000, 0x000B1000, 0x000A3000,
             0x00092000, 0x0007F000, 0x0006A000, 0x00053000, 0x00039000, 0x0001D000,-0x00001000,-0x00023000,
            -0x00047000,-0x0006E000,-0x00098000,-0x000C4000,-0x000F3000,-0x00125000,-0x0015A000,-0x00190000,
            -0x001CA000,-0x00206000,-0x00244000,-0x00284000,-0x002C6000,-0x0030A000,-0x0034F000,-0x00396000,
            -0x003DE000,-0x00427000,-0x00470000,-0x004B9000,-0x00502000,-0x0054B000,-0x00593000,-0x005D9000,
            -0x0061E000,-0x00661000,-0x006A1000,-0x006DE000,-0x00718000,-0x0074D000,-0x0077E000,-0x007A9000,
            -0x007D0000,-0x007EF000,-0x00808000,-0x0081A000,-0x00824000,-0x00826000,-0x0081F000,-0x0080E000,
             0x007F5000, 0x007D0000, 0x007A0000, 0x00765000, 0x0071E000, 0x006CB000, 0x0066C000, 0x005FF000,
             0x00586000, 0x00500000, 0x0046B000, 0x003CA000, 0x0031A000, 0x0025D000, 0x00192000, 0x000B9000,
            -0x0002C000,-0x0011F000,-0x00220000,-0x0032D000,-0x00446000,-0x0056B000,-0x0069B000,-0x007D5000,
            -0x00919000,-0x00A66000,-0x00BBB000,-0x00D16000,-0x00E78000,-0x00FDE000,-0x01148000,-0x012B3000,
            -0x01420000,-0x0158C000,-0x016F6000,-0x0185C000,-0x019BC000,-0x01B16000,-0x01C66000,-0x01DAC000,
            -0x01EE5000,-0x02010000,-0x0212A000,-0x02232000,-0x02325000,-0x02402000,-0x024C7000,-0x02570000,
            -0x025FE000,-0x0266D000,-0x026BB000,-0x026E6000,-0x026ED000,-0x026CE000,-0x02686000,-0x02615000,
            -0x02577000,-0x024AC000,-0x023B2000,-0x02287000,-0x0212B000,-0x01F9B000,-0x01DD7000,-0x01BDD000,
             0x019AE000, 0x01747000, 0x014A8000, 0x011D1000, 0x00EC0000, 0x00B77000, 0x007F5000, 0x0043A000,
             0x00046000,-0x003E5000,-0x00849000,-0x00CE3000,-0x011B4000,-0x016B9000,-0x01BF1000,-0x0215B000,
            -0x026F6000,-0x02CBE000,-0x032B3000,-0x038D3000,-0x03F1A000,-0x04586000,-0x04C15000,-0x052C4000,
            -0x05990000,-0x06075000,-0x06771000,-0x06E80000,-0x0759F000,-0x07CCA000,-0x083FE000,-0x08B37000,
            -0x09270000,-0x099A7000,-0x0A0D7000,-0x0A7FD000,-0x0AF14000,-0x0B618000,-0x0BD05000,-0x0C3D8000,
            -0x0CA8C000,-0x0D11D000,-0x0D789000,-0x0DDC9000,-0x0E3DC000,-0x0E9BD000,-0x0EF68000,-0x0F4DB000,
            -0x0FA12000,-0x0FF09000,-0x103BD000,-0x1082C000,-0x10C53000,-0x1102E000,-0x113BD000,-0x116FB000,
            -0x119E8000,-0x11C82000,-0x11EC6000,-0x120B3000,-0x12248000,-0x12385000,-0x12467000,-0x124EF000,
             0x1251E000, 0x124F0000, 0x12468000, 0x12386000, 0x12249000, 0x120B4000, 0x11EC7000, 0x11C83000,
             0x119E9000, 0x116FC000, 0x113BE000, 0x1102F000, 0x10C54000, 0x1082D000, 0x103BE000, 0x0FF0A000,
             0x0FA13000, 0x0F4DC000, 0x0EF69000, 0x0E9BE000, 0x0E3DD000, 0x0DDCA000, 0x0D78A000, 0x0D11E000,
             0x0CA8D000, 0x0C3D9000, 0x0BD06000, 0x0B619000, 0x0AF15000, 0x0A7FE000, 0x0A0D8000, 0x099A8000,
             0x09271000, 0x08B38000, 0x083FF000, 0x07CCB000, 0x075A0000, 0x06E81000, 0x06772000, 0x06076000,
             0x05991000, 0x052C5000, 0x04C16000, 0x04587000, 0x03F1B000, 0x038D4000, 0x032B4000, 0x02CBF000,
             0x026F7000, 0x0215C000, 0x01BF2000, 0x016BA000, 0x011B5000, 0x00CE4000, 0x0084A000, 0x003E6000,
            -0x00045000,-0x00439000,-0x007F4000,-0x00B76000,-0x00EBF000,-0x011D0000,-0x014A7000,-0x01746000,
             0x019AE000, 0x01BDE000, 0x01DD8000, 0x01F9C000, 0x0212C000, 0x02288000, 0x023B3000, 0x024AD000,
             0x02578000, 0x02616000, 0x02687000, 0x026CF000, 0x026EE000, 0x026E7000, 0x026BC000, 0x0266E000,
             0x025FF000, 0x02571000, 0x024C8000, 0x02403000, 0x02326000, 0x02233000, 0x0212B000, 0x02011000,
             0x01EE6000, 0x01DAD000, 0x01C67000, 0x01B17000, 0x019BD000, 0x0185D000, 0x016F7000, 0x0158D000,
             0x01421000, 0x012B4000, 0x01149000, 0x00FDF000, 0x00E79000, 0x00D17000, 0x00BBC000, 0x00A67000,
             0x0091A000, 0x007D6000, 0x0069C000, 0x0056C000, 0x00447000, 0x0032E000, 0x00221000, 0x00120000,
             0x0002D000,-0x000B8000,-0x00191000,-0x0025C000,-0x00319000,-0x003C9000,-0x0046A000,-0x004FF000,
            -0x00585000,-0x005FE000,-0x0066B000,-0x006CA000,-0x0071D000,-0x00764000,-0x0079F000,-0x007CF000,
             0x007F5000, 0x0080F000, 0x00820000, 0x00827000, 0x00825000, 0x0081B000, 0x00809000, 0x007F0000,
             0x007D1000, 0x007AA000, 0x0077F000, 0x0074E000, 0x00719000, 0x006DF000, 0x006A2000, 0x00662000,
             0x0061F000, 0x005DA000, 0x00594000, 0x0054C000, 0x00503000, 0x004BA000, 0x00471000, 0x00428000,
             0x003DF000, 0x00397000, 0x00350000, 0x0030B000, 0x002C7000, 0x00285000, 0x00245000, 0x00207000,
             0x001CB000, 0x00191000, 0x0015B000, 0x00126000, 0x000F4000, 0x000C5000, 0x00099000, 0x0006F000,
             0x00048000, 0x00024000, 0x00002000,-0x0001C000,-0x00038000,-0x00052000,-0x00069000,-0x0007E000,
            -0x00091000,-0x000A2000,-0x000B0000,-0x000BC000,-0x000C7000,-0x000CF000,-0x000D6000,-0x000DC000,
            -0x000DF000,-0x000E2000,-0x000E3000,-0x000E3000,-0x000E2000,-0x000E0000,-0x000DD000,-0x000D9000,
             0x000D5000, 0x000D0000, 0x000CA000, 0x000C4000, 0x000BE000, 0x000B7000, 0x000B0000, 0x000A9000,
             0x000A1000, 0x0009A000, 0x00093000, 0x0008B000, 0x00084000, 0x0007D000, 0x00075000, 0x0006F000,
             0x00068000, 0x00061000, 0x0005B000, 0x00055000, 0x0004F000, 0x00049000, 0x00044000, 0x0003F000,
             0x0003A000, 0x00035000, 0x00031000, 0x0002D000, 0x00029000, 0x00026000, 0x00023000, 0x0001F000,
             0x0001D000, 0x0001A000, 0x00018000, 0x00015000, 0x00013000, 0x00011000, 0x00010000, 0x0000E000,
             0x0000D000, 0x0000B000, 0x0000A000, 0x00009000, 0x00008000, 0x00007000, 0x00007000, 0x00006000,
             0x00005000, 0x00005000, 0x00004000, 0x00004000, 0x00003000, 0x00003000, 0x00002000, 0x00002000,
             0x00002000, 0x00002000, 0x00001000, 0x00001000, 0x00001000, 0x00001000, 0x00001000, 0x00001000,
        };
    }
}
