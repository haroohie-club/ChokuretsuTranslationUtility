using HaruhiChokuretsuLib.Util;
using System.Collections.Generic;
using System.Linq;

namespace HaruhiChokuretsuLib.Archive.Event
{
    public partial class EventFile
    {
        public List<Tutorial> Tutorials { get; set; } = new();

        public void InitializeTutorialFile()
        {
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
