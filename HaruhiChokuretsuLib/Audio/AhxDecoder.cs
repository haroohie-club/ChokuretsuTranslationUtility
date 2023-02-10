using HaruhiChokuretsuLib.Util;
using NAudio.MediaFoundation;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// This code is ported from https://github.com/LemonHaze420/ahx2wav
namespace HaruhiChokuretsuLib.Audio
{
    public struct QuantClass
    {
        public int NLevels { get; set; }
        public int Bits { get; set; }
        public QuantClass(int nLevels, int bits)
        {
            NLevels = nLevels;
            Bits = bits;
        }
    }

    public class AhxDecoder : IAdxDecoder
    {
        public AdxHeader Header { get; set; }
        public List<byte> Data { get; set; }
        public int CurrentSample { get; set; }
        public List<Sample> Samples { get; set; } = new();
        public uint Channels => Header.ChannelCount;
        public uint SampleRate => Header.SampleRate;
        public LoopInfo? LoopInfo => null;

        private int _currentOffset = 0;
        private int _currentBit = 0;

        public AhxDecoder(IEnumerable<byte> data, ILogger log)
        {
            Header = new(data, log);
            Data = data.ToList();
            PopulateCosineDecwinAndPowTables();
            _currentOffset = Header.HeaderSize;
        }

        private void PopulateCosineDecwinAndPowTables()
        {
            // Popwulate cosine table
            for (int i = 0; i < _cosTable.Length; i++)
            {
                _cosTable[i] = new double[16];
            }

            for (int i = 0; i < 16; i++)
                _cosTable[0][i] = 0.5 / Math.Cos(Math.PI * ((i << 1) + 1) / 64.0);

            for (int i = 0; i < 8; i++)
                _cosTable[1][i] = 0.5 / Math.Cos(Math.PI * ((i << 1) + 1) / 32.0);

            for (int i = 0; i < 4; i++)
                _cosTable[2][i] = 0.5 / Math.Cos(Math.PI * ((i << 1) + 1) / 16.0);

            for (int i = 0; i < 2; i++)
                _cosTable[3][i] = 0.5 / Math.Cos(Math.PI * ((i << 1) + 1) / 8.0);

            for (int i = 0; i < 1; i++)
                _cosTable[4][i] = 0.5 / Math.Cos(Math.PI * ((i << 1) + 1) / 4.0);


            // Populate deciwn
            for (int i = 0, j = 0; i < 256; i++, j += 32)
            {
                if (j < 512 + 16)
                {
                    _decwin[j] = _decwin[j + 16] = intwineBase[i] / 65536.0 * 32768.0 * (((i & 64) != 0) ? +1.0 : -1.0);
                }
                if ((i & 31) == 31)
                {
                    j -= 1023;
                }
            }
            for (int i = 0, j = 8; i < 256; i++, j += 32)
            {
                if (j < 512 + 16)
                {
                    _decwin[j] = _decwin[j + 16] = intwineBase[256 - i] / 65536.0 * 32768.0 * (((i & 64) != 0) ? +1.0 : -1.0);
                }
                if ((i & 31) == 31)
                {
                    j -= 1023;
                }
            }

            for (int i = 0; i < 64; i++)
            {
                _powTable[i] = Math.Pow(2.0, (3 - i) / 3.0);
            }
        }

