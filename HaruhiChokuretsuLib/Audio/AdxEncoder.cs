using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace HaruhiChokuretsuLib.Audio
{
    public class AdxEncoder
    {
        public const uint HIGHPASS_FREQ = 0x01F4;

        public BinaryWriter Writer { get; set; }
        public AdxSpec Spec { get; set; }
        public uint HeaderSize { get; set; }
        public uint AlignmentSamples { get; set; }
        public (int Coeff1, int Coeff2) Coefficients { get; set; }
        public uint SamplesEncoded { get; set; }
        public Frame CurrentFrame { get; set; }

        public AdxEncoder(BinaryWriter writer, AdxSpec spec)
        {
            if (spec.LoopInfo is not null)
            {
                AlignmentSamples = (32 - (spec.LoopInfo?.StartSample ?? 0 % 32)) % 32;
                spec.LoopInfo.StartSample += AlignmentSamples;
                spec.LoopInfo.EndSample += AlignmentSamples;

                uint bytesTillLoopStart = SampleToByte(spec.LoopInfo.StartSample, spec.Channels);
                uint fsBlocks = bytesTillLoopStart / 0x800;
                if (bytesTillLoopStart % 0x800 > 0x800 - AdxHeader.ADX_HEADER_LENGTH)
                {
                    fsBlocks++;
                }
                fsBlocks++;
                HeaderSize = fsBlocks * 0x800 - bytesTillLoopStart;
            }
            else
            {
                AlignmentSamples = 0;
                HeaderSize = AdxHeader.ADX_HEADER_LENGTH;
            }

            writer.Seek((int)HeaderSize, SeekOrigin.Begin);

            Writer = writer;
            Spec = spec;
            Coefficients = AdxUtil.GenerateCoefficients(HIGHPASS_FREQ, spec.SampleRate);
            SamplesEncoded = 0;
            CurrentFrame = new(spec.Channels);

            List<Sample> samples = new();
            for (int i = 0; i < AlignmentSamples; i++)
            {
                samples.Add(new(new short[spec.Channels]));
            }
            EncodeData(samples);
        }

        public void EncodeData(IEnumerable<Sample> samples)
        {
            foreach (Sample sample in samples)
            {
                CurrentFrame.Push(sample, Coefficients);
                SamplesEncoded++;
                if (CurrentFrame.IsFull)
                {
                    CurrentFrame.Write(Writer, Coefficients);
                    CurrentFrame = Frame.FromPrev(CurrentFrame);
                }
            }
        }

        public void Finish()
        {
            if (!CurrentFrame.IsEmpty)
            {
                CurrentFrame.Write(Writer, Coefficients);
            }

            Writer.Write((ushort)0x8001);
            Writer.Write((ushort)0x000E);
            Writer.Write(new byte[13]);

            Writer.Seek(0, SeekOrigin.Begin);

            AdxVersion3LoopInfo loopInfo = new()
            {
                AlignmentSamples = (ushort)AlignmentSamples,
                EnabledShort = 1,
                EnabledInt = 1,
                BeginSample = Spec.LoopInfo?.StartSample ?? 0,
                BeginByte = SampleToByte(Spec.LoopInfo?.StartSample ?? 0, Spec.Channels) + HeaderSize,
                EndSample = Spec.LoopInfo?.EndSample ?? 0,
                EndByte = SampleToByte(Spec.LoopInfo?.EndSample ?? 0, Spec.Channels) + HeaderSize,
            };

            AdxHeader header = new()
            {
                AdxEncoding = AdxEncoding.Standard,
                LoopInfo = loopInfo,
                BlockSize = 18,
                SampleBitdepth = 4,
                ChannelCount = (byte)Spec.Channels,
                SampleRate = Spec.SampleRate,
                TotalSamples = SamplesEncoded,
                HighpassFrequency = (ushort)HIGHPASS_FREQ,
                Version = 3,
                Flags = 0,
            };
            Writer.Write(header.GetBytes((int)HeaderSize).ToArray());
        }

        public uint SampleToByte(uint startSample, uint channels)
        {
            uint frames = startSample / 32;
            if (startSample % 32 != 32)
            {
                frames++;
            }
            return (uint)(frames * 18 * channels);
        }
    }
}
