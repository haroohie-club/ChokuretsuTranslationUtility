using HaruhiChokuretsuLib.Util;
using System.Collections.Generic;
using System.Linq;

// This code is ported from https://github.com/Isaac-Lozano/radx
namespace HaruhiChokuretsuLib.Audio.ADX
{
    public class AdxDecoder : IAdxDecoder
    {
        public AdxHeader Header { get; set; }
        public List<byte> Data { get; set; }
        public List<Sample> Samples { get; set; } = new();
        public Sample PreviousSample { get; set; }
        public Sample PrevPrevSample { get; set; }
        public int Coeff1 { get; set; }
        public int Coeff2 { get; set; }
        public uint AlignmentSamples { get; set; }
        public uint CurrentSample { get; set; }
        public LoopReadInfo? LoopReadInfo { get; set; }
        public bool DoLoop { get; set; }

        public uint Channels => Header.ChannelCount;
        public uint SampleRate => Header.SampleRate;
        public LoopInfo LoopInfo => Header.Version == 3 || Header.Version == 4 ? new()
        {
            StartSample = Header.LoopInfo.BeginSample - Header.LoopInfo.AlignmentSamples,
            EndSample = Header.LoopInfo.EndSample - Header.LoopInfo.AlignmentSamples,
        } : null;

        private int _currentOffset = 0;
        private int _currentBit = 0;

        public AdxDecoder(IEnumerable<byte> data, ILogger log)
        {
            Header = new(data, log);
            DoLoop = Header.LoopInfo.EnabledInt == 1;
            Data = data.ToList();
            (Coeff1, Coeff2) = AdxUtil.GenerateCoefficients(Header.HighpassFrequency, Header.SampleRate);
            Samples = new();
            PreviousSample = new(new short[Header.ChannelCount]);
            PrevPrevSample = new(new short[Header.ChannelCount]);

            if (Header.Version == 3 || Header.Version == 4)
            {
                AlignmentSamples = Header.LoopInfo.AlignmentSamples;
                LoopReadInfo = new()
                {
                    BeginByte = (int)Header.LoopInfo.BeginByte,
                    BeginSample = (int)Header.LoopInfo.BeginSample,
                    EndSample = (int)Header.LoopInfo.EndSample,
                };
            }
            _currentOffset = Header.HeaderSize;
        }

        public List<Sample> ReadFrame()
        {
            uint samplesPerBlock = ((uint)Header.BlockSize - 2) * 8 / Header.SampleBitdepth;
            List<Sample> samples = new(new Sample[samplesPerBlock]);

            for (int channel = 0; channel < Header.ChannelCount; channel++)
            {
                uint rawScale = BigEndianIO.ReadBits(Data, _currentOffset, _currentBit, 16);
                _currentBit += 16;
                if (rawScale == 0x8001)
                {
                    return Enumerable.Empty<Sample>().ToList();
                }

                int scale = (int)rawScale;
                for (int sampleIndex = 0; sampleIndex < samplesPerBlock; sampleIndex++)
                {
                    if (samples[sampleIndex] is null)
                    {
                        samples[sampleIndex] = new();
                    }

                    int predictionFixedPoint = Coeff1 * PreviousSample[channel] + Coeff2 * PrevPrevSample[channel];

                    int prediction = predictionFixedPoint >> 12;

                    int delta = scale * AdxUtil.SignExtend(BigEndianIO.ReadBits(Data, _currentOffset, _currentBit, Header.SampleBitdepth), Header.SampleBitdepth);
                    _currentBit += Header.SampleBitdepth;

                    int unclampledSample = prediction + delta;

                    short sample = unclampledSample >= short.MaxValue ? short.MaxValue : unclampledSample <= short.MinValue ? short.MinValue : (short)unclampledSample;

                    PrevPrevSample[channel] = PreviousSample[channel];
                    PreviousSample[channel] = sample;
                    samples[sampleIndex].Add(sample);
                }
            }

            if (AlignmentSamples != 0)
            {
                CurrentSample = AlignmentSamples;
                AlignmentSamples = 0;
            }

            _currentOffset += _currentBit / 8;
            _currentBit %= 8;

            return samples;
        }

        public Sample NextSample()
        {
            if (LoopReadInfo is not null)
            {
                if (CurrentSample == LoopReadInfo?.EndSample && DoLoop)
                {
                    _currentOffset = LoopReadInfo?.BeginByte ?? 0;
                    CurrentSample = (uint)LoopReadInfo?.BeginSample;
                }
            }

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

            return Samples[(int)CurrentSample++];
        }
    }

    public struct LoopReadInfo
    {
        public int BeginByte { get; set; }
        public int BeginSample { get; set; }
        public int EndSample { get; set; }
    }
}
