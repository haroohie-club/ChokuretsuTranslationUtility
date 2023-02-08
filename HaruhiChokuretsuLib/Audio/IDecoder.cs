using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HaruhiChokuretsuLib.Audio
{
    public interface IAudioDecoder
    {
        public uint Channels { get; }
        public uint SampleRate { get; }
        public LoopInfo? LoopInfo { get; }
        public Sample NextSample();
    }
}