        private (double[], double[]) Dct(double[] src)
        {
            double[] dst0 = new double[17];
            double[] dst1 = new double[17];

            double[][] tmp = new double[2][];
            for (int i = 0; i < tmp.Length; i++)
            {
                tmp[i] = new double[32];
            }

            for (int i = 0; i < 32; i++)
            {
                if ((i & 16) != 0)
                {
                    tmp[0][i] = (-src[i] + src[31 ^ i]) * _cosTable[0][~i & 15];
                }
                else
                {
                    tmp[0][i] = (+src[i] + src[31 ^ i]);
                }
            }
            for (int i = 0; i < 32; i++)
            {
                if ((i & 8) != 0)
                {
                    tmp[1][i] = (-tmp[0][i] + tmp[0][15 ^ i]) * _cosTable[1][~i & 7] * (((i & 16) != 0) ? -1.0 : 1.0);
                }
                else
                {
                    tmp[1][i] = (+tmp[0][i] + tmp[0][15 ^ i]);
                }
            }
            for (int i = 0; i < 32; i++)
            {
                if ((i & 4) != 0)
                {
                    tmp[0][i] = (-tmp[1][i] + tmp[1][7 ^ i]) * _cosTable[2][~i & 3] * (((i & 8) != 0) ? -1.0 : 1.0);
                }
                else
                {
                    tmp[0][i] = (+tmp[1][i] + tmp[1][7 ^ i]);
                }
            }
            for (int i = 0; i < 32; i++)
            {
                if ((i & 2) != 0)
                {
                    tmp[1][i] = (-tmp[0][i] + tmp[0][3 ^ i]) * _cosTable[3][~i & 1] * (((i & 4) != 0) ? -1.0 : 1.0);
                }
                else
                {
                    tmp[1][i] = (+tmp[0][i] + tmp[0][3 ^ i]);
                }
            }
            for (int i = 0; i < 32; i++)
            {
                if ((i & 1) != 0)
                {
                    tmp[0][i] = (-tmp[1][i] + tmp[1][1 ^ i]) * _cosTable[4][0] * (((i & 2) != 0) ? -1.0 : 1.0);
                }
                else
                {
                    tmp[0][i] = (+tmp[1][i] + tmp[1][1 ^ i]);
                }
            }

            for (int i = 0; i < 32; i += 4)
            {
                tmp[0][i + 2] += tmp[0][i + 3];
            }

            for (int i = 0; i < 32; i += 8)
            {
                tmp[0][i + 4] += tmp[0][i + 6];
                tmp[0][i + 6] += tmp[0][i + 5];
                tmp[0][i + 5] += tmp[0][i + 7];
            }

            for (int i = 0; i < 32; i += 16)
            {
                tmp[0][i + 8] += tmp[0][i + 12];
                tmp[0][i + 12] += tmp[0][i + 10];
                tmp[0][i + 10] += tmp[0][i + 14];
                tmp[0][i + 14] += tmp[0][i + 9];
                tmp[0][i + 9] += tmp[0][i + 13];
                tmp[0][i + 13] += tmp[0][i + 11];
                tmp[0][i + 11] += tmp[0][i + 15];
            }

            dst0[16] = tmp[0][0];
            dst0[15] = tmp[0][16 + 0] + tmp[0][16 + 8];
            dst0[14] = tmp[0][8];
            dst0[13] = tmp[0][16 + 8] + tmp[0][16 + 4];
            dst0[12] = tmp[0][4];
            dst0[11] = tmp[0][16 + 4] + tmp[0][16 + 12];
            dst0[10] = tmp[0][12];
            dst0[9] = tmp[0][16 + 12] + tmp[0][16 + 2];
            dst0[8] = tmp[0][2];
            dst0[7] = tmp[0][16 + 2] + tmp[0][16 + 10];
            dst0[6] = tmp[0][10];
            dst0[5] = tmp[0][16 + 10] + tmp[0][16 + 6];
            dst0[4] = tmp[0][6];
            dst0[3] = tmp[0][16 + 6] + tmp[0][16 + 14];
            dst0[2] = tmp[0][14];
            dst0[1] = tmp[0][16 + 14] + tmp[0][16 + 1];
            dst0[0] = tmp[0][1];

            dst1[0] = tmp[0][1];
            dst1[1] = tmp[0][16 + 1] + tmp[0][16 + 9];
            dst1[2] = tmp[0][9];
            dst1[3] = tmp[0][16 + 9] + tmp[0][16 + 5];
            dst1[4] = tmp[0][5];
            dst1[5] = tmp[0][16 + 5] + tmp[0][16 + 13];
            dst1[6] = tmp[0][13];
            dst1[7] = tmp[0][16 + 13] + tmp[0][16 + 3];
            dst1[8] = tmp[0][3];
            dst1[9] = tmp[0][16 + 3] + tmp[0][16 + 11];
            dst1[10] = tmp[0][11];
            dst1[11] = tmp[0][16 + 11] + tmp[0][16 + 7];
            dst1[12] = tmp[0][7];
            dst1[13] = tmp[0][16 + 7] + tmp[0][16 + 15];
            dst1[14] = tmp[0][15];
            dst1[15] = tmp[0][16 + 15];

            return (dst0, dst1);
        }

