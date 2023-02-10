using NAudio.Wave;
using System;
using System.Linq;

namespace HaruhiChokuretsuLib.Audio
{
    public class AdxWaveProvider : IWaveProvider
    {
        private readonly WaveFormat _waveFormat;
        private readonly IAdxDecoder _decoder;

        public AdxWaveProvider(IAdxDecoder decoder)
        {
            _waveFormat = new((int)decoder.SampleRate, (int)decoder.Channels);
            _decoder = decoder;
        }

        public WaveFormat WaveFormat => _waveFormat;

        public int Read(byte[] buffer, int offset, int count)
        {
            int i = 0;
            while (i < count)
            {
                Sample nextSample;
                try
                {
                    nextSample = _decoder.NextSample();
                }
                catch
                {
                    nextSample = null;
                }
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
