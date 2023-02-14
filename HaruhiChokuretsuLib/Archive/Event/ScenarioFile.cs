using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace HaruhiChokuretsuLib.Archive.Event
{
    public partial class EventFile
    {
        public ScenarioStruct Scenario { get; set; }
    }

    public class ScenarioStruct
    {
        public List<ScenarioCommand> Commands { get; set; } = new();
        public List<ScenarioSelectionStruct> Selects { get; set; } = new();

        public ScenarioStruct(IEnumerable<byte> data, List<DialogueLine> lines, List<EventFileSection> sections)
        {
            int commandsOffset = IO.ReadInt(data, sections[0].Pointer);
            int commandsCount = IO.ReadInt(data, sections[0].Pointer + 0x08);
            int selectionsOffset = IO.ReadInt(data, sections[0].Pointer + 0x04);
            int selectionsCount = IO.ReadInt(data, sections[0].Pointer + 0x0C);

            for (int i = 0; i <= commandsCount; i++)
            {
                Commands.Add(new(data.Skip(commandsOffset + i * 4).Take(4)));
            }

            for (int i = 0; i < selectionsCount; i++)
            {
                int[] routeSelectionOffsets = new int[IO.ReadInt(data, selectionsOffset + i * 0x14)];
                for (int j = 0; j < routeSelectionOffsets.Length; j++)
                {
                    routeSelectionOffsets[j] = IO.ReadInt(data, selectionsOffset + i * 0x14 + (j + 1) * 4);
                }
                Selects.Add(new(routeSelectionOffsets, lines, data));
            }
        }
    }

    public class ScenarioCommand
    {
        public enum ScenarioVerb : short
        {
            NEW_GAME,
            SAVE,
            LOAD_SCENE,
            PUZZLE_PHASE,
            ROUTE_SELECT,
            STOP,
            SAVE2,
            TOPICS,
            COMPANION_SELECT,
            PLAY_VIDEO,
            NOP,
            UNKNOWN0B,
            UNLOCK,
            END,
        };
        private short _verbIndex;

        public ScenarioVerb Verb { get => (ScenarioVerb)_verbIndex; set => _verbIndex = (short)value; }
        public int Parameter { get; set; } = new();

        public ScenarioCommand(ScenarioVerb verb, int parameter)
        {
            _verbIndex = (short)verb;
            Parameter = parameter;
        }

        public ScenarioCommand(IEnumerable<byte> data)
        {
            _verbIndex = BitConverter.ToInt16(data.Take(2).ToArray());
            Parameter = BitConverter.ToInt16(data.Skip(2).Take(2).ToArray());
        }

        public string GetAsm(int indentation, ArchiveFile<EventFile> evt, ArchiveFile<DataFile> dat)
        {
            return $"{Helpers.Indent(indentation + 1)}{Verb} {GetParameterString(evt, dat)}";
        }

        public static string GetMacros()
        {
            StringBuilder sb = new();

            sb.AppendLine(".include DATBIN.INC");
            sb.AppendLine(".include EVTBIN.INC");
            sb.AppendLine(".set MOVIE00, 0");
            sb.AppendLine(".set MOVIE01, 1");

            foreach (ScenarioVerb verb in Enum.GetValues<ScenarioVerb>())
            {
                sb.AppendLine($".macro {verb} p");
                sb.AppendLine($"   .short {(short)verb}");
                sb.AppendLine("   .short \\p");
                sb.AppendLine(".endm");
                sb.AppendLine();
            }

            return sb.ToString();
        }

        public override string ToString()
        {
            return $"{Verb}({Parameter})";
        }

        public string GetParameterString(ArchiveFile<EventFile> evt, ArchiveFile<DataFile> dat)
        {
            string parameterString = _verbIndex switch
            {
                2 => $"\"{evt.Files.First(f => f.Index == Parameter).Name[0..^1]}\" ({Parameter})", // LOAD_SCENE
                3 => $"\"{dat.Files.First(f => f.Index == Parameter).Name[0..^1]}\"", // PUZZLE_PHASE
                9 => $"\"MOVIE{Parameter}\"", // PLAY_VIDEO
                _ => Parameter.ToString(),
            };
            return $"{Verb}({parameterString})";
        }
    }

    public class ScenarioSelectionStruct
    {
        public List<ScenarioRouteSelectionStruct> RouteSelections { get; set; } = new();

        public ScenarioSelectionStruct(int[] routeSelectionOffsets, List<DialogueLine> lines, IEnumerable<byte> data)
        {
            for (int i = 0; i < routeSelectionOffsets.Length; i++)
            {
                if (routeSelectionOffsets[i] == 0)
                {
                    RouteSelections.Add(null);
                }
                else
                {
                    RouteSelections.Add(new(routeSelectionOffsets[i], lines, data));
                }
            }
        }
    }

    public class ScenarioRouteSelectionStruct
    {
        public List<ScenarioRouteStruct> Routes { get; set; } = new();
        public int TitleIndex { get; set; }
        public string Title { get; }
        public int FutureDescIndex { get; set; }
        public string FutureDesc { get; }
        public int PastDescIndex { get; set; }
        public string PastDesc { get; }

        public int UnknownInt1 { get; set; }
        public int UnknownInt2 { get; set; }
        public int UnknownInt3 { get; set; }
        public int UnknownInt4 { get; set; }
        public int UnknownInt5 { get; set; }
        public int UnknownInt6 { get; set; }
        public string RequiredBrigadeMember { get; set; }
        public bool HaruhiPresent { get; set; }

        public ScenarioRouteSelectionStruct(int dataStartIndex, List<DialogueLine> lines, IEnumerable<byte> data)
        {
            TitleIndex = lines.IndexOf(lines.First(l => l.Pointer == IO.ReadInt(data, dataStartIndex)));
            FutureDescIndex = lines.IndexOf(lines.First(l => l.Pointer == IO.ReadInt(data, dataStartIndex + 0x04)));
            PastDescIndex = lines.IndexOf(lines.First(l => l.Pointer == IO.ReadInt(data, dataStartIndex + 0x08)));
            Title = lines[TitleIndex].Text;
            FutureDesc = lines[FutureDescIndex].Text;
            PastDesc = lines[PastDescIndex].Text;

            UnknownInt1 = IO.ReadInt(data, dataStartIndex + 0x0C);
            UnknownInt2 = IO.ReadInt(data,dataStartIndex + 0x10);
            UnknownInt3 = IO.ReadInt(data, dataStartIndex + 0x14);
            UnknownInt4 = IO.ReadInt(data, dataStartIndex + 0x18);
            UnknownInt5 = IO.ReadInt(data, dataStartIndex + 0x1C);
            UnknownInt6 = IO.ReadInt(data, dataStartIndex + 0x20);
            switch (IO.ReadInt(data, dataStartIndex + 0x24))
            {
                case -1:
                    RequiredBrigadeMember = "ANY";
                    break;
                case 3:
                    RequiredBrigadeMember = "MIKURU";
                    break;
                case 4:
                    RequiredBrigadeMember = "NAGATO";
                    break;
                case 5:
                    RequiredBrigadeMember = "KOIZUMI";
                    break;
                case 22:
                    RequiredBrigadeMember = "NONE";
                    break;
            }
            HaruhiPresent = IO.ReadInt(data, dataStartIndex + 0x28) > 0;

            for (int i = 0x2C; IO.ReadInt(data, dataStartIndex + i) > 0; i += 0x10)
            {
                Routes.Add(new(dataStartIndex + i, lines, data));
            }
        }

        public override string ToString()
        {
            return Title;
        }
    }

    public class ScenarioRouteStruct
    {
        public short ScriptIndex { get; set; }
        public short UnknownShort { get; set; }
        public int UnknownPointer { get; set; }
        public int RouteTitleIndex { get; set; }
        public string Title { get; }
        public List<Speaker> CharactersInvolved { get; set; } = new();

        [Flags]
        public enum CharacterMask : byte
        {
            KYON = 0b0000_0010,
            HARUHI = 0b0000_0100,
            MIKURU = 0b0000_1000,
            NAGATO = 0b0001_0000,
            KOIZUMI = 0b0010_0000,
        }

        public ScenarioRouteStruct(int dataStartIndex, List<DialogueLine> lines, IEnumerable<byte> data)
        {
            CharacterMask charactersInvolved = (CharacterMask)IO.ReadInt(data, dataStartIndex);

            if (charactersInvolved.HasFlag(CharacterMask.KYON))
            {
                CharactersInvolved.Add(Speaker.KYON);
            }
            if (charactersInvolved.HasFlag(CharacterMask.HARUHI))
            {
                CharactersInvolved.Add(Speaker.HARUHI);
            }
            if (charactersInvolved.HasFlag(CharacterMask.MIKURU))
            {
                CharactersInvolved.Add(Speaker.MIKURU);
            }
            if (charactersInvolved.HasFlag(CharacterMask.NAGATO))
            {
                CharactersInvolved.Add(Speaker.NAGATO);
            }
            if (charactersInvolved.HasFlag(CharacterMask.KOIZUMI))
            {
                CharactersInvolved.Add(Speaker.KOIZUMI);
            }

            ScriptIndex = IO.ReadShort(data, dataStartIndex + 4);
            UnknownShort = IO.ReadShort(data, dataStartIndex + 6);
            UnknownPointer = IO.ReadInt(data, dataStartIndex + 8);
            RouteTitleIndex = lines.IndexOf(lines.First(l => l.Pointer == BitConverter.ToInt32(data.Skip(dataStartIndex + 12).Take(4).ToArray())));
            Title = lines[RouteTitleIndex].Text;
        }

        public override string ToString()
        {
            return Title;
        }
    }
}
