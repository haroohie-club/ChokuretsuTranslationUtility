using System.Collections.Generic;
using System.Threading;

namespace HaruhiChokuretsuLib.Audio.ADX
{
    public interface IAdxEncoder
    {
        public void EncodeData(IEnumerable<Sample> samples, CancellationToken cancellationToken);
        public void Finish();
    }
}
