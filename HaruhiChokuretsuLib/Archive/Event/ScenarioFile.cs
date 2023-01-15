using HaruhiChokuretsuLib.Archive.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

        public ScenarioStruct(List<byte> data, int commandOffset, int selectsOffset)
        {
            Commands.Add(new(data.Skip(commandOffset).Take(4)));
            for (int i = 4; Commands.Last().Verb != "END"; i += 4)
            {
                Commands.Add(new(data.Skip(commandOffset + i).Take(4)));
            }
        }
    }

    public class ScenarioCommand
    {
        private static string[] VERBS = new string[]
        {
            "NEW_GAME",
            "SAVE",
            "LOAD_SCENE",
            "PUZZLE_PHASE",
            "ROUTE_SELECT",
            "STOP",
            "SAVE2",
            "TOPICS",
            "COMPANION_SELECT",
            "PLAY_VIDEO",
            "NOP",
            "UNKNOWN0B",
            "UNLOCK",
            "END",

        };
        private short _verbIndex;

        public string Verb => VERBS[_verbIndex];
        public int Parameter { get; set; } = new();

        public ScenarioCommand(IEnumerable<byte> data)
        {
            _verbIndex = BitConverter.ToInt16(data.Take(2).ToArray());
            Parameter = BitConverter.ToInt16(data.Skip(2).Take(2).ToArray());
        }

        public string GetAsm(int indentation)
        {
            return $"{string.Join(" ", new string[indentation + 1])}{Verb} {Parameter}";
        }

        public static string GetMacros()
        {
            StringBuilder sb = new();

            for (int i = 0; i < VERBS.Length; i++)
            {
                sb.AppendLine($".macro {VERBS[i]} p");
                sb.AppendLine($"   .short {i}");
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
                9 => $"\"MOVIE{Parameter}.MODS\"", // PLAY_VIDEO
                _ => Parameter.ToString(),
            };
            return $"{Verb}({parameterString})";
        }
    }

    public class ScenarioSelectionStruct
    {
        public List<ScenarioRouteSelectionStruct> RouteSelections { get; set; } = new();
        public int NumRoutes { get; set; }
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

        public ScenarioRouteSelectionStruct(int dataStartIndex, List<DialogueLine> lines, List<byte> data)
        {
            TitleIndex = lines.IndexOf(lines.First(l => l.Pointer == BitConverter.ToInt32(data.Skip(dataStartIndex).Take(4).ToArray())));
            FutureDescIndex = lines.IndexOf(lines.First(l => l.Pointer == BitConverter.ToInt32(data.Skip(dataStartIndex + 0x04).Take(4).ToArray())));
            PastDescIndex = lines.IndexOf(lines.First(l => l.Pointer == BitConverter.ToInt32(data.Skip(dataStartIndex + 0x08).Take(4).ToArray())));
            Title = lines[TitleIndex].Text;
            FutureDesc = lines[FutureDescIndex].Text;
            PastDesc = lines[PastDescIndex].Text;

            UnknownInt1 = BitConverter.ToInt32(data.Skip(dataStartIndex + 0x0C).Take(4).ToArray());
            UnknownInt2 = BitConverter.ToInt32(data.Skip(dataStartIndex + 0x10).Take(4).ToArray());
            UnknownInt3 = BitConverter.ToInt32(data.Skip(dataStartIndex + 0x14).Take(4).ToArray());
            UnknownInt4 = BitConverter.ToInt32(data.Skip(dataStartIndex + 0x18).Take(4).ToArray());
            UnknownInt5 = BitConverter.ToInt32(data.Skip(dataStartIndex + 0x1C).Take(4).ToArray());
            UnknownInt6 = BitConverter.ToInt32(data.Skip(dataStartIndex + 0x20).Take(4).ToArray());
            switch (BitConverter.ToInt32(data.Skip(dataStartIndex + 0x24).Take(4).ToArray()))
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
            HaruhiPresent = BitConverter.ToInt32(data.Skip(dataStartIndex + 0x28).Take(4).ToArray()) > 0;

            for (int i = 0x2C; BitConverter.ToInt32(data.Skip(dataStartIndex + i).Take(4).ToArray()) > 0; i += 0x10)
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

        public ScenarioRouteStruct(int dataStartIndex, List<DialogueLine> lines, List<byte> data)
        {
            CharacterMask charactersInvolved = (CharacterMask)BitConverter.ToInt32(data.Skip(dataStartIndex).Take(4).ToArray());

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

            ScriptIndex = BitConverter.ToInt16(data.Skip(dataStartIndex + 4).Take(2).ToArray());
            UnknownShort = BitConverter.ToInt16(data.Skip(dataStartIndex + 6).Take(2).ToArray());
            UnknownPointer = BitConverter.ToInt32(data.Skip(dataStartIndex + 8).Take(4).ToArray());
            RouteTitleIndex = lines.IndexOf(lines.First(l => l.Pointer == BitConverter.ToInt32(data.Skip(dataStartIndex + 12).Take(4).ToArray())));
            Title = lines[RouteTitleIndex].Text;

        }

        public override string ToString()
        {
            return Title;
        }
    }
}
