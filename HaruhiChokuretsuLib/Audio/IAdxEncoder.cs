using NAudio.Wave;

namespace HaruhiChokuretsuLib.Audio
{
    public interface IAdxEncoder
    {
        public IWaveProvider InputWave { get; set; }
    }
}
