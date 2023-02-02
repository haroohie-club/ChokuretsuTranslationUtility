using HaruhiChokuretsuLib.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HaruhiChokuretsuLib.Archive.Data
{
    public class PuzzleFile : DataFile
    {
        public List<(int Offset, int ItemCount)> SectionOffsetsAndCounts { get; set; } = new();

        public List<(int Topic, int Unknown)> AssociatedTopics { get; set; } = new();
        public List<PuzzleHaruhiRoute> HaruhiRoutes { get; set; } = new();
        public PuzzleSettings Settings { get; set; }

        public override void Initialize(byte[] decompressedData, int offset, ILogger log)
        {
            _log = log;
            Offset = offset;
            Data = decompressedData.ToList();

            int numSections = IO.ReadInt(decompressedData, 0x00);
            if (numSections != 13)
            {
                _log.LogError($"Detected more than 13 sections in a puzzle file ({numSections} detected)");
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

        public override string GetSource(Dictionary<string, IncludeEntry[]> includes)
        {
            if (!includes.ContainsKey("GRPBIN"))
            {
                _log.LogError("Includes needs GRPBIN to be present.");
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

            foreach (var mainTopic in AssociatedTopics)
            {
                sb.AppendLine($"   .word {mainTopic.Topic}");
                sb.AppendLine($"   .word {mainTopic.Unknown}");
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
            sb.AppendLine($"   .word {(Settings.AccompanyingCharacter.StartsWith("UNKNOWN") ? Settings.AccompanyingCharacter[(Settings.AccompanyingCharacter.IndexOf('(') + 1)..(Settings.AccompanyingCharacter.LastIndexOf(')'))] : Settings.AccompanyingCharacter)}");
            sb.AppendLine($"   .word {(Settings.PowerCharacter1.StartsWith("UNKNOWN") ? Settings.AccompanyingCharacter[(Settings.PowerCharacter1.IndexOf('(') + 1)..(Settings.PowerCharacter1.LastIndexOf(')'))] : Settings.PowerCharacter1)}");
            sb.AppendLine($"   .word {(Settings.PowerCharacter2.StartsWith("UNKNOWN") ? Settings.AccompanyingCharacter[(Settings.PowerCharacter2.IndexOf('(') + 1)..(Settings.PowerCharacter1.LastIndexOf(')'))] : Settings.PowerCharacter2)}");
            sb.AppendLine($"   .word {includes["GRPBIN"].First(i => i.Value == Settings.SingularityTexture).Name}");
            sb.AppendLine($"   .word {includes["GRPBIN"].First(i => i.Value == Settings.SingularityLayout).Name}");
            sb.AppendLine($"   .word {(Settings.SingularityAnim1 - 1 > 0 ? includes["GRPBIN"].First(i => i.Value == Settings.SingularityAnim1).Name : 0)}");
            sb.AppendLine($"   .word {(Settings.SingularityAnim2 - 1 > 0 ? includes["GRPBIN"].First(i => i.Value == Settings.SingularityAnim2).Name : 0)}");
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

    public class PuzzleHaruhiRoute
    {
        public enum RouteEventType : byte
        {
            NOTHING,
            TOPIC,
            ACCIDENT
        }

        public struct RouteEvent
        {
            public RouteEventType EventType;
            public byte AssociatedTopicIndex;
        }

        public List<RouteEvent> HaruhiRoute { get; set; } = new();

        public PuzzleHaruhiRoute(byte[] data)
        {
            for (int i = 0; i < data.Length - 1; i += 2)
            {
                HaruhiRoute.Add(new() { EventType = (RouteEventType)data[i], AssociatedTopicIndex = data[i + 1] });
            }
        }

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

    public class PuzzleSettings
    {
        public int MapId { get; set; }
        public int BaseTime { get; set; }
        public int NumSingularities { get; set; }
        public int Unknown04 { get; set; }
        public int TargetNumber { get; set; }
        public bool ContinueOnFailure { get; set; }
        public string AccompanyingCharacter { get; set; }
        public string PowerCharacter1 { get; set; }
        public string PowerCharacter2 { get; set; }
        public int SingularityTexture { get; set; }
        public int SingularityLayout { get; set; }
        public int SingularityAnim1 { get; set; }
        public int SingularityAnim2 { get; set; }
        public int TopicSet { get; set; }
        public int Unknown15 { get; set; }
        public int Unknown16 { get; set; }
        public int Unknown17 { get; set; }
        public int PointersSectionOffset { get; set; }

        public PuzzleSettings(IEnumerable<byte> data)
        {
            MapId = IO.ReadInt(data, 0);
            BaseTime = IO.ReadInt(data, 0x04);
            NumSingularities = IO.ReadInt(data, 0x08);
            Unknown04 = IO.ReadInt(data, 0x0C);
            TargetNumber = IO.ReadInt(data, 0x10);
            ContinueOnFailure = IO.ReadInt(data, 0x14) > 0;
            AccompanyingCharacter = CharacterSwitch(IO.ReadInt(data, 0x18));
            PowerCharacter1 = CharacterSwitch(IO.ReadInt(data, 0x1C));
            PowerCharacter2 = CharacterSwitch(IO.ReadInt(data, 0x20));
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

        public string GetMapName(List<byte> qmapData)
        {
            return Encoding.ASCII.GetString(
                qmapData.Skip(BitConverter.ToInt32(qmapData.Skip(0x14 + MapId * 8).Take(4).ToArray()))
                .TakeWhile(b => b != 0).ToArray());
        }

        private static string CharacterSwitch(int characterCode)
        {
            switch (characterCode)
            {
                default:
                    return $"UNKNOWN ({characterCode})";
                case 1:
                    return "KYON";
                case 2:
                    return "HARUHI";
                case 3:
                    return "MIKURU";
                case 4:
                    return "NAGATO";
                case 5:
                    return "KOIZUMI";
                case 22:
                    return "ANY";
            }
        }
    }
}
