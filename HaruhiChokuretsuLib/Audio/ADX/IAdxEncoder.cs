using System.Collections.Generic;
using System.Threading;

namespace HaruhiChokuretsuLib.Audio.ADX
{
    /// <summary>
    /// Interface for ADX encoders
    /// </summary>
    public interface IAdxEncoder
    {
        /// <summary>
        /// Encode audio data in ADX format
        /// </summary>
        /// <param name="samples">List of audio samples</param>
        /// <param name="cancellationToken">Cancellation token for cancelling the operation</param>
        public void EncodeData(IEnumerable<Sample> samples, CancellationToken cancellationToken);
        /// <summary>
        /// Finish encoding and clean up
        /// </summary>
        public void Finish();
    }
}
