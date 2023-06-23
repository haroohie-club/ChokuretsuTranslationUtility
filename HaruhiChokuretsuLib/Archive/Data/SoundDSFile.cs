using HaruhiChokuretsuLib.Util;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HaruhiChokuretsuLib.Archive.Data
{
    public class SoundDSFile : DataFile
    {
        public List<int> UnknownSection1 { get; set; }
        public List<UnknownAudio02Entry> UnknownAudio02Section { get; set; }
        public List<SfxEntry> SfxSection { get; set; }
        public List<short> UnknownSection4 { get; set; }
        public List<string> BgmSection { get; set; }
        public List<string> VoiceSection { get; set; }

        public override void Initialize(byte[] decompressedData, int offset, ILogger log)
        {
            _log = log;

            int numSections = IO.ReadInt(decompressedData, 0);
            if (numSections != 10)
            {
                _log.LogError($"DS sound file should have 10 sections, {numSections} detected");
                return;
            }



            Data = decompressedData.ToList();
            Offset = offset;
        }

        public override string GetSource(Dictionary<string, IncludeEntry[]> includes)
        {
            StringBuilder sb = new();



            return sb.ToString();
        }
    }

    public class UnknownAudio02Entry
    {
        public short Unknown01 { get; set; }
        public short Unknown02 { get; set; }
        public short Unknown03 { get; set; }
        public short Unknown04 { get; set; }
        public short Unknown05 { get; set; }

        public string GetSource()
        {
            StringBuilder sb = new();

            sb.AppendLine($".word {Unknown01}");
            sb.AppendLine($".word {Unknown02}");
            sb.AppendLine($".word {Unknown03}");
            sb.AppendLine($".word {Unknown04}");
            sb.AppendLine($".word {Unknown05}");
            sb.AppendLine($".skip 2");

            return sb.ToString();
        }
    }

    public class SfxEntry
    {
        public int Bank { get; set; }
        public int Index { get; set; }
        public int Volume { get; set; }
    }
}
