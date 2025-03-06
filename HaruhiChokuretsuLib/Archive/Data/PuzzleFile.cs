using HaruhiChokuretsuLib.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HaruhiChokuretsuLib.Archive.Event;

namespace HaruhiChokuretsuLib.Archive.Data;

/// <summary>
/// A representation of puzzle files in dat.bin
/// </summary>
public class PuzzleFile : DataFile
{
    internal List<(int Offset, int ItemCount)> SectionOffsetsAndCounts { get; set; } = [];

    /// <summary>
    /// The list of the puzzle's associated topics
    /// </summary>
    public List<PuzzleTopicRecord> AssociatedTopics { get; set; } = [];
    /// <summary>
    /// The list of the puzzle's Haruhi Routes
    /// </summary>
    public List<PuzzleHaruhiRoute> HaruhiRoutes { get; set; } = [];
    /// <summary>
    /// A representation of the puzzle's settings
    /// </summary>
    public PuzzleSettings Settings { get; set; }

    /// <inheritdoc/>
    public override void Initialize(byte[] decompressedData, int offset, ILogger log)
    {
        Log = log;
        Offset = offset;
        Data = [.. decompressedData];

        int numSections = IO.ReadInt(decompressedData, 0x00);
        if (numSections != 13)
        {
            Log.LogError($"A puzzle file must have 13 sections but {numSections} were detected in one");
            return;
        }
        int fileStart = IO.ReadInt(decompressedData, 0x08);
        for (int i = 0x0C; i < fileStart; i += 0x08)
        {
            SectionOffsetsAndCounts.Add((IO.ReadInt(decompressedData, i), IO.ReadInt(decompressedData, i + 4)));
        }

        Settings = new(decompressedData[SectionOffsetsAndCounts[0].Offset..(SectionOffsetsAndCounts[0].Offset + 0x48)]);
        for (int i = 1; i < 11; i++)
        {
            HaruhiRoutes.Add(new(Data.Skip(SectionOffsetsAndCounts[i].Offset).Take(SectionOffsetsAndCounts[i].ItemCount * 2).ToArray()));
        }
        for (int i = 0; i < SectionOffsetsAndCounts[12].ItemCount; i++)
        {
            AssociatedTopics.Add(new(IO.ReadInt(decompressedData, SectionOffsetsAndCounts[12].Offset + i * 8), IO.ReadInt(decompressedData, SectionOffsetsAndCounts[12].Offset + i * 8 + 4)));
        }
    }

    /// <inheritdoc/>
    public override string GetSource(Dictionary<string, IncludeEntry[]> includes)
    {
        if (!includes.TryGetValue("GRPBIN", out IncludeEntry[] grpBinInclude))
        {
            Log.LogError("Includes needs GRPBIN to be present.");
            return null;
        }

        StringBuilder sb = new();

        sb.AppendLine(".include \"GRPBIN.INC\"");
        sb.AppendLine();

        sb.AppendLine(".word 13");
        sb.AppendLine(".word END_POINTERS");
        sb.AppendLine(".word FILE_START");
        sb.AppendLine(".word PUZZLE_SETTINGS");
        sb.AppendLine(".word 1");
        for (int i = 0; i < HaruhiRoutes.Count; i++)
        {
            sb.AppendLine($".word HARUHI_ROUTE{i:D2}");
            sb.AppendLine($".word {HaruhiRoutes[i].HaruhiRoute.Count}");
        }
        sb.AppendLine(".word POINTERS");
        sb.AppendLine(".word 1");
        sb.AppendLine(".word MAIN_TOPICS");
        sb.AppendLine($".word {AssociatedTopics.Count}");
        sb.AppendLine();

        sb.AppendLine("FILE_START:");
        sb.AppendLine("MAIN_TOPICS:");

        foreach (var (topic, unknown) in AssociatedTopics)
        {
            sb.AppendLine($"   .word {topic}");
            sb.AppendLine($"   .word {unknown}");
        }
        sb.AppendLine();

        for (int i = 0; i < HaruhiRoutes.Count; i++)
        {
            sb.AppendLine($"HARUHI_ROUTE{i:D2}:");
            foreach (PuzzleHaruhiRoute.RouteEvent routeEvent in HaruhiRoutes[i].HaruhiRoute)
            {
                sb.AppendLine($"   .byte {(byte)routeEvent.EventType}");
                sb.AppendLine($"   .byte {routeEvent.AssociatedTopicIndex}");
            }
            sb.AppendLine(".skip 2");
        }
        sb.AppendLine();

        sb.AppendLine("POINTERS:");
        int numPointers = 0;
        sb.AppendLine($"   POINTER{numPointers++}: .word MAIN_TOPICS");
        for (int i = 0; i < HaruhiRoutes.Count; i++)
        {
            sb.AppendLine($"   POINTER{numPointers++}: .word HARUHI_ROUTE{i:D2}");
        }
        sb.AppendLine();

        sb.AppendLine("PUZZLE_SETTINGS:");
        sb.AppendLine($"   .word {Settings.MapId}");
        sb.AppendLine($"   .word {Settings.BaseTime}");
        sb.AppendLine($"   .word {Settings.NumSingularities}");
        sb.AppendLine($"   .word {Settings.Unknown04}");
        sb.AppendLine($"   .word {Settings.TargetNumber}");
        sb.AppendLine($"   .word {(Settings.ContinueOnFailure ? 1 : 0)}");
        sb.AppendLine($"   .word {(int)Settings.AccompanyingCharacter}");
        sb.AppendLine($"   .word {(int)Settings.PowerCharacter1}");
        sb.AppendLine($"   .word {(int)Settings.PowerCharacter2}");
        sb.AppendLine($"   .word {grpBinInclude.First(i => i.Value == Settings.SingularityTexture).Name}");
        sb.AppendLine($"   .word {grpBinInclude.First(i => i.Value == Settings.SingularityLayout).Name}");
        sb.AppendLine($"   .word {(Settings.SingularityAnim1 - 1 > 0 ? grpBinInclude.First(i => i.Value == Settings.SingularityAnim1).Name : 0)}");
        sb.AppendLine($"   .word {(Settings.SingularityAnim2 - 1 > 0 ? grpBinInclude.First(i => i.Value == Settings.SingularityAnim2).Name : 0)}");
        sb.AppendLine($"   .word {Settings.TopicSet}");
        sb.AppendLine($"   .word {Settings.Unknown15}");
        sb.AppendLine($"   .word {Settings.Unknown16}");
        sb.AppendLine($"   .word {Settings.Unknown17}");
        sb.AppendLine($"   POINTER{numPointers++}: .word POINTERS");
        sb.AppendLine();

        sb.AppendLine("END_POINTERS:");
        sb.AppendLine($".word {numPointers}");
        for (int i = 0; i < numPointers; i++)
        {
            sb.AppendLine($"   .word POINTER{i}");
        }

        return sb.ToString();
    }
}

