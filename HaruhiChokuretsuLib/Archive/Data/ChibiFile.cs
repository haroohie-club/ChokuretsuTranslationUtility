using HaruhiChokuretsuLib.Util;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HaruhiChokuretsuLib.Archive.Data;

/// <summary>
/// A representation of CHIBI.S in dat.bin
/// </summary>
public class ChibiFile : DataFile
{
    /// <summary>
    /// The list of chibis defined in the file
    /// </summary>
    public List<Chibi> Chibis { get; set; } = [];
    private const int NumChibiEntries = 57;

    /// <inheritdoc/>
    public override void Initialize(byte[] decompressedData, int offset, ILogger log)
    {
        Log = log;

        int numSections = IO.ReadInt(decompressedData, 0);
        int numChibis = IO.ReadInt(decompressedData, 0x10) - 2;

        if (numSections != numChibis + 1)
        {
            Log.LogError($"Number of sections ({numSections}) does not match number of chibis ({numChibis}) + 1");
            return;
        }

        int chibiListOffset = IO.ReadInt(decompressedData, 0x0C);
        for (int i = 1; i < numChibis + 1; i++)
        {
            int entryOffset = IO.ReadInt(decompressedData, chibiListOffset + i * 4);
            Chibis.Add(new(decompressedData[entryOffset..(entryOffset + NumChibiEntries * 8)], NumChibiEntries));
        }
    }

    /// <inheritdoc/>
    public override string GetSource(Dictionary<string, IncludeEntry[]> includes)
    {
        if (!includes.ContainsKey("GRPBIN"))
        {
            Log.LogError("Includes needs GRPBIN to be present.");
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
            sb.AppendLine($".word {NumChibiEntries}");
        }
        sb.AppendLine(".word CHIBI00");
        sb.AppendLine($".word {NumChibiEntries}");
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
        set => ChibiEntries[(int)entryName] = value;
    }

    /// <summary>
    /// Parameterless constructor for serialization
    /// </summary>
    public Chibi()
    {
    }

    /// <summary>
    /// Create a chibi given data from CHIBI.S
    /// </summary>
    /// <param name="data">Binary data from the CHIBI.S entry</param>
    /// <param name="numChibiEntries">The number of chibi entries present (defined as 57 without hacking to change that value)</param>
    public Chibi(byte[] data, int numChibiEntries)
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

    /// <inheritdoc/>
    public readonly override string ToString()
    {
        return $"{Texture}, {Animation}";
    }
}

