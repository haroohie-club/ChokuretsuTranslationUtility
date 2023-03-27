using NAudio.Wave;
using System;
using System.Linq;

namespace HaruhiChokuretsuLib.Audio
{
    public class AdxWaveProvider : IWaveProvider
    {
        private readonly WaveFormat _waveFormat;
        private readonly IAdxDecoder _decoder;

        public uint LoopStartSample { get; }
        public uint LoopEndSample { get; }

        public AdxWaveProvider(IAdxDecoder decoder, uint loopStartSample = 0, uint loopEndSample = 0)
        {
            _waveFormat = new((int)decoder.SampleRate, (int)decoder.Channels);
            _decoder = decoder;
            LoopStartSample = loopStartSample;
            LoopEndSample = loopEndSample;
        }

        public WaveFormat WaveFormat => _waveFormat;

        public int Read(byte[] buffer, int offset, int count)
        {
            int i = 0;
            while (i < count)
            {
                Sample nextSample = _decoder.NextSample();
                if (nextSample is null)
                {
                    return i;
                }
                byte[] bytes = nextSample.SelectMany(s => BitConverter.GetBytes(s)).ToArray();
                Array.Copy(bytes, 0, buffer, offset + i, bytes.Length);
                i += bytes.Length;
            }
            return i;
        }
    }
}