/// <summary>
/// A representation of one of the Haruhi Routes for a puzzle
/// </summary>
public class PuzzleHaruhiRoute
{
    /// <summary>
    /// An enum representing the different kinds of events that can exist on a Haruhi route
    /// </summary>
    public enum RouteEventType : byte
    {
        /// <summary>
        /// No event occurs
        /// </summary>
        NOTHING,
        /// <summary>
        /// A topic can be used
        /// </summary>
        TOPIC,
        /// <summary>
        /// An accident occurs
        /// </summary>
        ACCIDENT
    }

    /// <summary>
    /// A struct representing a route event
    /// </summary>
    public struct RouteEvent
    {
        /// <summary>
        /// The type of the route event
        /// </summary>
        public RouteEventType EventType;
        /// <summary>
        /// If the event is an accident, the associated index of the main topic in TOPIC.S
        /// </summary>
        public byte AssociatedTopicIndex;
    }

    /// <summary>
    /// The list of route events comprising this Haruhi Route
    /// </summary>
    public List<RouteEvent> HaruhiRoute { get; set; } = [];

    /// <summary>
    /// Parameterless constructor for serialization
    /// </summary>
    public PuzzleHaruhiRoute()
    {
    }
    
    /// <summary>
    /// Constructs a Haruhi Route
    /// </summary>
    /// <param name="data">The data from the puzzle file</param>
    public PuzzleHaruhiRoute(byte[] data)
    {
        for (int i = 0; i < data.Length - 1; i += 2)
        {
            HaruhiRoute.Add(new() { EventType = (RouteEventType)data[i], AssociatedTopicIndex = data[i + 1] });
        }
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        string str = string.Empty;

        foreach (RouteEvent e in HaruhiRoute)
        {
            switch (e.EventType)
            {
                case RouteEventType.NOTHING:
                    str += "-";
                    break;
                case RouteEventType.TOPIC:
                    str += "T";
                    break;
                case RouteEventType.ACCIDENT:
                    str += "A";
                    break;
            }
        }

        return str;
    }
}

