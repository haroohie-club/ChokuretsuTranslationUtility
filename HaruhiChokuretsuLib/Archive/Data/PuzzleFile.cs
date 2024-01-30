using HaruhiChokuretsuLib.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HaruhiChokuretsuLib.Archive.Data
{
    /// <summary>
    /// A representation of puzzle files in dat.bin
    /// </summary>
    public class PuzzleFile : DataFile
    {
        internal List<(int Offset, int ItemCount)> SectionOffsetsAndCounts { get; set; } = [];

        /// <summary>
        /// The list of the puzzle's associated topics
        /// </summary>
        public List<(int Topic, int Unknown)> AssociatedTopics { get; set; } = [];
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

            Settings = new(Data.Skip(SectionOffsetsAndCounts[0].Offset).Take(0x48));
            for (int i = 1; i < 11; i++)
            {
                HaruhiRoutes.Add(new(Data.Skip(SectionOffsetsAndCounts[i].Offset).Take(SectionOffsetsAndCounts[i].ItemCount * 2).ToArray()));
            }
            for (int i = 0; i < SectionOffsetsAndCounts[12].ItemCount; i++)
            {
                AssociatedTopics.Add((IO.ReadInt(Data, SectionOffsetsAndCounts[12].Offset + i * 8), IO.ReadInt(Data, SectionOffsetsAndCounts[12].Offset + i * 8 + 4)));
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

            sb.AppendLine(".set KYON, 1");
            sb.AppendLine(".set HARUHI, 2");
            sb.AppendLine(".set MIKURU, 3");
            sb.AppendLine(".set NAGATO, 4");
            sb.AppendLine(".set KOIZUMI, 5");
            sb.AppendLine(".set ANY, 22");
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
            sb.AppendLine($"   .word {(Settings.AccompanyingCharacterName.StartsWith("UNKNOWN") ? Settings.AccompanyingCharacter : Settings.AccompanyingCharacterName)}");
            sb.AppendLine($"   .word {(Settings.PowerCharacter1Name.StartsWith("UNKNOWN") ? Settings.PowerCharacter1 : Settings.PowerCharacter1Name)}");
            sb.AppendLine($"   .word {(Settings.PowerCharacter2Name.StartsWith("UNKNOWN") ? Settings.PowerCharacter2 : Settings.PowerCharacter2Name)}");
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
    public class PuzzleSettings(IEnumerable<byte> data)
    {
        /// <summary>
        /// The ID of the map to use for the puzzle
        /// </summary>
        public int MapId { get; set; } = IO.ReadInt(data, 0);
        /// <summary>
        /// The base amount of time to solve the puzzle; will be modified by the Haruhi Meter
        /// </summary>
        public int BaseTime { get; set; } = IO.ReadInt(data, 0x04);
        /// <summary>
        /// The number of singularities to place in the puzzle
        /// </summary>
        public int NumSingularities { get; set; } = IO.ReadInt(data, 0x08);
        /// <summary>
        /// Unknown
        /// </summary>
        public int Unknown04 { get; set; } = IO.ReadInt(data, 0x0C);
        /// <summary>
        /// The number of singularities which need to be cleared in order for the player to clear the puzzle
        /// </summary>
        public int TargetNumber { get; set; } = IO.ReadInt(data, 0x10);
        /// <summary>
        /// If true, failing the puzzle will not result in game over (the story will continue; used in the tutorial puzzle)
        /// </summary>
        public bool ContinueOnFailure { get; set; } = IO.ReadInt(data, 0x14) > 0;
        /// <summary>
        /// The index of the character who will accompany Haruhi while the puzzle is solved (1 = KYON, 2 = HARUHI, 3 = MIKURU, 4 = NAGATO, 5 = KOIZUMI, 22 = ANY)
        /// </summary>
        public int AccompanyingCharacter { get; set; } = IO.ReadInt(data, 0x18);
        /// <summary>
        /// The name the accompanying character
        /// </summary>
        public string AccompanyingCharacterName => CharacterSwitch(AccompanyingCharacter);
        /// <summary>
        /// The index of the first character whose powers can be used (3 = MIKURU, 4 = NAGATO, 5 = KOIZUMI, 22 = ANY)
        /// </summary>
        public int PowerCharacter1 { get; set; } = IO.ReadInt(data, 0x1C);
        /// <summary>
        /// The name the first power character
        /// </summary>
        public string PowerCharacter1Name => CharacterSwitch(PowerCharacter1);
        /// <summary>
        /// The index of the second character whose powers can be used (3 = MIKURU, 4 = NAGATO, 5 = KOIZUMI, 22 = ANY)
        /// </summary>
        public int PowerCharacter2 { get; set; } = IO.ReadInt(data, 0x20);
        /// <summary>
        /// The name the second power character
        /// </summary>
        public string PowerCharacter2Name => CharacterSwitch(PowerCharacter2);
        /// <summary>
        /// The grp.bin index of the texture to use for singularities
        /// </summary>
        public int SingularityTexture { get; set; } = IO.ReadInt(data, 0x24);
        /// <summary>
        /// The grp.bin index of the layout to use for singularities
        /// </summary>
        public int SingularityLayout { get; set; } = IO.ReadInt(data, 0x28);
        /// <summary>
        /// The grp.bin index of the first animation to use for singularities
        /// </summary>
        public int SingularityAnim1 { get; set; } = IO.ReadInt(data, 0x2C);
        /// <summary>
        /// The grp.bin index of the second animation to use for singularities
        /// </summary>
        public int SingularityAnim2 { get; set; } = IO.ReadInt(data, 0x30);
        /// <summary>
        /// The topic set to for this puzzle
        /// </summary>
        public int TopicSet { get; set; } = IO.ReadInt(data, 0x34);
        /// <summary>
        /// Unknown
        /// </summary>
        public int Unknown15 { get; set; } = IO.ReadInt(data, 0x38);
        /// <summary>
        /// Unknown
        /// </summary>
        public int Unknown16 { get; set; } = IO.ReadInt(data, 0x3C);
        /// <summary>
        /// Unknown
        /// </summary>
        public int Unknown17 { get; set; } = IO.ReadInt(data, 0x40);
        internal int PointersSectionOffset { get; set; } = IO.ReadInt(data, 0x44);

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

        private static string CharacterSwitch(int characterCode)
        {
            return characterCode switch
            {
                1 => "KYON",
                2 => "HARUHI",
                3 => "MIKURU",
                4 => "NAGATO",
                5 => "KOIZUMI",
                22 => "ANY",
                _ => $"UNKNOWN ({characterCode})",
            };
        }
    }
}
