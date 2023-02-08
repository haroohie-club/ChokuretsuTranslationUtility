using HaruhiChokuretsuLib.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// This code is ported from https://github.com/Isaac-Lozano/radx
namespace HaruhiChokuretsuLib.Audio
{
    public class AdxDecoder
    {
        public AdxHeader Header { get; set; }
        public List<Sample> Samples { get; set; } = new();
        public int SampleVecIndex { get; set; }
        public Sample PreviousSample { get; set; }
        public Sample PrevPrevSample { get; set; }
        public int Coeff1 { get; set; }
        public int Coeff2 { get; set; }
        public uint AlignmentSamples { get; set; }
        public uint CurrentSamples { get; set; }
        public LoopReadInfo? LoopInfo { get; set; }

        public AdxDecoder(IEnumerable<byte> data, ILogger log)
        {
            Header = new(data, log);
            (Coeff1, Coeff2) = AdxUtil.GenerateCoefficients(Header.HighpassFrequency, Header.SampleRate);
            Samples = new();
            PreviousSample = new(new short[Header.ChannelCount]);
            PrevPrevSample = new(new short[Header.ChannelCount]);

            if (Header.Version == 3)
            {
                AlignmentSamples = Header.LoopInfo.AlignmentSamples;
                LoopInfo = new()
                {
                    BeginByte = (int)Header.LoopInfo.BeginByte,
                    BeginSample = (int)Header.LoopInfo.BeginSample,
                    EndSample = (int)Header.LoopInfo.EndSample,
                };
            }
        }

        public List<Sample> ReadFrame()
        {
            uint samplesPerBlock = (((uint)Header.BlockSize - 2) * 8) / Header.SampleBitdepth;
            List<Sample> samples = new();

            for (int channel = 0; channel < Header.ChannelCount; channel++)
            {

            }

            return samples;
        }
    }

    public struct LoopReadInfo
    {
        public int BeginByte { get; set; }
        public int BeginSample { get; set; }
        public int EndSample { get; set; }
    }
}
