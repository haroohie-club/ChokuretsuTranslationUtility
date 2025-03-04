using HaruhiChokuretsuLib.Util;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HaruhiChokuretsuLib.Archive.Event;

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
    /// Parameterless constructor for serialization
    /// </summary>
    public EventTable()
    {
    }
    
    /// <summary>
    /// Default constructor
    /// </summary>
    /// <param name="data">Binary data representation of EVTTBL.S</param>
    public EventTable(byte[] data)
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

        sb.AppendLine(".include \"EVTBIN.INC\"");
        sb.AppendLine();
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
            if (!string.IsNullOrEmpty(Entries[i].EventFileName))
            {
                sb.AppendLine($"EVENT{i:D3}: .string \"{Entries[i].EventFileName}\"");
                sb.AsmPadString(Entries[i].EventFileName, Encoding.ASCII);
            }
        }
        sb.AppendLine();
        sb.AppendLine("END_POINTERS:");
        sb.AppendLine($".word {Entries.Count(e => !string.IsNullOrEmpty(e.EventFileName))}");
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
public class EventTableEntry
{
    /// <summary>
    /// The name of the event file referenced
    /// </summary>
    public string EventFileName { get; set; }
    /// <summary>
    /// The index of the event file in evt.bin
    /// </summary>
    public short EventFileIndex { get; set; }
    /// <summary>
    /// The SDAT sound group index used by this event
    /// </summary>
    public short SfxGroupIndex { get; set; }
    /// <summary>
    /// The first read flag for this event
    /// </summary>
    public short FirstReadFlag { get; set; }

    /// <summary>
    /// Parameter-based constructor
    /// </summary>
    /// <param name="eventFileIndex">The event file index</param>
    /// <param name="sfxGroupIndex">The SFX group index</param>
    /// <param name="firstReadFlag">The first read flag</param>
    public EventTableEntry(short eventFileIndex, short sfxGroupIndex, short firstReadFlag)
    {
        EventFileIndex = eventFileIndex;
        SfxGroupIndex = sfxGroupIndex;
        FirstReadFlag = firstReadFlag;
    }

    /// <summary>
    /// Data-based constructor
    /// </summary>
    /// <param name="data">The data for the entire event table file</param>
    /// <param name="idx">The offset into the event table file where this struct begins</param>
    public EventTableEntry(byte[] data, int idx)
    {
        EventFileName = IO.ReadInt(data, idx) == 0 ? string.Empty : IO.ReadAsciiString(data, IO.ReadInt(data, idx));
        EventFileIndex = IO.ReadShort(data, idx + 0x04);
        SfxGroupIndex = IO.ReadShort(data, idx + 0x06);
        FirstReadFlag = IO.ReadShort(data, idx + 0x08);
    }

    /// <summary>
    /// Gets a source representation of the entry
    /// </summary>
    /// <param name="idx">The position of this entry in the table</param>
    /// <param name="evtIncludes">The evt.bin includes</param>
    /// <returns>An ARM assembly source representation of this entry</returns>
    public string GetSource(int idx, IncludeEntry[] evtIncludes)
    {
        StringBuilder sb = new();
        if (string.IsNullOrEmpty(EventFileName))
        {
            sb.AppendLine(".word 0");
        }
        else
        {
            sb.AppendLine($"ENDPOINTER{idx:D3}: .word EVENT{idx:D3}");
        }
        sb.AppendLine($".short {evtIncludes.FirstOrDefault(i => i.Value == EventFileIndex)?.Name ?? EventFileIndex.ToString()}");
        sb.AppendLine($".short {SfxGroupIndex}");
        sb.AppendLine($".short {FirstReadFlag}");
        sb.AppendLine(".skip 2");
        sb.AppendLine();

        return sb.ToString();
    }
}