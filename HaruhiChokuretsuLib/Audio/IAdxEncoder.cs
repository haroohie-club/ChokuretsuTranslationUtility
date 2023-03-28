using HaruhiChokuretsuLib.Util;
using System.Collections.Generic;

namespace HaruhiChokuretsuLib.Audio
{
    public interface IAdxEncoder
    {
        public void EncodeData(IEnumerable<Sample> samples, IProgressTracker tracker);
        public void Finish(IProgressTracker tracker);
    }
}
