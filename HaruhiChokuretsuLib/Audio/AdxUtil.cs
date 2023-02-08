using System;
using System.Collections.Generic;

// This code is ported from https://github.com/Isaac-Lozano/radx
namespace HaruhiChokuretsuLib.Audio
{
    public struct LoopInfo
    {
        public uint StartSample { get; set; }
        public uint EndSample { get; set; }
    }

    public struct AdxSpec
    {
        public uint Channels { get; set; }
        public uint SampleRate { get; set; }
        public LoopInfo? LoopInfo { get; set; }
    }

    public class Sample : List<short>
    {
        public Sample() : base()
        {
        }

        public Sample(IEnumerable<short> collection) : base(collection)
        {
        }
    }

    public static class AdxUtil
    {
        public static (int, int) GenerateCoefficients(uint highpassFrequency, uint sampleRate)
        {
            double highpassSamples = (double)highpassFrequency / sampleRate;
            double a = Math.Sqrt(2) - Math.Cos(2 * Math.PI * highpassSamples);
            double b = Math.Sqrt(2) - 1.0;
            double c = Math.Sqrt(a - ((a + b) * (a - b))) / b;

            double coeff1 = c * 2.0;
            double coeff2 = -Math.Pow(c, 2);

            return ((int)((coeff1 * 4096.0) + 0.5), (int)((coeff2 * 4096.0) + 0.5));
        }

        public static int SignExtend(uint num, uint bits)
        {
            int bitsToShift = (int)(32 - bits);
            return (int)(num << bitsToShift) >> bitsToShift;
        }
    }
}
