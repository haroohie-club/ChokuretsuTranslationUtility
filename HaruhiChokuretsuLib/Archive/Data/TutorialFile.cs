using HaruhiChokuretsuLib.Util;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HaruhiChokuretsuLib.Archive.Data
{
    public class TutorialFile : DataFile
    {
        public List<Tutorial> Tutorials { get; set; } = new();

        public override void Initialize(byte[] decompressedData, int offset, ILogger log)
        {
            _log = log;
            Data = decompressedData.ToList();
            Offset = offset;

            int numSections = IO.ReadInt(Data, 0x00);
            if (numSections != 1)
            {
                _log.LogError($"Tutorial file should have 1 section, {numSections} detected");
                return;
            }

            int tutorialsStart = IO.ReadInt(Data, 0x0C);
            int numTutorials = IO.ReadInt(Data, 0x10);

            for (int i = 0; i < numTutorials; i++)
            {
                Tutorials.Add(new(Data.Skip(tutorialsStart + i * 0x04).Take(0x04)));
            }
        }

        public override string GetSource(Dictionary<string, IncludeEntry[]> includes)
        {
            StringBuilder sb = new();

            sb.AppendLine(".word 1");
            sb.AppendLine(".word END_POINTERS");
            sb.AppendLine(".word FILE_START");
            sb.AppendLine(".word TUTORIALS");
            sb.AppendLine($".word {Tutorials.Count}");
            sb.AppendLine();
            sb.AppendLine("FILE_START:");
            sb.AppendLine("TUTORIALS:");
            
            foreach (Tutorial tutorial in Tutorials)
            {
                sb.AppendLine($".short {tutorial.Id}");
                sb.AppendLine($".short {tutorial.AssociatedScript}");
            }

            sb.AppendLine("END_POINTERS:");
            sb.AppendLine(".word 0");

            return sb.ToString();
        }
    }

    public class Tutorial
    {
        public short Id { get; set; }
        public short AssociatedScript { get; set; }

        public Tutorial(IEnumerable<byte> data)
        {
            Id = IO.ReadShort(data, 0x00);
            AssociatedScript = IO.ReadShort(data, 0x02);
        }
    }
}
