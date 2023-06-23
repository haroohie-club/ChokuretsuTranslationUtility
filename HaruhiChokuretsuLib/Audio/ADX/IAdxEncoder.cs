using System.Collections.Generic;

namespace HaruhiChokuretsuLib.Audio.ADX
{
    public interface IAdxEncoder
    {
        public void EncodeData(IEnumerable<Sample> samples);
        public void Finish();
    }
}
