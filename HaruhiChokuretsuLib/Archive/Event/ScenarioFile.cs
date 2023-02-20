using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Util;
using System;
using System.Collections.Generic;
using System.Data;
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
        public List<short> UnknownShorts { get; set; } = new();

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

            for (int i = 0; i < 6; i++)
            {
                UnknownShorts.Add(IO.ReadShort(data, sections[0].Pointer + 0x10 + i * 2));
            }
        }

        public string GetSource(Dictionary<string, IncludeEntry[]> includes, ILogger log)
        {
            if (!includes.ContainsKey("EVTBIN"))
            {
                log.LogError("Includes needs EVTBIN to be present.");
                return null;
            }
            if (!includes.ContainsKey("DATBIN"))
            {
                log.LogError("Includes needs DATBIN to be present.");
                return null;
            }

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            StringBuilder sb = new();

            sb.AppendLine(ScenarioCommand.GetMacros());

            int numSections = Selects.Sum(s => s.RouteSelections.Where(rs => rs is not null).Sum(rs => rs.Routes.Count)) // all route shorts headers
                + Selects.Sum(s => s.RouteSelections.Where(rs => rs is not null).Count()) // all route selections
                + 3; // scenario holder + commands holder + settings
            sb.AppendLine($".word {numSections}");
            sb.AppendLine(".word END_POINTERS");
            sb.AppendLine(".word FILE_START");

            // Define sections
            sb.AppendLine(".word SETTINGS");
            sb.AppendLine(".word 1");

            bool first = true;
            foreach (ScenarioSelectionStruct select in Selects)
            {
                foreach (ScenarioRouteStruct route in select.RouteSelections.Where(rs => rs is not null).SelectMany(rs => rs.Routes))
                {
                    if (first)
                    {
                        first = false;
                        continue;
                    }
                    sb.AppendLine($".word SHORTSHEADER{route.RouteTitleIndex:D2}");
                    sb.AppendLine($".word {route.UnknownShortsHeader.Count}");
                }
                
                foreach (ScenarioRouteSelectionStruct routeSelection in select.RouteSelections.Where(rs => rs is not null))
                {
                    sb.AppendLine($".word ROUTESELECTION{routeSelection.TitleIndex:D2}");
                    sb.AppendLine(".word 1");
                }
            }

            sb.AppendLine(".word SELECTS");
            sb.AppendLine($".word {Selects.Count + 1}");
            sb.AppendLine(".word COMMANDS");
            sb.AppendLine($".word {Commands.Count}");
            sb.AppendLine($".word SHORTSHEADER{Selects[0].RouteSelections[0].Routes[0].RouteTitleIndex:D2}");
            sb.AppendLine($".word {Selects[0].RouteSelections[0].Routes[0].UnknownShortsHeader.Count}");
            sb.AppendLine();

            sb.AppendLine("FILE_START:");

            int currentPointer = 0;
            foreach (ScenarioSelectionStruct selection in Selects)
            {
                sb.AppendLine(selection.GetSource(includes, ref currentPointer));
            }

            sb.AppendLine("SELECTS:");
            foreach (ScenarioSelectionStruct selection in Selects)
            {
                sb.AppendLine($".word {selection.RouteSelections.Count}");
                for (int i = 0; i < 4; i++)
                {
                    if (i >= selection.RouteSelections.Count || selection.RouteSelections[i] is null)
                    {
                        sb.AppendLine(".word 0");
                    }
                    else
                    {
                        sb.AppendLine($"POINTER{currentPointer++:D2}: .word ROUTESELECTION{selection.RouteSelections[i].TitleIndex:D2}");
                    }
                }
            }
            sb.AppendLine(".skip 0x14");

            sb.AppendLine("COMMANDS:");
            sb.AppendLine(string.Join("\n", Commands.Select(c => c.GetAsm(3, includes))));

            sb.AppendLine("SETTINGS:");
            sb.AppendLine($"POINTER{currentPointer++:D2}: .word COMMANDS");
            sb.AppendLine($"POINTER{currentPointer++:D2}: .word SELECTS");
            sb.AppendLine($".word {Commands.Count - 1}");
            sb.AppendLine($".word {Selects.Count}");

            foreach (short unknownShort in UnknownShorts)
            {
                sb.AppendLine($".short {unknownShort}");
            }

            sb.AppendLine("END_POINTERS:");
            sb.AppendLine($".word {currentPointer}");
            for (int i = 0; i < currentPointer; i++)
            {
                sb.AppendLine($".word POINTER{i}");
            }

            return sb.ToString();
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

        public string GetAsm(int indentation, Dictionary<string, IncludeEntry[]> includes)
        {
            return $"{Helpers.Indent(indentation + 1)}{Verb} {GetParameterString(includes)}";
        }

        public static string GetMacros()
        {
            StringBuilder sb = new();

            sb.AppendLine(".include \"DATBIN.INC\"");
            sb.AppendLine(".include \"EVTBIN.INC\"");
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
                9 => $"\"MOVIE{Parameter:D2}\"", // PLAY_VIDEO
                _ => Parameter.ToString(),
            };
            return $"{Verb}({parameterString})";
        }
        public string GetParameterString(Dictionary<string, IncludeEntry[]> includes)
        {
            string parameterString = _verbIndex switch
            {
                2 => $"\"{includes["EVTBIN"].First(i => i.Value == Parameter).Name}\"", // LOAD_SCENE
                3 => $"\"{includes["DATBIN"].First(i => i.Value == Parameter).Name}\"", // PUZZLE_PHASE
                9 => $"\"MOVIE{Parameter:D2}\"", // PLAY_VIDEO
                _ => Parameter.ToString(),
            };
            return parameterString;
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

        public string GetSource(Dictionary<string, IncludeEntry[]> includes, ref int currentPointer)
        {
            StringBuilder sb = new();

            foreach (ScenarioRouteStruct route in RouteSelections.Where(rs => rs is not null).SelectMany(rs => rs.Routes))
            {
                sb.AppendLine($"SHORTSHEADER{route.RouteTitleIndex:D2}:");
                foreach (short s in route.UnknownShortsHeader)
                {
                    sb.AppendLine($"   .short {s}");
                }
                if (route.UnknownShortsHeader.Count % 2 > 0)
                {
                    sb.AppendLine("   .skip 2");
                }
            }

            foreach (ScenarioRouteSelectionStruct routeSelection in RouteSelections.Where(rs => rs is not null))
            {
                sb.AppendLine(routeSelection.GetSource(includes, ref currentPointer));
                sb.AppendLine();
            }

            return sb.ToString();
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

        public (BrigadeMember, BrigadeMember, BrigadeMember) OptimalGroup { get; set; }
        public (BrigadeMember, BrigadeMember, BrigadeMember) WorstGroup { get; set; }
        public BrigadeMember RequiredBrigadeMember { get; set; }
        public bool HaruhiPresent { get; set; }

        public ScenarioRouteSelectionStruct(int dataStartIndex, List<DialogueLine> lines, IEnumerable<byte> data)
        {
            TitleIndex = lines.IndexOf(lines.First(l => l.Pointer == IO.ReadInt(data, dataStartIndex)));
            FutureDescIndex = lines.IndexOf(lines.First(l => l.Pointer == IO.ReadInt(data, dataStartIndex + 0x04)));
            PastDescIndex = lines.IndexOf(lines.First(l => l.Pointer == IO.ReadInt(data, dataStartIndex + 0x08)));
            Title = lines[TitleIndex].Text;
            FutureDesc = lines[FutureDescIndex].Text;
            PastDesc = lines[PastDescIndex].Text;

            OptimalGroup = ((BrigadeMember)IO.ReadInt(data, dataStartIndex + 0x0C), (BrigadeMember)IO.ReadInt(data, dataStartIndex + 0x10), (BrigadeMember)IO.ReadInt(data, dataStartIndex + 0x14));
            WorstGroup = ((BrigadeMember)IO.ReadInt(data, dataStartIndex + 0x18), (BrigadeMember)IO.ReadInt(data, dataStartIndex + 0x1C), (BrigadeMember)IO.ReadInt(data, dataStartIndex + 0x20));
            RequiredBrigadeMember = (BrigadeMember)IO.ReadInt(data, dataStartIndex + 0x24);
            HaruhiPresent = IO.ReadInt(data, dataStartIndex + 0x28) > 0;

            for (int i = 0x2C; IO.ReadInt(data, dataStartIndex + i) > 0; i += 0x10)
            {
                Routes.Add(new(dataStartIndex + i, lines, data));
            }
        }

        public string GetSource(Dictionary<string, IncludeEntry[]> includes, ref int currentPointer)
        {
            StringBuilder sb = new();

            sb.AppendLine($"ROUTESELECTION{TitleIndex:D2}:");
            sb.AppendLine($"   POINTER{currentPointer++}: .word ROUTESELECTIONTITLE{TitleIndex:D2}");
            sb.AppendLine($"   POINTER{currentPointer++}: .word ROUTESELECTIONFUTUREDESC{TitleIndex:D2}");
            sb.AppendLine($"   POINTER{currentPointer++}: .word ROUTESELECTIONPASTDESC{TitleIndex:D2}");
            sb.AppendLine($"   .word {(int)OptimalGroup.Item1}");
            sb.AppendLine($"   .word {(int)OptimalGroup.Item2}");
            sb.AppendLine($"   .word {(int)OptimalGroup.Item3}");
            sb.AppendLine($"   .word {(int)WorstGroup.Item1}");
            sb.AppendLine($"   .word {(int)WorstGroup.Item2}");
            sb.AppendLine($"   .word {(int)WorstGroup.Item3}");
            sb.AppendLine($"   .word {(int)RequiredBrigadeMember}");
            sb.AppendLine($"   .word {(HaruhiPresent ? 1 : 0)}");

            foreach (ScenarioRouteStruct route in Routes)
            {
                sb.AppendLine(route.GetSource(includes, ref currentPointer));
            }

            sb.AppendLine(".word -1");
            sb.AppendLine($".skip {0x100 - Routes.Count * 0x10 - 4}");

            sb.AppendLine($"ROUTESELECTIONTITLE{TitleIndex:D2}: .string \"{Title.EscapeShiftJIS()}\"");
            sb.AsmPadString(Title, Encoding.GetEncoding("Shift-JIS"));
            sb.AppendLine($"ROUTESELECTIONFUTUREDESC{TitleIndex:D2}: .string \"{FutureDesc.EscapeShiftJIS()}\"");
            sb.AsmPadString(FutureDesc, Encoding.GetEncoding("Shift-JIS"));
            sb.AppendLine($"ROUTESELECTIONPASTDESC{TitleIndex:D2}: .string \"{PastDesc.EscapeShiftJIS()}\"");
            sb.AsmPadString(PastDesc, Encoding.GetEncoding("Shift-JIS"));

            foreach (ScenarioRouteStruct route in Routes)
            {
                sb.AppendLine($"ROUTETITLE{route.RouteTitleIndex:D2}: .string \"{route.Title.EscapeShiftJIS()}\"");
                sb.AsmPadString(route.Title, Encoding.GetEncoding("Shift-JIS"));
            }

            return sb.ToString();
        }

        public override string ToString()
        {
            return Title;
        }

        public enum BrigadeMember
        {
            ANY = -1,
            MIKURU = 3,
            NAGATO = 4,
            KOIZUMI = 5,
            NONE = 22,
        }
    }

    public class ScenarioRouteStruct
    {
        public short ScriptIndex { get; set; }
        public short UnknownShort { get; set; }
        public List<short> UnknownShortsHeader { get; set; } = new();
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

            int pointerToShortArray = IO.ReadInt(data, dataStartIndex + 8);
            int currentShortOffset = 0;
            do
            {
                UnknownShortsHeader.Add(IO.ReadShort(data, pointerToShortArray + currentShortOffset));
                currentShortOffset += 2;
            } while (UnknownShortsHeader.Last() > 0);

            RouteTitleIndex = lines.IndexOf(lines.First(l => l.Pointer == BitConverter.ToInt32(data.Skip(dataStartIndex + 12).Take(4).ToArray())));
            Title = lines[RouteTitleIndex].Text;
        }

        private int GetCharactersInvolvedFlag()
        {
            int flag = 0;

            foreach (Speaker character in CharactersInvolved)
            {
                flag |= (byte)Enum.Parse<CharacterMask>(character.ToString());
            }

            return flag;
        }

        public string GetSource(Dictionary<string, IncludeEntry[]> includes, ref int currentPointer)
        {
            StringBuilder sb = new();

            sb.AppendLine($".word {GetCharactersInvolvedFlag()}");
            sb.AppendLine($".short {includes["EVTBIN"].First(i => i.Value == ScriptIndex).Name}");
            sb.AppendLine($".short {UnknownShort}");
            sb.AppendLine($"POINTER{currentPointer++}: .word SHORTSHEADER{RouteTitleIndex:D2}");
            sb.AppendLine($"POINTER{currentPointer++}: .word ROUTETITLE{RouteTitleIndex:D2}");

            return sb.ToString();
        }

        public override string ToString()
        {
            return Title;
        }
    }
}