        private List<Sample> ReadFrame()
        {
            List<Sample> samples = new();

            if (BigEndianIO.ReadBits(Data, _currentOffset, _currentBit, 12) != 0xFFF)
            {
                _currentBit += 12;
                return null;
            }
            _currentBit += 32; // we ignore the remaining 20 bits of the frame header

            int[] bitAlloc = new int[32], scfsi = new int[32];
            int[][] scaleFactor = new int[32][];
            for (int i = 0; i < scaleFactor.Length; i++)
            {
                scaleFactor[i] = new int[3];
            }

            double[][] sbSamples = new double[36][];
            for (int i = 0; i < sbSamples.Length; i++)
            {
                sbSamples[i] = new double[32];
            }

            for (int sb = 0; sb < 30; sb++)
            {
                bitAlloc[sb] = (int)BigEndianIO.ReadBits(Data, _currentOffset, _currentBit, _bitAllocTable[sb]);
                _currentBit += _bitAllocTable[sb];
            }

            for (int sb = 0; sb < 30; sb++)
            {
                if (bitAlloc[sb] != 0)
                {
                    scfsi[sb] = (int)BigEndianIO.ReadBits(Data, _currentOffset, _currentBit, 2);
                    _currentBit += 2;
                }
            }

            for (int sb = 0; sb < 30; sb++)
            {
                if (bitAlloc[sb] != 0)
                {
                    scaleFactor[sb][0] = (int)BigEndianIO.ReadBits(Data, _currentOffset, _currentBit, 6);
                    _currentBit += 6;
                    switch (scfsi[sb])
                    {
                        case 0:
                            scaleFactor[sb][1] = (int)BigEndianIO.ReadBits(Data, _currentOffset, _currentBit, 6);
                            _currentBit += 6;
                            scaleFactor[sb][2] = (int)BigEndianIO.ReadBits(Data, _currentOffset, _currentBit, 6);
                            _currentBit += 6;
                            break;

                        case 1:
                            scaleFactor[sb][1] = scaleFactor[sb][0];
                            scaleFactor[sb][2] = (int)BigEndianIO.ReadBits(Data, _currentOffset, _currentBit, 6);
                            _currentBit += 6;
                            break;

                        case 2:
                            scaleFactor[sb][1] = scaleFactor[sb][0];
                            scaleFactor[sb][2] = scaleFactor[sb][0];
                            break;

                        case 3:
                            scaleFactor[sb][1] = (int)(BigEndianIO.ReadBits(Data, _currentOffset, _currentBit, 6));
                            scaleFactor[sb][2] = scaleFactor[sb][1];
                            break;
                    }
                }
            }

            for (int gr = 0; gr < 12; gr++)
            {
                for (int sb = 0; sb < 30; sb++)
                {
                    if (bitAlloc[sb] != 0)
                    {
                        int index = _offsetTable[_bitAllocTable[sb]][bitAlloc[sb] - 1];
                        int q = 0;
                        if (_qcTable[index].Bits < 0)
                        {
                            int t = (int)BigEndianIO.ReadBits(Data, _currentOffset, _currentBit, -_qcTable[index].Bits);
                            _currentBit += -_qcTable[index].Bits;
                            q = (t % _qcTable[index].NLevels) * 2 - _qcTable[index].NLevels + 1;
                            sbSamples[gr * 3 + 0][sb] = (double)q / _qcTable[index].NLevels;
                            t /= _qcTable[index].NLevels;
                            q = (t % +_qcTable[index].NLevels) * 2 - _qcTable[index].NLevels + 1;
                            sbSamples[gr * 3 + 1][sb] = (double)q / _qcTable[index].NLevels;
                            t /= _qcTable[index].NLevels;
                            q = t * 2 - _qcTable[index].NLevels + 1;
                            sbSamples[gr * 3 + 2][sb] = (double)q / _qcTable[index].NLevels;
                        }
                        else
                        {
                            q = (int)BigEndianIO.ReadBits(Data, _currentOffset, _currentBit, _qcTable[index].Bits) * 2 - _qcTable[index].NLevels + 1;
                            _currentBit += _qcTable[index].Bits;
                            sbSamples[gr * 3 + 0][sb] = (double)q / _qcTable[index].NLevels;
                            q = (int)BigEndianIO.ReadBits(Data, _currentOffset, _currentBit, _qcTable[index].Bits) * 2 - _qcTable[index].NLevels + 1;
                            _currentBit += _qcTable[index].Bits;
                            sbSamples[gr * 3 + 1][sb] = (double)q / _qcTable[index].NLevels;
                            q = (int)BigEndianIO.ReadBits(Data, _currentOffset, _currentBit, _qcTable[index].Bits) * 2 - _qcTable[index].NLevels + 1;
                            _currentBit += _qcTable[index].Bits;
                            sbSamples[gr * 3 + 2][sb] = (double)q / _qcTable[index].NLevels;
                        }
                    }
                    else
                    {
                        sbSamples[gr * 3 + 0][sb] = 0;
                        sbSamples[gr * 3 + 1][sb] = 0;
                        sbSamples[gr * 3 + 2][sb] = 0;
                    }
                    sbSamples[gr * 3 + 0][sb] *= _powTable[scaleFactor[sb][gr >> 2]];
                    sbSamples[gr * 3 + 1][sb] *= _powTable[scaleFactor[sb][gr >> 2]];
                    sbSamples[gr * 3 + 2][sb] *= _powTable[scaleFactor[sb][gr >> 2]];
                }
            }

            int phase = 0;
            double[][][] dctBuf = new double[2][][];
            for (int i = 0; i < dctBuf.Length; i++)
            {
                dctBuf[i] = new double[16][];
                for (int j = 0; j < dctBuf[i].Length; j++)
                {
                    dctBuf[i][j] = new double[17];
                }
            }
            for (int gr = 0; gr < 36; gr++)
            {
                double sum = 0;
                if ((phase & 1) != 0)
                {
                    (dctBuf[0][(phase + 1) & 15], dctBuf[1][phase]) = Dct(sbSamples[gr]);
                }
                else
                {
                    (dctBuf[1][phase], dctBuf[0][phase + 1]) = Dct(sbSamples[gr]);
                }
                int decwinIndex = 16 - (phase | 1);
                for (int i = 0; i < 16; i++)
                {
                    sum = _decwin[decwinIndex++] * dctBuf[phase & 1][0][i];
                    for (int j = 1; j < 16; j++)
                    {
                        sum -= _decwin[decwinIndex++] * dctBuf[phase & 1][j][i];
                        sum += _decwin[decwinIndex++] * dctBuf[phase & 1][j][i];
                    }
                    if (sum >= short.MaxValue)
                    {
                        samples.Add(new(new short[] { short.MaxValue }));
                    }
                    else if (sum <= short.MinValue)
                    {
                        samples.Add(new(new short[] { short.MinValue }));
                    }
                    else
                    {
                        samples.Add(new(new short[] { (short)sum }));
                    }
                }
                int k = 0;
                sum = _decwin[decwinIndex + k] * dctBuf[phase & 1][k][16];
                k += 2;
                while (k < 16)
                {
                    sum += _decwin[decwinIndex + k] * dctBuf[phase & 1][k][16];
                    k += 2;
                }
                if (sum >= short.MaxValue)
                {
                    samples.Add(new(new short[] { short.MaxValue }));
                }
                else if (sum <= short.MinValue)
                {
                    samples.Add(new(new short[] { short.MinValue }));
                }
                else
                {
                    samples.Add(new(new short[] { (short)sum }));
                }
                
                decwinIndex += -16 + (phase | 1) * 2;
                for (int i = 15; i >= 1; i--, decwinIndex -= 16)
                {
                    sum = -_decwin[--decwinIndex] * dctBuf[phase & 1][0][i];
                    for (int j = 1; j < 16; j++)
                    {
                        sum -= -_decwin[--decwinIndex] * dctBuf[phase & 1][j][i];
                    }

                    if (sum >= short.MaxValue)
                    {
                        samples.Add(new(new short[] { short.MaxValue }));
                    }
                    else if (sum <= short.MinValue)
                    {
                        samples.Add(new(new short[] { short.MinValue }));
                    }
                    else
                    {
                        samples.Add(new(new short[] { (short)sum }));
                    }
                }

                phase = (phase - 1) & 0xF;
            }

            _currentOffset += _currentBit / 8;
            _currentBit %= 8;
            return samples;
        }

