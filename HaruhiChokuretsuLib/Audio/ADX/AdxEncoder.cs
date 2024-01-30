using HaruhiChokuretsuLib.Util;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

// This code is ported from https://github.com/Isaac-Lozano/radx
namespace HaruhiChokuretsuLib.Audio.ADX
{
    /// <summary>
    /// An encoder for ADX audio
    /// </summary>
    public class AdxEncoder : IAdxEncoder
    {
        /// <summary>
        /// Highpass frequency
        /// </summary>
        public const uint HIGHPASS_FREQ = 0x01F4;

        internal BinaryWriter Writer { get; set; }
        /// <summary>
        /// The ADX specification to write with
        /// </summary>
        public AdxSpec Spec { get; set; }
        internal uint HeaderSize { get; set; }
        internal uint AlignmentSamples { get; set; }
        internal (int Coeff1, int Coeff2) Coefficients { get; set; }
        internal uint SamplesEncoded { get; set; }
        internal Frame CurrentFrame { get; set; }

        /// <summary>
        /// Creates an ADX encoder
        /// </summary>
        /// <param name="writer">A BinaryWriter to write the ADX file to</param>
        /// <param name="spec">The ADX specification to follow</param>
        public AdxEncoder(BinaryWriter writer, AdxSpec spec)
        {
            if (spec.LoopInfo is not null)
            {
                AlignmentSamples = (32 - spec.LoopInfo.StartSample % 32) % 32;
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

            if (spec.LoopInfo is not null)
            {
                List<Sample> samples = new();
                for (int i = 0; i < AlignmentSamples; i++)
                {
                    samples.Add(new(new short[spec.Channels]));
                }
                EncodeData(samples, new());
            }
        }

        /// <inheritdoc/>
        public void EncodeData(IEnumerable<Sample> samples, CancellationToken cancellationToken)
        {
            foreach (Sample sample in samples)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                CurrentFrame.Push(sample, Coefficients);
                SamplesEncoded++;
                if (CurrentFrame.IsFull)
                {
                    CurrentFrame.Write(Writer, Coefficients);
                    CurrentFrame = Frame.FromPrev(CurrentFrame);
                }
            }
        }

        /// <inheritdoc/>
        public void Finish()
        {
            if (!CurrentFrame.IsEmpty)
            {
                CurrentFrame.Write(Writer, Coefficients);
            }

            Writer.Write(BigEndianIO.GetBytes((ushort)0x8001).ToArray());
            Writer.Write(BigEndianIO.GetBytes((ushort)0x000E).ToArray());
            Writer.Write(new byte[14]);

            Writer.Seek(0, SeekOrigin.Begin);

            AdxVersion3LoopInfo loopInfo;
            if (Spec.LoopInfo is not null)
            {
                loopInfo = new()
                {
                    AlignmentSamples = (ushort)AlignmentSamples,
                    EnabledShort = 1,
                    EnabledInt = 1,
                    BeginSample = Spec.LoopInfo.StartSample,
                    BeginByte = SampleToByte(Spec.LoopInfo.StartSample, Spec.Channels) + HeaderSize,
                    EndSample = Spec.LoopInfo.EndSample,
                    EndByte = SampleToByte(Spec.LoopInfo.EndSample, Spec.Channels) + HeaderSize,
                };
            }
            else
            {
                loopInfo = new()
                {
                    AlignmentSamples = (ushort)AlignmentSamples,
                    EnabledShort = 0,
                    EnabledInt = 0,
                    BeginSample = 0,
                    BeginByte = SampleToByte(0, Spec.Channels) + HeaderSize,
                    EndSample = 0,
                    EndByte = SampleToByte(0, Spec.Channels) + HeaderSize,
                };
            }

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
            Writer.Flush();
        }

        private static uint SampleToByte(uint startSample, uint channels)
        {
            uint frames = startSample / 32;
            if (startSample % 32 != 32)
            {
                frames++;
            }
            return frames * 18 * channels;
        }
    }
}
