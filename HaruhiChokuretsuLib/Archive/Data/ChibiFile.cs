using HaruhiChokuretsuLib.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HaruhiChokuretsuLib.Archive.Data
{
    public class ChibiFile : DataFile
    {
        public List<Chibi> Chibis = new();
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

    public class Chibi
    {
        public List<ChibiEntry> ChibiEntries { get; set; } = new();

        public Chibi(IEnumerable<byte> data, int numChibiEntries)
        {
            for (int i = 0; i < numChibiEntries; i++)
            {
                ChibiEntries.Add(new() { Texture = IO.ReadShort(data, i * 8), Animation = IO.ReadShort(data, i * 8 + 2) });
            }
        }
    }

    public struct ChibiEntry
    {
        public short Texture { get; set; }
        public short Animation { get; set; }
    }
}
