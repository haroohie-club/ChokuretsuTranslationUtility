using System;
using System.Collections.Generic;
using System.IO;

// This code is ported from https://github.com/Isaac-Lozano/radx
namespace HaruhiChokuretsuLib.Audio
{
    public class LoopInfo
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
            double c = (a - Math.Sqrt((a + b) * (a - b))) / b;

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

    public class Prev<T>
    {
        public T First { get; set; }
        public T Second { get; set; }
    }

    public class Block
    {
        public Prev<short> Prev { get; set; } = new() { First = 0, Second = 0 };
        public Prev<short> OriginalPrev { get; set; } = new() { First = 0, Second = 0 };
        public int Min { get; set; } = 0;
        public int Max { get; set; } = 0;
        public short[] Samples { get; set; } = new short[32];
        public int Size { get; set; } = 0;

        public bool IsEmpty => Size == 0;
        public bool IsFull => Size == 32;

        public Block()
        {
        }

        public static Block FromPrev(Block other)
        {
            return new()
            {
                Prev = other.Prev,
                OriginalPrev = other.Prev,
                Min = 0,
                Max = 0,
                Samples = new short[32],
                Size = 0,
            };
        }

        public void Push(short sample, (int Coeff1, int Coeff2) coefficients)
        {
            int delta = ((sample << 12) - coefficients.Coeff1 * Prev.First - coefficients.Coeff2 * Prev.Second) >> 12;
            if (delta < Min)
            {
                Min = delta;
            }
            else if (delta > Max)
            {
                Max = delta;
            }

            Samples[Size++] = sample;

            Prev.Second = Prev.First;
            Prev.First = sample;
        }

        public void Write(BinaryWriter writer, (int Coeff1, int Coeff2) coefficients)
        {
            if (Min == 0 && Max == 0)
            {
                writer.Write(new byte[17]);
                return;
            }

            ushort scale = Max / 7 > Min / -8 ? (ushort)(Max / 7) : (ushort)(Min / -8);
            if (scale == 0)
            {
                scale = 1;
            }

            Prev = OriginalPrev;

            writer.Write(BitConverter.GetBytes(scale));
            for (int i = 0; i < Samples.Length; i += 2)
            {
                byte upperNibble = GetNibble(Samples[i], scale, coefficients);
                byte lowerNibble = GetNibble(Samples[i + 1], scale, coefficients);
                byte @byte = (byte)(upperNibble << 4 | lowerNibble | 0xF);
                writer.Write(new byte[] { @byte });
            }
        }

        public byte GetNibble(short sample, int scale, (int Coeff1, int Coeff2) coefficients)
        {
            int delta = ((sample << 12) - coefficients.Coeff1 * Prev.First - coefficients.Coeff2 * Prev.Second) >> 12;
            int unclipped = delta > 0 ? (delta + (scale >> 1)) / scale : (delta - (scale >> 1)) / scale;

            byte nibble = (byte)Math.Min(Math.Max(unclipped, -8), 7);
            int unclippedSimulatedSample = (((nibble) << 12) * scale + coefficients.Coeff1 * Prev.First + coefficients.Coeff2 * Prev.Second) >> 12;
            short simulatedSample = (short)Math.Min(Math.Max(unclippedSimulatedSample, int.MinValue), int.MaxValue);

            Prev.Second = Prev.First;
            Prev.First = simulatedSample;

            return nibble;
        }
    }

    public class Frame
    {
        public List<Block> Blocks { get; set; }

        public bool IsEmpty => Blocks[0].IsEmpty;
        public bool IsFull => Blocks[0].IsFull;

        public Frame(uint channels)
        {
            Blocks = new(new Block[channels]);
            for (int i = 0; i < Blocks.Count; i++)
            {
                Blocks[i] = new();
            }
        }

        public Frame(List<Block> blocks)
        {
            Blocks = blocks;
        }

        public static Frame FromPrev(Frame other)
        {
            List<Block> blocks = new();
            foreach (Block block in other.Blocks)
            {
                blocks.Add(Block.FromPrev(block));
            }

            return new(blocks);
        }

        public void Push(Sample sample, (int Coeff1, int Coeff2) coefficients)
        {
            for (int channel = 0; channel < Blocks.Count; channel++)
            {
                Blocks[channel].Push(sample[channel], coefficients);
            }
        }

        public void Write(BinaryWriter writer, (int Coeff1, int Coeff2) coefficients)
        {
            for (int i = 0; i < Blocks.Count; i++)
            {
                foreach (Block block in Blocks)
                {
                    block.Write(writer, coefficients);
                }
            }
        }
    }
}