/// <summary>
/// Representation of a puzzle's settings section
/// </summary>
public class PuzzleSettings
{
    /// <summary>
    /// The ID of the map to use for the puzzle
    /// </summary>
    public int MapId { get; set; }
    /// <summary>
    /// The base amount of time to solve the puzzle; will be modified by the Haruhi Meter
    /// </summary>
    public int BaseTime { get; set; }
    /// <summary>
    /// The number of singularities to place in the puzzle
    /// </summary>
    public int NumSingularities { get; set; }
    /// <summary>
    /// Unknown
    /// </summary>
    public int Unknown04 { get; set; }
    /// <summary>
    /// The number of singularities which need to be cleared in order for the player to clear the puzzle
    /// </summary>
    public int TargetNumber { get; set; }
    /// <summary>
    /// If true, failing the puzzle will not result in game over (the story will continue; used in the tutorial puzzle)
    /// </summary>
    public bool ContinueOnFailure { get; set; }
    /// <summary>
    /// The index of the character who will accompany Haruhi while the puzzle is solved (1 = KYON, 2 = HARUHI, 3 = MIKURU, 4 = NAGATO, 5 = KOIZUMI, 22 = ANY)
    /// </summary>
    public Speaker AccompanyingCharacter { get; set; }
    /// <summary>
    /// The index of the first character whose powers can be used (3 = MIKURU, 4 = NAGATO, 5 = KOIZUMI, 22 = ANY)
    /// </summary>
    public Speaker PowerCharacter1 { get; set; }
    /// <summary>
    /// The name the first power character
    /// </summary>
    public Speaker PowerCharacter2 { get; set; }
    /// <summary>
    /// The name the second power character
    /// </summary>
    public int SingularityTexture { get; set; }
    /// <summary>
    /// The grp.bin index of the layout to use for singularities
    /// </summary>
    public int SingularityLayout { get; set; }
    /// <summary>
    /// The grp.bin index of the first animation to use for singularities
    /// </summary>
    public int SingularityAnim1 { get; set; }
    /// <summary>
    /// The grp.bin index of the second animation to use for singularities
    /// </summary>
    public int SingularityAnim2 { get; set; }
    /// <summary>
    /// The topic set to for this puzzle
    /// </summary>
    public int TopicSet { get; set; }
    /// <summary>
    /// Unknown
    /// </summary>
    public int Unknown15 { get; set; }
    /// <summary>
    /// Unknown
    /// </summary>
    public int Unknown16 { get; set; }
    /// <summary>
    /// Unknown
    /// </summary>
    public int Unknown17 { get; set; }
    internal int PointersSectionOffset { get; set; }

    /// <summary>
    /// Parameterless constructor for serialization
    /// </summary>
    public PuzzleSettings()
    {
    }

    /// <summary>
    /// Constructs puzzle settings from binary data
    /// </summary>
    /// <param name="data"></param>
    public PuzzleSettings(byte[] data)
    {
        MapId = IO.ReadInt(data, 0);
        BaseTime = IO.ReadInt(data, 0x04);
        NumSingularities = IO.ReadInt(data, 0x08);
        Unknown04 = IO.ReadInt(data, 0x0C);
        TargetNumber = IO.ReadInt(data, 0x10);
        ContinueOnFailure = IO.ReadInt(data, 0x14) > 0;
        AccompanyingCharacter = (Speaker)IO.ReadInt(data, 0x18);
        PowerCharacter1 = (Speaker)IO.ReadInt(data, 0x1C);
        PowerCharacter2 = (Speaker)IO.ReadInt(data, 0x20);
        SingularityTexture = IO.ReadInt(data, 0x24);
        SingularityLayout = IO.ReadInt(data, 0x28);
        SingularityAnim1 = IO.ReadInt(data, 0x2C);
        SingularityAnim2 = IO.ReadInt(data, 0x30);
        TopicSet = IO.ReadInt(data, 0x34);
        Unknown15 = IO.ReadInt(data, 0x38);
        Unknown16 = IO.ReadInt(data, 0x3C);
        Unknown17 = IO.ReadInt(data, 0x40);
        PointersSectionOffset = IO.ReadInt(data, 0x44);
    }
    
    /// <summary>
    /// Gets the name of the map by pulling from QMAP.S
    /// </summary>
    /// <param name="qmapData">QMap binary data</param>
    /// <returns>The name of the map this puzzle uses</returns>
    public string GetMapName(IEnumerable<byte> qmapData)
    {
        return Encoding.ASCII.GetString(
            qmapData.Skip(BitConverter.ToInt32(qmapData.Skip(0x14 + MapId * 8).Take(4).ToArray()))
                .TakeWhile(b => b != 0).ToArray());
    }
}

/// <summary>
/// A simple record stored in puzzle items that relates to topics
/// </summary>
/// <param name="Topic">The topic ID the puzzle uses</param>
/// <param name="Unknown">Unknown</param>
public record PuzzleTopicRecord(int Topic, int Unknown)
{
    /// <summary>
    /// The topic ID the puzzle uses
    /// </summary>
    public int Topic { get; set; }
    /// <summary>
    /// Unknown
    /// </summary>
    public int Unknown { get; set; }
}