        public Sample NextSample()
        {
            if (CurrentSample == Samples.Count)
            {
                List<Sample> nextFrame = ReadFrame();
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

        private static int[] _bitAllocTable = new int[] { 4, 4, 4, 4, 3, 3, 3, 3, 3, 3, 3, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2 };
        private static int[][] _offsetTable = new int[][]
        {
            new int[] { 0 },
            new int[] { 0 },
            new int[] { 0, 1, 3, 4, },
            new int[] { 0, 1, 3, 4, 5, 6, 7, 8, },
            new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14 },
        };
        private static QuantClass[] _qcTable = new QuantClass[]
        {
            new(3, -5),
            new(5, -7),
            new(7, 3),
            new(9, -10),
            new(15, 4),
            new(31, 5),
            new(63, 6),
            new(127, 7),
            new(255, 8),
            new(511, 9),
            new(1023, 10),
            new(2047, 11),
            new(4095, 12),
            new(8191, 13),
            new(16383, 14),
            new(32767, 15),
            new(65535, 16)
        };
        private static int[] intwineBase = new int[257]
        {
            0,    -1,    -1,    -1,    -1,    -1,    -1,    -2,    -2,    -2,    -2,    -3,    -3,    -4,    -4,    -5,
            -5,    -6,    -7,    -7,    -8,    -9,   -10,   -11,   -13,   -14,   -16,   -17,   -19,   -21,   -24,   -26,
            -29,   -31,   -35,   -38,   -41,   -45,   -49,   -53,   -58,   -63,   -68,   -73,   -79,   -85,   -91,   -97,
            -104,  -111,  -117,  -125,  -132,  -139,  -147,  -154,  -161,  -169,  -176,  -183,  -190,  -196,  -202,  -208,
            -213,  -218,  -222,  -225,  -227,  -228,  -228,  -227,  -224,  -221,  -215,  -208,  -200,  -189,  -177,  -163,
            -146,  -127,  -106,   -83,   -57,   -29,     2,    36,    72,   111,   153,   197,   244,   294,   347,   401,
            459,   519,   581,   645,   711,   779,   848,   919,   991,  1064,  1137,  1210,  1283,  1356,  1428,  1498,
            1567,  1634,  1698,  1759,  1817,  1870,  1919,  1962,  2001,  2032,  2057,  2075,  2085,  2087,  2080,  2063,
            2037,  2000,  1952,  1893,  1822,  1739,  1644,  1535,  1414,  1280,  1131,   970,   794,   605,   402,   185,
            -45,  -288,  -545,  -814, -1095, -1388, -1692, -2006, -2330, -2663, -3004, -3351, -3705, -4063, -4425, -4788,
            -5153, -5517, -5879, -6237, -6589, -6935, -7271, -7597, -7910, -8209, -8491, -8755, -8998, -9219, -9416, -9585,
            -9727, -9838, -9916, -9959, -9966, -9935, -9863, -9750, -9592, -9389, -9139, -8840, -8492, -8092, -7640, -7134,
            -6574, -5959, -5288, -4561, -3776, -2935, -2037, -1082,   -70,   998,  2122,  3300,  4533,  5818,  7154,  8540,
            9975, 11455, 12980, 14548, 16155, 17799, 19478, 21189, 22929, 24694, 26482, 28289, 30112, 31947, 33791, 35640,
            37489, 39336, 41176, 43006, 44821, 46617, 48390, 50137, 51853, 53534, 55178, 56778, 58333, 59838, 61289, 62684,
            64019, 65290, 66494, 67629, 68692, 69679, 70590, 71420, 72169, 72835, 73415, 73908, 74313, 74630, 74856, 74992,
            75038
        };

        private double[][] _cosTable = new double[5][];
        private double[] _decwin = new double[544];
        private double[] _powTable = new double[64]; 
    }
}
