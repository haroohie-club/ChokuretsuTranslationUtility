using HaruhiChokuretsuLib.Util;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HaruhiChokuretsuLib.Archive.Data
{
    /// <summary>
    /// A representation of CHIBI.S in dat.bin
    /// </summary>
    public class ChibiFile : DataFile
    {
        /// <summary>
        /// The list of chibis defined in the file
        /// </summary>
        public List<Chibi> Chibis { get; set; } = [];
        private const int NUM_CHIBI_ENTRIES = 57;

        public override void Initialize(byte[] decompressedData, int offset, ILogger log)
        {
            _log = log;

            int numSections = IO.ReadInt(decompressedData, 0);
            int numChibis = IO.ReadInt(decompressedData, 0x10) - 2;

            if (numSections != numChibis + 1)
            {
                _log.LogError($"Number of sections ({numSections}) does not match number of chibis ({numChibis}) + 1");
                return;
            }

            int chibiListOffset = IO.ReadInt(decompressedData, 0x0C);
            for (int i = 1; i < numChibis + 1; i++)
            {
                Chibis.Add(new(decompressedData.Skip(IO.ReadInt(decompressedData, chibiListOffset + i * 4)).Take(NUM_CHIBI_ENTRIES * 8), NUM_CHIBI_ENTRIES));
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
            sb.AppendLine($".word {Chibis.Count + 1}");
            sb.AppendLine(".word END_POINTERS");
            sb.AppendLine(".word FILE_START");
            sb.AppendLine(".word CHIBI_LIST");
            sb.AppendLine($".word {Chibis.Count + 2}");
            for (int i = 1; i < Chibis.Count; i++)
            {
                sb.AppendLine($".word CHIBI{i:D2}");
                sb.AppendLine($".word {NUM_CHIBI_ENTRIES}");
            }
            sb.AppendLine(".word CHIBI00");
            sb.AppendLine($".word {NUM_CHIBI_ENTRIES}");
            sb.AppendLine();
            sb.AppendLine("FILE_START:");

            for (int i = 0; i < Chibis.Count; i++)
            {
                sb.AppendLine($"CHIBI{i:D2}:");
                foreach (ChibiEntry chibiEntry in Chibis[i].ChibiEntries)
                {
                    if (chibiEntry.Texture == 0)
                    {
                        sb.AppendLine("   .short 0");
                    }
                    else
                    {
                        sb.AppendLine($"   .short {includes["GRPBIN"].First(inc => inc.Value == chibiEntry.Texture).Name}");
                    }
                    if (chibiEntry.Animation == 0)
                    {
                        sb.AppendLine("   .short 0");
                    }
                    else
                    {
                        sb.AppendLine($"   .short {includes["GRPBIN"].First(inc => inc.Value == chibiEntry.Animation).Name}");
                    }
                    sb.AppendLine("   .word 0");
                }
                sb.AppendLine();
            }

            sb.AppendLine("CHIBI_LIST:");
            int currentPointer = 0;

            sb.AppendLine("   .word 0");
            for (int i = 0; i < Chibis.Count; i++)
            {
                sb.AppendLine($"   POINTER{currentPointer++:D2}: .word CHIBI{i:D2}");
            }
            sb.AppendLine("   .word 0");
            sb.AppendLine();

            sb.AppendLine($"END_POINTERS:");
            sb.AppendLine($".word {currentPointer}");
            for (int i = 0; i < currentPointer; i++)
            {
                sb.AppendLine($".word POINTER{i:D2}");
            }

            return sb.ToString();
        }
    }

    /// <summary>
    /// A representation of a set of chibi sprites used in the investigation phase or on the top screen corresponding to a particular character
    /// </summary>
    public class Chibi
    {
        /// <summary>
        /// A list of chibi entries defining the chibi data
        /// </summary>
        public List<ChibiEntry> ChibiEntries { get; set; } = [];
        /// <summary>
        /// Indexes into the list of chibi entries by name rather than index
        /// </summary>
        /// <param name="entryName">The name of the chibi entry</param>
        /// <returns>The chibi entry in question</returns>
        public ChibiEntry this[ChibiEntryName entryName]
        {
            get => ChibiEntries[(int)entryName];
            set { ChibiEntries[(int)entryName] = value; }
        }

        /// <summary>
        /// Create a chibi given data from CHIBI.S
        /// </summary>
        /// <param name="data">Binary data from the CHIBI.S entry</param>
        /// <param name="numChibiEntries">The number of chibi entries present (defined as 57 without hacking to change that value)</param>
        public Chibi(IEnumerable<byte> data, int numChibiEntries)
        {
            for (int i = 0; i < numChibiEntries; i++)
            {
                ChibiEntries.Add(new() { Texture = IO.ReadShort(data, i * 8), Animation = IO.ReadShort(data, i * 8 + 2) });
            }
        }
    }

    /// <summary>
    /// A struct representing a particular chibi sprite
    /// </summary>
    public struct ChibiEntry
    {
        /// <summary>
        /// The grp.bin index of the chibi sprite's texture
        /// </summary>
        public short Texture { get; set; }
        /// <summary>
        /// The grp.bin index of the chibi sprite's animation file
        /// </summary>
        public short Animation { get; set; }

        public override readonly string ToString()
        {
            return $"{Texture}, {Animation}";
        }
    }

    /// <summary>
    /// The name of the chibi entry (corresponds to pose and animation)
    /// </summary>
    public enum ChibiEntryName
    {
        IDLE_BOTTOM_LEFT,
        IDLE_BOTTOM_RIGHT,
        IDLE_UPPER_LEFT,
        IDLE_UPPER_RIGHT,
        WALK_BOTTOM_LEFT,
        WALK_BOTTOM_RIGHT,
        WALK_UPPER_LEFT,
        WALK_UPPER_RIGHT,
        INVESTIGATE_BOTTOM_LEFT,
        INVESTIGATE_BOTTOM_RIGHT,
        INVESTIGATE_UPPER_LEFT,
        INVESTIGATE_UPPER_RIGHT,
        ERASE_BOTTOM_LEFT,
        ERASE_BOTTOM_RIGHT,
        ERASE_UPPER_LEFT,
        ERASE_UPPER_RIGHT,
        FAILURE_BOTTOM_LEFT,
        FAILURE_BOTTOM_RIGHT,
        FAILURE_UPPER_LEFT,
        FAILURE_UPPER_RIGHT,
        SUCCESS_BOTTOM_LEFT,
        SUCCESS_BOTTOM_RIGHT,
        SUCCESS_UPPER_LEFT,
        SUCCESS_UPPER_RIGHT,
        ABILITY_BOTTOM_LEFT,
        ABILITY_BOTTOM_RIGHT,
        ABILITY_UPPER_LEFT,
        ABILITY_UPPER_RIGHT,
        ABILITY_FAIL_BOTTOM_LEFT,
        ABILITY_FAIL_BOTTOM_RIGHT,
        ABILITY_FAIL_UPPER_LEFT,
        ABILITY_FAIL_UPPER_RIGHT,
        RUN_BOTTOM_LEFT,
        RUN_BOTTOM_RIGHT,
        RUN_UPPER_LEFT,
        RUN_UPPER_RIGHT,
        UNKNONW09_BOTTOM_LEFT,
        UNKNOWN09_BOTTOM_RIGHT,
        UNKNOWN09_UPPER_LEFT,
        UNKNOWN09_UPPER_RIGHT,
        LOOK_BOTTOM_LEFT,
        LOOK_BOTTOM_RIGHT,
        LOOK_UPPER_LEFT,
        LOOK_UPPER_RIGHT,
        STATICPOSE97_BOTTOM_LEFT,
        STATICPOSE97_BOTTOM_RIGHT,
        STATICPOSE97_UPPER_LEFT,
        STATICPOSE97_UPPER_RIGHT,
        STATICPOSE98_BOTTOM_LEFT,
        STATICPOSE98_BOTTOM_RIGHT,
        STATICPOSE98_UPPER_LEFT,
        STATICPOSE98_UPPER_RIGHT,
        STATICPOSE99_BOTTOM_LEFT,
        STATICPOSE99_BOTTOM_RIGHT,
        STATICPOSE99_UPPER_LEFT,
        STATICPOSE99_UPPER_RIGHT,
    }
}
