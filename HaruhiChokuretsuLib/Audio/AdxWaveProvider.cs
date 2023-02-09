using NAudio.Wave;
using System;
using System.Linq;

namespace HaruhiChokuretsuLib.Audio
{
    public class AdxWaveProvider : IWaveProvider
    {
        private readonly WaveFormat _waveFormat;
        private readonly AdxDecoder _decoder;

        public AdxWaveProvider(AdxDecoder adx)
        {
            _waveFormat = new((int)adx.SampleRate, (int)adx.Channels);
            _decoder = adx;
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
                    return 0;
                }
                byte[] bytes = nextSample.SelectMany(s => BitConverter.GetBytes(s)).ToArray();
                Array.Copy(bytes, 0, buffer, offset + i, bytes.Length);
                i += bytes.Length;
            }
            return i;
        }
    }
}
