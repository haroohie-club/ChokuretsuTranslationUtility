using HaruhiChokuretsuLib.Util;
using System.Collections.Generic;
using System.Linq;

namespace HaruhiChokuretsuLib.Archive.Event
{
    public partial class EventFile
    {
        /// <summary>
        /// In TUTORIAL.S, represents the list of tutorials
        /// </summary>
        public List<Tutorial> Tutorials { get; set; } = new();

        /// <summary>
        /// Initializes TUTORIAL.S
        /// </summary>
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

    /// <summary>
    /// Represents a tutorial entry in TUTORIAL.S
    /// </summary>
    public class Tutorial(IEnumerable<byte> data)
    {
        /// <summary>
        /// The ID/flag of the tutorial
        /// </summary>
        public short Id { get; set; } = IO.ReadShort(data, 0x00);
        /// <summary>
        /// The script to be loaded for that tutorial
        /// </summary>
        public short AssociatedScript { get; set; } = IO.ReadShort(data, 0x02);
    }
}
