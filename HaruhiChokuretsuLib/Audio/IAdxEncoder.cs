using System.Collections.Generic;

namespace HaruhiChokuretsuLib.Audio
{
    public interface IAdxEncoder
    {
        public void EncodeData(IEnumerable<Sample> samples);
        public void Finish();
    }
}