/// <summary>
/// The name of the chibi entry (corresponds to pose and animation)
/// </summary>
public enum ChibiEntryName
{
    /// <summary>
    /// Idle animation, facing down to the left
    /// </summary>
    IDLE_BOTTOM_LEFT,
    /// <summary>
    /// Idle animation, facing down to the right
    /// </summary>
    IDLE_BOTTOM_RIGHT,
    /// <summary>
    /// Idle animation, facing up to the left
    /// </summary>
    IDLE_UPPER_LEFT,
    /// <summary>
    /// Idle animation, facing up to the right
    /// </summary>
    IDLE_UPPER_RIGHT,
    /// <summary>
    /// Walk cycle animation, facing down to the left
    /// </summary>
    WALK_BOTTOM_LEFT,
    /// <summary>
    /// Walk cycle animation, facing down to the right
    /// </summary>
    WALK_BOTTOM_RIGHT,
    /// <summary>
    /// Walk cycle animation, facing up to the left
    /// </summary>
    WALK_UPPER_LEFT,
    /// <summary>
    /// Walk cycle animation, facing up to the right
    /// </summary>
    WALK_UPPER_RIGHT,
    /// <summary>
    /// Search for singularity animation, facing down to the left
    /// </summary>
    INVESTIGATE_BOTTOM_LEFT,
    /// <summary>
    /// Search for singularity animation, facing down to the right
    /// </summary>
    INVESTIGATE_BOTTOM_RIGHT,
    /// <summary>
    /// Search for singularity animation, facing up to the left
    /// </summary>
    INVESTIGATE_UPPER_LEFT,
    /// <summary>
    /// Search for singularity animation, facing up to the right
    /// </summary>
    INVESTIGATE_UPPER_RIGHT,
    /// <summary>
    /// Erase singularity animation, facing down to the left
    /// </summary>
    ERASE_BOTTOM_LEFT,
    /// <summary>
    /// Erase singularity animation, facing down to the right
    /// </summary>
    ERASE_BOTTOM_RIGHT,
    /// <summary>
    /// Erase singularity animation, facing up to the left
    /// </summary>
    ERASE_UPPER_LEFT,
    /// <summary>
    /// Erase singularity animation, facing up to the right
    /// </summary>
    ERASE_UPPER_RIGHT,
    /// <summary>
    /// Puzzle phase failure animation, facing down to the left
    /// </summary>
    FAILURE_BOTTOM_LEFT,
    /// <summary>
    /// Puzzle phase failure animation, facing down to the right
    /// </summary>
    FAILURE_BOTTOM_RIGHT,
    /// <summary>
    /// Puzzle phase failure animation, facing up to the left
    /// </summary>
    FAILURE_UPPER_LEFT,
    /// <summary>
    /// Puzzle phase failure animation, facing up to the right
    /// </summary>
    FAILURE_UPPER_RIGHT,
    /// <summary>
    /// Puzzle phase success animation, facing down to the left
    /// </summary>
    SUCCESS_BOTTOM_LEFT,
    /// <summary>
    /// Puzzle phase success animation, facing down to the right
    /// </summary>
    SUCCESS_BOTTOM_RIGHT,
    /// <summary>
    /// Puzzle phase success animation, facing up to the left
    /// </summary>
    SUCCESS_UPPER_LEFT,
    /// <summary>
    /// Puzzle phase success animation, facing up to the right
    /// </summary>
    SUCCESS_UPPER_RIGHT,
    /// <summary>
    /// Puzzle phase ability use animation, facing down to the left
    /// </summary>
    ABILITY_BOTTOM_LEFT,
    /// <summary>
    /// Puzzle phase ability use animation, facing down to the right
    /// </summary>
    ABILITY_BOTTOM_RIGHT,
    /// <summary>
    /// Puzzle phase ability use animation, facing up to the left
    /// </summary>
    ABILITY_UPPER_LEFT,
    /// <summary>
    /// Puzzle phase ability use animation, facing up to the right
    /// </summary>
    ABILITY_UPPER_RIGHT,
    /// <summary>
    /// Puzzle phase ability failure animation, facing down to the left
    /// </summary>
    ABILITY_FAIL_BOTTOM_LEFT,
    /// <summary>
    /// Puzzle phase ability failure animation, facing down to the right
    /// </summary>
    ABILITY_FAIL_BOTTOM_RIGHT,
    /// <summary>
    /// Puzzle phase ability failure animation, facing up to the left
    /// </summary>
    ABILITY_FAIL_UPPER_LEFT,
    /// <summary>
    /// Puzzle phase ability failure animation, facing up to the right
    /// </summary>
    ABILITY_FAIL_UPPER_RIGHT,
    /// <summary>
    /// Run cycle animation, facing down to the left
    /// </summary>
    RUN_BOTTOM_LEFT,
    /// <summary>
    /// Run cycle animation, facing down to the right
    /// </summary>
    RUN_BOTTOM_RIGHT,
    /// <summary>
    /// Run cycle animation, facing up to the left
    /// </summary>
    RUN_UPPER_LEFT,
    /// <summary>
    /// Run cycle animation, facing up to the right
    /// </summary>
    RUN_UPPER_RIGHT,
    /// <summary>
    /// Unknown
    /// </summary>
    UNKNONW09_BOTTOM_LEFT,
    /// <summary>
    /// Unknown
    /// </summary>
    UNKNOWN09_BOTTOM_RIGHT,
    /// <summary>
    /// Unknown
    /// </summary>
    UNKNOWN09_UPPER_LEFT,
    /// <summary>
    /// Unknown
    /// </summary>
    UNKNOWN09_UPPER_RIGHT,
    /// <summary>
    /// Look animation, facing down to the left
    /// </summary>
    LOOK_BOTTOM_LEFT,
    /// <summary>
    /// Look animation, facing down to the right
    /// </summary>
    LOOK_BOTTOM_RIGHT,
    /// <summary>
    /// Look animation, facing up to the left
    /// </summary>
    LOOK_UPPER_LEFT,
    /// <summary>
    /// Look animation, facing up to the right
    /// </summary>
    LOOK_UPPER_RIGHT,
    /// <summary>
    /// Static pose 97, facing down to the left
    /// </summary>
    STATICPOSE97_BOTTOM_LEFT,
    /// <summary>
    /// Static pose 97, facing down to the right
    /// </summary>
    STATICPOSE97_BOTTOM_RIGHT,
    /// <summary>
    /// Static pose 97, facing up to the left
    /// </summary>
    STATICPOSE97_UPPER_LEFT,
    /// <summary>
    /// Static pose 97, facing up to the right
    /// </summary>
    STATICPOSE97_UPPER_RIGHT,
    /// <summary>
    /// Static pose 98, facing down to the left
    /// </summary>
    STATICPOSE98_BOTTOM_LEFT,
    /// <summary>
    /// Static pose 98, facing down to the right
    /// </summary>
    STATICPOSE98_BOTTOM_RIGHT,
    /// <summary>
    /// Static pose 98, facing up to the left
    /// </summary>
    STATICPOSE98_UPPER_LEFT,
    /// <summary>
    /// Static pose 98, facing up to the right
    /// </summary>
    STATICPOSE98_UPPER_RIGHT,
    /// <summary>
    /// Static pose 99, facing down to the left
    /// </summary>
    STATICPOSE99_BOTTOM_LEFT,
    /// <summary>
    /// Static pose 99, facing down to the right
    /// </summary>
    STATICPOSE99_BOTTOM_RIGHT,
    /// <summary>
    /// Static pose 99, facing up to the left
    /// </summary>
    STATICPOSE99_UPPER_LEFT,
    /// <summary>
    /// Static pose 99, facing up to the right
    /// </summary>
    STATICPOSE99_UPPER_RIGHT,
}