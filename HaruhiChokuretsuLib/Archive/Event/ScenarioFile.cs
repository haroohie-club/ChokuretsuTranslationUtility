using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HaruhiChokuretsuLib.Archive.Event
{
    public partial class EventFile
    {
        /// <summary>
        /// If this file is SCENARIO.S this defines the scenario
        /// </summary>
        public ScenarioStruct Scenario { get; set; }
    }

    /// <summary>
    /// A representation of the scenario flow of the game defined in SCENARIO.S
    /// </summary>
    public class ScenarioStruct
    {
        /// <summary>
        /// A list of scenario commands defining the flow of the game
        /// </summary>
        public List<ScenarioCommand> Commands { get; set; } = [];
        /// <summary>
        /// A list of group selections
        /// </summary>
        public List<ScenarioSelection> Selects { get; set; } = [];
        /// <summary>
        /// A list of Kyonless topics
        /// </summary>
        public List<short> KyonlessTopicIds { get; set; } = [];

        /// <summary>
        /// Creates a scenario structure from SCENARIO.S data
        /// </summary>
        /// <param name="data">Binary SCENARIO.S data</param>
        /// <param name="lines">List of dialogue lines for string reference</param>
        /// <param name="sections">List of event file sections</param>
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
                int[] activityOffset = new int[IO.ReadInt(data, selectionsOffset + i * 0x14)];
                for (int j = 0; j < activityOffset.Length; j++)
                {
                    activityOffset[j] = IO.ReadInt(data, selectionsOffset + i * 0x14 + (j + 1) * 4);
                }
                Selects.Add(new(activityOffset, lines, data));
            }

            for (int i = 0; i < 6; i++)
            {
                KyonlessTopicIds.Add(IO.ReadShort(data, sections[0].Pointer + 0x10 + i * 2));
            }
        }

        /// <summary>
        /// Generates ARM assembly source code for the scenario file
        /// </summary>
        /// <param name="includes">List of includes (requires EVTBIN and DATBIN includes)</param>
        /// <param name="log">ILogger instance for logging</param>
        /// <returns>A string with an assembly representation of the scenario file</returns>
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

            int numSections = Selects.Sum(s => s.Activities.Where(rs => rs is not null).Sum(rs => rs.Routes.Count)) // all route shorts headers
                + Selects.Sum(s => s.Activities.Where(rs => rs is not null).Count()) // all route selections
                + 3; // scenario holder + commands holder + settings
            sb.AppendLine($".word {numSections}");
            sb.AppendLine(".word END_POINTERS");
            sb.AppendLine(".word FILE_START");

            // Define sections
            sb.AppendLine(".word SETTINGS");
            sb.AppendLine(".word 1");

            bool first = true;
            foreach (ScenarioSelection select in Selects)
            {
                foreach (ScenarioRoute route in select.Activities.Where(rs => rs is not null).SelectMany(rs => rs.Routes))
                {
                    if (first)
                    {
                        first = false;
                        continue;
                    }
                    sb.AppendLine($".word KYONLESS_TOPICS{route.RouteTitleIndex:D2}");
                    sb.AppendLine($".word {route.KyonlessTopics.Count}");
                }
                
                foreach (ScenarioActivity activity in select.Activities.Where(rs => rs is not null))
                {
                    sb.AppendLine($".word ACTIVITY{activity.TitleIndex:D2}");
                    sb.AppendLine(".word 1");
                }
            }

            sb.AppendLine(".word SELECTS");
            sb.AppendLine($".word {Selects.Count + 1}");
            sb.AppendLine(".word COMMANDS");
            sb.AppendLine($".word {Commands.Count}");
            sb.AppendLine($".word KYONLESS_TOPICS{Selects[0].Activities[0].Routes[0].RouteTitleIndex:D2}");
            sb.AppendLine($".word {Selects[0].Activities[0].Routes[0].KyonlessTopics.Count}");
            sb.AppendLine();

            sb.AppendLine("FILE_START:");

            int currentPointer = 0;
            foreach (ScenarioSelection selection in Selects)
            {
                sb.AppendLine(selection.GetSource(includes, ref currentPointer));
            }

            sb.AppendLine("SELECTS:");
            foreach (ScenarioSelection selection in Selects)
            {
                sb.AppendLine($".word {selection.Activities.Count}");
                for (int i = 0; i < 4; i++)
                {
                    if (i >= selection.Activities.Count || selection.Activities[i] is null)
                    {
                        sb.AppendLine(".word 0");
                    }
                    else
                    {
                        sb.AppendLine($"POINTER{currentPointer++:D2}: .word ACTIVITY{selection.Activities[i].TitleIndex:D2}");
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

            foreach (short topicId in KyonlessTopicIds)
            {
                sb.AppendLine($".short {topicId}");
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

    /// <summary>
    /// A command used in the scenario commands section of SCENARIO.S
    /// </summary>
    public class ScenarioCommand
    {
        /// <summary>
        /// An enum defining the verbs of a scenario command
        /// </summary>
        public enum ScenarioVerb : short
        {
            /// <summary>
            /// Sets the location where selecting an episode from the new game menu will jump to
            /// </summary>
            NEW_GAME,
            /// <summary>
            /// Prompts the user to save the game
            /// </summary>
            SAVE,
            /// <summary>
            /// Loads a specified event file
            /// </summary>
            LOAD_SCENE,
            /// <summary>
            /// Starts the puzzle phase with a specified puzzle
            /// </summary>
            PUZZLE_PHASE,
            /// <summary>
            /// Opens a particular group selection
            /// </summary>
            ROUTE_SELECT,
            /// <summary>
            /// Stops executing the scenario
            /// </summary>
            STOP,
            /// <summary>
            /// Also prompts the user to save (difference from SAVE unknown)
            /// </summary>
            SAVE2,
            /// <summary>
            /// Shows the collected topics screen
            /// </summary>
            TOPICS,
            /// <summary>
            /// Prompts the user to select the character who will accompany Haruhi
            /// </summary>
            COMPANION_SELECT,
            /// <summary>
            /// Streams a video from the cartridge
            /// </summary>
            PLAY_VIDEO,
            /// <summary>
            /// Does nothing
            /// </summary>
            NOP,
            /// <summary>
            /// Unlocks a particular character ending
            /// </summary>
            UNLOCK_ENDINGS,
            /// <summary>
            /// Unlocks an unlockable
            /// </summary>
            UNLOCK,
            /// <summary>
            /// Ends the game and returns to the title screen
            /// </summary>
            END,
        };
        private short _verbIndex;

        /// <summary>
        /// The verb of the scenario command
        /// </summary>
        public ScenarioVerb Verb { get => (ScenarioVerb)_verbIndex; set => _verbIndex = (short)value; }
        /// <summary>
        /// The parameter of the scenario command
        /// </summary>
        public int Parameter { get; set; } = new();

        /// <summary>
        /// Creates a scenario command from a verb and parameter
        /// </summary>
        /// <param name="verb">The command verb</param>
        /// <param name="parameter">The command parameter</param>
        public ScenarioCommand(ScenarioVerb verb, int parameter)
        {
            _verbIndex = (short)verb;
            Parameter = parameter;
        }

        /// <summary>
        /// Creates a scenario command from data
        /// </summary>
        /// <param name="data">The scenario command data from SCENARIO.S</param>
        public ScenarioCommand(IEnumerable<byte> data)
        {
            _verbIndex = BitConverter.ToInt16(data.Take(2).ToArray());
            Parameter = BitConverter.ToInt16(data.Skip(2).Take(2).ToArray());
        }

        internal string GetAsm(int indentation, Dictionary<string, IncludeEntry[]> includes)
        {
            return $"{Helpers.Indent(indentation + 1)}{Verb} {GetParameterString(includes)}";
        }

        internal static string GetMacros()
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

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{Verb}({Parameter})";
        }

        /// <summary>
        /// Gets the stringified version of a particular parameter
        /// </summary>
        /// <param name="evt">ArchiveFile for evt.bin</param>
        /// <param name="dat">ArchiveFile for dat.bin</param>
        /// <returns>String version of a particular parameter</returns>
        public string GetParameterString(ArchiveFile<EventFile> evt, ArchiveFile<DataFile> dat)
        {
            string parameterString = _verbIndex switch
            {
                2 => $"\"{evt.GetFileByIndex(Parameter).Name[0..^1]}\" ({Parameter})", // LOAD_SCENE
                3 => $"\"{dat.GetFileByIndex(Parameter).Name[0..^1]}\"", // PUZZLE_PHASE
                9 => $"\"MOVIE{Parameter:D2}\"", // PLAY_VIDEO
                _ => Parameter.ToString(),
            };
            return $"{Verb}({parameterString})";
        }

        /// <summary>
        /// Gets the stringified version of a particular parameter
        /// </summary>
        /// <param name="includes">Dictionary of includes as would be passed to GetSource()</param>
        /// <returns>String version of a particular parameter</returns>
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

    /// <summary>
    /// Represents a group selection object
    /// </summary>
    public class ScenarioSelection
    {
        /// <summary>
        /// The list of activities (e.g. what you send Kyon and the others to do) available to this group selection
        /// </summary>
        public List<ScenarioActivity> Activities { get; set; } = [];

        /// <summary>
        /// Creates a scenario selection object from SCENARIO.S data
        /// </summary>
        /// <param name="activityOffsets">The oofsets of each route selection in SCENARIO.S</param>
        /// <param name="lines">Dialogue lines for string reference</param>
        /// <param name="data">SCENARIO.S binary data</param>
        public ScenarioSelection(int[] activityOffsets, List<DialogueLine> lines, IEnumerable<byte> data)
        {
            for (int i = 0; i < activityOffsets.Length; i++)
            {
                if (activityOffsets[i] == 0)
                {
                    Activities.Add(null);
                }
                else
                {
                    Activities.Add(new(activityOffsets[i], lines, data));
                }
            }
        }

        internal string GetSource(Dictionary<string, IncludeEntry[]> includes, ref int currentPointer)
        {
            StringBuilder sb = new();

            foreach (ScenarioRoute route in Activities.Where(rs => rs is not null).SelectMany(rs => rs.Routes))
            {
                sb.AppendLine($"KYONLESS_TOPICS{route.RouteTitleIndex:D2}:");
                foreach (short s in route.KyonlessTopics)
                {
                    sb.AppendLine($"   .short {s}");
                }
                if (route.KyonlessTopics.Count % 2 > 0)
                {
                    sb.AppendLine("   .skip 2");
                }
            }

            foreach (ScenarioActivity activity in Activities.Where(rs => rs is not null))
            {
                sb.AppendLine(activity.GetSource(includes, ref currentPointer));
                sb.AppendLine();
            }

            return sb.ToString();
        }
    }

    /// <summary>
    /// Represents a particular route selection in a group selection
    /// </summary>
    public class ScenarioActivity
    {
        /// <summary>
        /// A list of routes available when sending Kyon to do this activity
        /// </summary>
        public List<ScenarioRoute> Routes { get; set; } = [];
        /// <summary>
        /// Dialogue line index of Title
        /// </summary>
        internal int TitleIndex { get; set; }
        /// <summary>
        /// The title of the activity
        /// </summary>
        public string Title { get; set; }

        private int _futureDescIndex;
        /// <summary>
        /// Future tense description of what the activity is
        /// </summary>
        public string FutureDesc { get; set; }

        private int _pastDescIndex;
        /// <summary>
        /// Past tense description of what the activity is
        /// </summary>
        public string PastDesc { get; set; }

        /// <summary>
        /// Tuple representing up to three brigade members comprising the defined "optimal group" for this activity
        /// </summary>
        public List<BrigadeMember> OptimalGroup { get; set; }
        /// <summary>
        /// Tuple representing up to three brigade members comprising the defined "worst group" for this activity
        /// </summary>
        public List<BrigadeMember> WorstGroup { get; set; }
        /// <summary>
        /// A brigade member who is required to be assigned to this activity
        /// </summary>
        public BrigadeMember RequiredBrigadeMember { get; set; }
        /// <summary>
        /// If true, Haruhi is present for this activity
        /// </summary>
        public bool HaruhiPresent { get; set; }

        /// <summary>
        /// Creates a scenario activity from SCENARIO.S data
        /// </summary>
        /// <param name="dataStartIndex">Start index of data</param>
        /// <param name="lines">Dialogue lines for string references</param>
        /// <param name="data">SCENARIO.S binary data</param>
        public ScenarioActivity(int dataStartIndex, List<DialogueLine> lines, IEnumerable<byte> data)
        {
            TitleIndex = lines.IndexOf(lines.First(l => l.Pointer == IO.ReadInt(data, dataStartIndex)));
            _futureDescIndex = lines.IndexOf(lines.First(l => l.Pointer == IO.ReadInt(data, dataStartIndex + 0x04)));
            _pastDescIndex = lines.IndexOf(lines.First(l => l.Pointer == IO.ReadInt(data, dataStartIndex + 0x08)));
            Title = lines[TitleIndex].Text;
            FutureDesc = lines[_futureDescIndex].Text;
            PastDesc = lines[_pastDescIndex].Text;

            OptimalGroup = [(BrigadeMember)IO.ReadInt(data, dataStartIndex + 0x0C), (BrigadeMember)IO.ReadInt(data, dataStartIndex + 0x10), (BrigadeMember)IO.ReadInt(data, dataStartIndex + 0x14)];
            WorstGroup = [(BrigadeMember)IO.ReadInt(data, dataStartIndex + 0x18), (BrigadeMember)IO.ReadInt(data, dataStartIndex + 0x1C), (BrigadeMember)IO.ReadInt(data, dataStartIndex + 0x20)];
            RequiredBrigadeMember = (BrigadeMember)IO.ReadInt(data, dataStartIndex + 0x24);
            HaruhiPresent = IO.ReadInt(data, dataStartIndex + 0x28) > 0;

            for (int i = 0x2C; IO.ReadInt(data, dataStartIndex + i) > 0; i += 0x10)
            {
                Routes.Add(new(dataStartIndex + i, lines, data));
            }
        }

        internal string GetSource(Dictionary<string, IncludeEntry[]> includes, ref int currentPointer)
        {
            StringBuilder sb = new();

            sb.AppendLine($"ACTIVITY{TitleIndex:D2}:");
            sb.AppendLine($"   POINTER{currentPointer++}: .word ACTIVITYTITLE{TitleIndex:D2}");
            sb.AppendLine($"   POINTER{currentPointer++}: .word ACTIVITYFUTUREDESC{TitleIndex:D2}");
            sb.AppendLine($"   POINTER{currentPointer++}: .word ACTIVITYPASTDESC{TitleIndex:D2}");
            sb.AppendLine($"   .word {(int)OptimalGroup[0]}");
            sb.AppendLine($"   .word {(int)OptimalGroup[1]}");
            sb.AppendLine($"   .word {(int)OptimalGroup[2]}");
            sb.AppendLine($"   .word {(int)WorstGroup[0]}");
            sb.AppendLine($"   .word {(int)WorstGroup[1]}");
            sb.AppendLine($"   .word {(int)WorstGroup[2]}");
            sb.AppendLine($"   .word {(int)RequiredBrigadeMember}");
            sb.AppendLine($"   .word {(HaruhiPresent ? 1 : 0)}");

            foreach (ScenarioRoute route in Routes)
            {
                sb.AppendLine(route.GetSource(includes, ref currentPointer));
            }

            sb.AppendLine(".word -1");
            sb.AppendLine($".skip {0x100 - Routes.Count * 0x10 - 4}");

            sb.AppendLine($"ACTIVITYTITLE{TitleIndex:D2}: .string \"{Title.EscapeShiftJIS()}\"");
            sb.AsmPadString(Title, Encoding.GetEncoding("Shift-JIS"));
            sb.AppendLine($"ACTIVITYFUTUREDESC{TitleIndex:D2}: .string \"{FutureDesc.EscapeShiftJIS()}\"");
            sb.AsmPadString(FutureDesc, Encoding.GetEncoding("Shift-JIS"));
            sb.AppendLine($"ACTIVITYPASTDESC{TitleIndex:D2}: .string \"{PastDesc.EscapeShiftJIS()}\"");
            sb.AsmPadString(PastDesc, Encoding.GetEncoding("Shift-JIS"));

            foreach (ScenarioRoute route in Routes)
            {
                sb.AppendLine($"ROUTETITLE{route.RouteTitleIndex:D2}: .string \"{route.Title.EscapeShiftJIS()}\"");
                sb.AsmPadString(route.Title, Encoding.GetEncoding("Shift-JIS"));
            }

            return sb.ToString();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return Title;
        }

        /// <summary>
        /// Enum representing the possible brigade member assignments
        /// </summary>
        public enum BrigadeMember
        {
            /// <summary>
            /// Any brigade member can be chosen
            /// </summary>
            ANY = -1,
            /// <summary>
            /// Mikuru Asahina
            /// </summary>
            MIKURU = 3,
            /// <summary>
            /// Yuki Nagato
            /// </summary>
            NAGATO = 4,
            /// <summary>
            /// Itsuki Koizumi
            /// </summary>
            KOIZUMI = 5,
            /// <summary>
            /// No brigade member may be chosen
            /// </summary>
            NONE = 22,
        }
    }

    /// <summary>
    /// Defines a particular route (an allotment of characters to a particular activity) in a group selection in SCENARIO.S
    /// </summary>
    public class ScenarioRoute
    {
        /// <summary>
        /// The index of the event file that is loaded if this route is selected
        /// </summary>
        public short ScriptIndex { get; set; }
        /// <summary>
        /// The flag that is set if this route is selected (i.e. the route ID)
        /// </summary>
        public short Flag { get; set; }
        /// <summary>
        /// If all the characters required for this route except Kyon are assigned to this route's activity,
        /// the topics contained in this list will be given to the player
        /// </summary>
        public List<short> KyonlessTopics { get; set; } = [];

        /// <summary>
        /// The dialogue line index of Title
        /// </summary>
        internal int RouteTitleIndex { get; set; }
        /// <summary>
        /// The title of the route
        /// </summary>
        public string Title { get; set; }
        
        /// <summary>
        /// The characters who must be assigned to this route's activity to trigger this route
        /// </summary>
        public List<Speaker> CharactersInvolved { get; set; } = [];

        /// <summary>
        /// Creates a scenario route given data from SCENARIO.S
        /// </summary>
        /// <param name="dataStartIndex">The start index of the route within the data</param>
        /// <param name="lines">Dialogue lines for string reference</param>
        /// <param name="data">SCENARIO.S binary data</param>
        public ScenarioRoute(int dataStartIndex, List<DialogueLine> lines, IEnumerable<byte> data)
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
            Flag = IO.ReadShort(data, dataStartIndex + 6);

            int pointerToShortArray = IO.ReadInt(data, dataStartIndex + 8);
            int currentShortOffset = 0;
            do
            {
                KyonlessTopics.Add(IO.ReadShort(data, pointerToShortArray + currentShortOffset));
                currentShortOffset += 2;
            } while (KyonlessTopics.Last() > 0);

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

        internal string GetSource(Dictionary<string, IncludeEntry[]> includes, ref int currentPointer)
        {
            StringBuilder sb = new();

            sb.AppendLine($".word {GetCharactersInvolvedFlag()}");
            sb.AppendLine($".short {includes["EVTBIN"].First(i => i.Value == ScriptIndex).Name}");
            sb.AppendLine($".short {Flag}");
            sb.AppendLine($"POINTER{currentPointer++}: .word KYONLESS_TOPICS{RouteTitleIndex:D2}");
            sb.AppendLine($"POINTER{currentPointer++}: .word ROUTETITLE{RouteTitleIndex:D2}");

            return sb.ToString();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return Title;
        }
    }

    /// <summary>
    /// A set of mask bytes that are used for determining characters involved
    /// </summary>
    [Flags]
    public enum CharacterMask : byte
    {
        /// <summary>
        /// Kyon
        /// </summary>
        KYON = 0b0000_0010,
        /// <summary>
        /// Haruhi Suzumiya
        /// </summary>
        HARUHI = 0b0000_0100,
        /// <summary>
        /// Mikuru Asahina
        /// </summary>
        MIKURU = 0b0000_1000,
        /// <summary>
        /// Yuki Nagato
        /// </summary>
        NAGATO = 0b0001_0000,
        /// <summary>
        /// Itsuki Koizumi
        /// </summary>
        KOIZUMI = 0b0010_0000,
    }
}
