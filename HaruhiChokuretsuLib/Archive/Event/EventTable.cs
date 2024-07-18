using HaruhiChokuretsuLib.Util;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HaruhiChokuretsuLib.Archive.Event
{
    public partial class EventFile
    {
        /// <summary>
        /// For EVTTBL.S, the representation of the event table
        /// </summary>
        public EventTable EvtTbl { get; set; }
    }

    /// <summary>
    /// Logical representation of EVTTBL.S in evt.bin
    /// </summary>
    public class EventTable
    {
        /// <summary>
        /// The entries in the event table
        /// </summary>
        public List<EventTableEntry> Entries { get; set; } = [];

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="data">Binary data representation of EVTTBL.S</param>
        public EventTable(List<byte> data)
        {
            int numEntries = IO.ReadInt(data, 0x10);
            for (int idx = 0x14; idx < 0x14 + 0x0C * numEntries; idx += 0x0C)
            {
                Entries.Add(new(data, idx));
            }
        }

        /// <summary>
        /// Gets the source file equivalent of this event table
        /// </summary>
        /// <param name="includes">The dictionary of includes</param>
        /// <param name="log">A logging instance</param>
        /// <returns>An ARM assembly source file representing the event table</returns>
        public string GetSource(Dictionary<string, IncludeEntry[]> includes, ILogger log)
        {
            if (!includes.TryGetValue("EVTBIN", out IncludeEntry[] evtBinInclude))
            {
                log.LogError("Includes needs EVTBIN to be present.");
                return null;
            }

            StringBuilder sb = new();

            sb.AppendLine(".word 1");
            sb.AppendLine(".word END_POINTERS");
            sb.AppendLine(".word FILE_START");
            sb.AppendLine(".word EVTTBL");
            sb.AppendLine($".word {Entries.Count}");
            sb.AppendLine();
            sb.AppendLine("FILE_START:");
            sb.AppendLine("EVTTBL:");

            for (int i = 0; i < Entries.Count; i++)
            {
                sb.AppendLine(Entries[i].GetSource(i, evtBinInclude));
            }
            for (int i = 0; i < Entries.Count; i++)
            {
                sb.AppendLine($"EVENT{i:D3}: .string \"{Entries[i].EventFileName}\"");
            }
            sb.AppendLine();
            sb.AppendLine("END_POINTERS:");
            sb.AppendLine($".word {Entries.Count}");
            for (int i = 0; i < Entries.Count; i++)
            {
                sb.AppendLine($".word ENDPOINTER{i:D3}");
            }

            return sb.ToString();
        }
    }

    /// <summary>
    /// Representation of an entry in the event table (EVTTBL.S)
    /// </summary>
    public class EventTableEntry(IEnumerable<byte> data, int idx)
    {
        /// <summary>
        /// The name of the event file referenced
        /// </summary>
        public string EventFileName { get; set; } = IO.ReadAsciiString(data, IO.ReadInt(data, idx));
        /// <summary>
        /// The index of the event file in evt.bin
        /// </summary>
        public short EventFileIndex { get; set; } = IO.ReadShort(data, idx + 0x04);
        /// <summary>
        /// The SDAT sound group index used by this event
        /// </summary>
        public short SfxGroupIndex { get; set; } = IO.ReadShort(data, idx + 0x06);
        /// <summary>
        /// The first read flag for this event
        /// </summary>
        public short FirstReadFlag { get; set; } = IO.ReadShort(data, idx + 0x08);

        /// <summary>
        /// Gets a source representation of the entry
        /// </summary>
        /// <param name="idx">The position of this entry in the table</param>
        /// <param name="evtIncludes">The evt.bin includes</param>
        /// <returns>An ARM assembly source representation of this entry</returns>
        public string GetSource(int idx, IncludeEntry[] evtIncludes)
        {
            StringBuilder sb = new();
            sb.AppendLine($"ENDPOINTER{idx:D3}: .word EVENT{idx:D3}");
            sb.AppendLine($".short {evtIncludes.First(i => i.Value == EventFileIndex).Name}");
            sb.AppendLine($".short {SfxGroupIndex}");
            sb.AppendLine($".short {FirstReadFlag}");
            sb.AppendLine(".skip 2");
            sb.AppendLine();

            return sb.ToString();
        }
    }
}
