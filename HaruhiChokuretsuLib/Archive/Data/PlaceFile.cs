using HaruhiChokuretsuLib.Util;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HaruhiChokuretsuLib.Archive.Data
{
    public class PlaceFile : DataFile
    {
        public List<int> PlaceGraphicIndices { get; set; } = new();

        public override void Initialize(byte[] decompressedData, int offset, ILogger log)
        {
            _log = log;

            int numSections = IO.ReadInt(decompressedData, 0);
            if (numSections != 1)
            {
                _log.LogError($"PLACE file should only have 1 section; {numSections} specified.");
                return;
            }

            int sectionOffset = IO.ReadInt(decompressedData, 0x0C);
            int sectionCount = IO.ReadInt(decompressedData, 0x10);
            for (int i = 0; i < sectionCount - 1; i++)
            {
                PlaceGraphicIndices.Add(IO.ReadInt(decompressedData, sectionOffset + i * 4));
            }
        }

        public override string GetSource(Dictionary<string, IncludeEntry[]> includes)
        {
            if (!includes.ContainsKey("GRPBIN"))
            {
                _log.LogError("Includes needs GRPBIN to be present.");
                return null;
            }
            StringBuilder sb = new();

            sb.AppendLine(".include \"GRPBIN.INC\"");
            sb.AppendLine();
            sb.AppendLine(".word 1");
            sb.AppendLine(".word END_POINTERS");
            sb.AppendLine(".word FILE_START");
            sb.AppendLine(".word PLACES");
            sb.AppendLine($".word {PlaceGraphicIndices.Count + 1}");
            sb.AppendLine();
            sb.AppendLine("FILE_START:");
            sb.AppendLine("PLACES:");
            
            for (int i = 0; i < PlaceGraphicIndices.Count; i++)
            {
                sb.AppendLine($".word {includes["GRPBIN"].First(g => g.Value == PlaceGraphicIndices[i]).Name}");
            }
            sb.AppendLine(".word 0");

            sb.AppendLine("END_POINTERS:");
            sb.AppendLine(".word 0");

            return sb.ToString();
        }
    }
}
