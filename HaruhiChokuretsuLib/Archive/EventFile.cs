using HaruhiChokuretsuLib.Font;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Resources.NetStandard;
using System.Text;

namespace HaruhiChokuretsuLib.Archive
{
    public class EventFile : FileInArchive, ISourceFile
    {
        public int NumSections { get; set; }

        public List<EventFileSection> SectionPointersAndCounts { get; set; } = new();
        public int PointerToEndPointerSection { get; set; }
        public List<int> EndPointers { get; set; } = new();
        public List<int> EndPointerPointers { get; set; } = new();
        public string Title { get; set; }

        public EventFileSettings Settings { get; set; }
        public Dictionary<int, string> DramatisPersonae { get; set; } = new();
        public List<DialogueLine> DialogueLines { get; set; } = new();

        public List<TopicStruct> TopicStructs { get; set; } = new();
        public ScenarioStruct Scenario { get; set; }

        public EventFile()
        {
        }

        public FontReplacementDictionary FontReplacementMap { get; set; } = new();

        private const int DIALOGUE_LINE_LENGTH = 230;

        public override void Initialize(byte[] decompressedData, int offset = 0)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Offset = offset;
            Data = decompressedData.ToList();

            NumSections = BitConverter.ToInt32(decompressedData.Take(4).ToArray());
            for (int i = 0; i < NumSections; i++)
            {
                SectionPointersAndCounts.Add(new()
                {
                    Pointer = BitConverter.ToInt32(decompressedData.Skip(0x0C + (0x08 * i)).Take(4).ToArray()),
                    ItemCount = BitConverter.ToInt32(decompressedData.Skip(0x10 + (0x08 * i)).Take(4).ToArray()),
                });
            }

            Settings = new(decompressedData.Skip(SectionPointersAndCounts[0].Pointer).Take(EventFileSettings.SETTINGS_LENGTH));

            int dialogueSectionPointerIndex = SectionPointersAndCounts.FindIndex(f => f.Pointer == Settings.DialogueSectionPointer);
            for (int i = SectionPointersAndCounts.FindIndex(f => f.Pointer == Settings.FlagsSectionPointer) + 1; i < dialogueSectionPointerIndex; i++)
            {
                DramatisPersonae.Add(SectionPointersAndCounts[i].Pointer,
                    Encoding.GetEncoding("Shift-JIS").GetString(decompressedData.Skip(SectionPointersAndCounts[i].Pointer).TakeWhile(b => b != 0x00).ToArray()));
            }

            InitializeDialogueAndEndPointers(decompressedData, offset);
        }

        protected void InitializeDialogueAndEndPointers(byte[] decompressedData, int offset, bool @override = false)
        {
            if ((Name != "CHESSS" && Name != "EVTTBLS" && Name != "TOPICS" && Name != "SCENARIOS" && Name != "VOICEMAPS"
                && Name != "MESSS"
                && Settings.DialogueSectionPointer < decompressedData.Length) || @override)
            {
                for (int i = 0; i < Settings.NumDialogueEntries; i++)
                {
                    int character = BitConverter.ToInt32(decompressedData.Skip(Settings.DialogueSectionPointer + i * 0x0C).Take(4).ToArray());
                    int speakerPointer = BitConverter.ToInt32(decompressedData.Skip(Settings.DialogueSectionPointer + i * 0x0C + 4).Take(4).ToArray());
                    int dialoguePointer = BitConverter.ToInt32(decompressedData.Skip(Settings.DialogueSectionPointer + i * 0x0C + 8).Take(4).ToArray());

                    if (character == 0 && speakerPointer == 0 && dialoguePointer == 0)
                    {
                        break;
                    }

                    DramatisPersonae.TryGetValue(speakerPointer, out string speakerName);
                    DialogueLines.Add(new DialogueLine((Speaker)character, speakerName, speakerPointer, dialoguePointer, Data.ToArray()));
                }
            }

            PointerToEndPointerSection = BitConverter.ToInt32(decompressedData.Skip(4).Take(4).ToArray());
            int numEndPointers = BitConverter.ToInt32(decompressedData.Skip(PointerToEndPointerSection).Take(4).ToArray());
            for (int i = 0; i < numEndPointers; i++)
            {
                EndPointers.Add(BitConverter.ToInt32(decompressedData.Skip(PointerToEndPointerSection + (0x04 * (i + 1))).Take(4).ToArray()));
            }

            EndPointerPointers = EndPointers.Select(p => { int x = offset; return BitConverter.ToInt32(decompressedData.Skip(p).Take(4).ToArray()); }).ToList();

            int titlePointer = BitConverter.ToInt32(decompressedData.Skip(0x08).Take(4).ToArray());
            Title = Encoding.ASCII.GetString(decompressedData.Skip(titlePointer).TakeWhile(b => b != 0x00).ToArray());

            for (int i = 0; EndPointerPointers.Count > 0 && DramatisPersonae.Count > 0 && EndPointerPointers[i] < DramatisPersonae.Keys.First(); i++)
            {
                if (decompressedData[EndPointerPointers[i]] >= 0x81 && decompressedData[EndPointerPointers[i]] <= 0x9F)
                {
                    DialogueLines.Add(new(Speaker.INFO, "CHOICE", 0, EndPointerPointers[i], decompressedData));
                }
            }
        }

        public void IdentifyEventFileTopics(IList<TopicStruct> availableTopics)
        {
            int topicsSectionPointer = SectionPointersAndCounts.Where(s => s.Pointer > DialogueLines.Last(d => d.SpeakerName != "CHOICE").Pointer).Select(s => s.Pointer).ToArray()[3]; // third pointer after dialogue section
            for (int i = topicsSectionPointer; i < PointerToEndPointerSection; i += 0x24)
            {
                int controlSwitch = BitConverter.ToInt32(Data.Skip(i).Take(4).ToArray());
                if (controlSwitch == 0x0E)
                {
                    int topicId = BitConverter.ToInt32(Data.Skip(i + 4).Take(4).ToArray());
                    TopicStruct topic = availableTopics.FirstOrDefault(t => t.Id == topicId);
                    if (topic is not null)
                    {
                        TopicStructs.Add(topic);
                    }
                }
            }
        }

        public void InitializeDialogueForSpecialFiles()
        {
            DialogueLines.Clear();
            for (int i = 0; i < EndPointerPointers.Count; i++)
            {
                DialogueLines.Add(new DialogueLine(Speaker.INFO, "INFO", 0, EndPointerPointers[i], Data.ToArray()));
            }
        }

        public void InitializeTopicFile()
        {
            InitializeDialogueForSpecialFiles();
            for (int i = 0; i < DialogueLines.Count; i += 2)
            {
                TopicStructs.Add(new(i, DialogueLines[i].Text, Data.Skip(0x18 + i / 2 * 0x24).Take(0x24).ToArray()));
            }
        }

        public void InitializeScenarioFile()
        {
            InitializeDialogueForSpecialFiles();
            Scenario = new(Data, SectionPointersAndCounts[^2].Pointer, SectionPointersAndCounts[^3].Pointer);
        }

        public override byte[] GetBytes() => Data.ToArray();

        public virtual void EditDialogueLine(int index, string newText)
        {
            Edited = true;
            int oldLength = DialogueLines[index].Length + DialogueLines[index].NumPaddingZeroes;
            DialogueLines[index].Text = newText;
            DialogueLines[index].NumPaddingZeroes = 4 - (DialogueLines[index].Length % 4);
            int lengthDifference = DialogueLines[index].Length + DialogueLines[index].NumPaddingZeroes - oldLength;

            List<byte> toWrite = new();
            toWrite.AddRange(DialogueLines[index].Data);
            for (int i = 0; i < DialogueLines[index].NumPaddingZeroes; i++)
            {
                toWrite.Add(0);
            }

            Data.RemoveRange(DialogueLines[index].Pointer, oldLength);
            Data.InsertRange(DialogueLines[index].Pointer, toWrite);

            ShiftPointers(DialogueLines[index].Pointer, lengthDifference);
        }

        public virtual void ShiftPointers(int shiftLocation, int shiftAmount)
        {
            for (int i = 0; i < SectionPointersAndCounts.Count; i++)
            {
                if (SectionPointersAndCounts[i].Pointer > shiftLocation)
                {
                    SectionPointersAndCounts[i].Pointer += shiftAmount;
                    Data.RemoveRange(0x0C + (0x08 * i), 4);
                    Data.InsertRange(0x0C + (0x08 * i), BitConverter.GetBytes(SectionPointersAndCounts[i].Pointer));
                }
            }
            if (PointerToEndPointerSection > shiftLocation)
            {
                PointerToEndPointerSection += shiftAmount;
                Data.RemoveRange(0x04, 4);
                Data.InsertRange(0x04, BitConverter.GetBytes(PointerToEndPointerSection));
            }
            for (int i = 0; i < EndPointers.Count; i++)
            {
                if (EndPointers[i] > shiftLocation)
                {
                    EndPointers[i] += shiftAmount;
                    Data.RemoveRange(PointerToEndPointerSection + 0x04 * (i + 1), 4);
                    Data.InsertRange(PointerToEndPointerSection + 0x04 * (i + 1), BitConverter.GetBytes(EndPointers[i]));
                }
            }
            for (int i = 0; i < EndPointerPointers.Count; i++)
            {
                if (EndPointerPointers[i] > shiftLocation)
                {
                    EndPointerPointers[i] += shiftAmount;
                    Data.RemoveRange(EndPointers[i], 4);
                    Data.InsertRange(EndPointers[i], BitConverter.GetBytes(EndPointerPointers[i]));
                }
            }
            foreach (DialogueLine dialogueLine in DialogueLines)
            {
                if (dialogueLine.Pointer > shiftLocation)
                {
                    dialogueLine.Pointer += shiftAmount;
                }
            }
        }

        public void WriteResxFile(string fileName)
        {
            using ResXResourceWriter resxWriter = new(fileName);
            for (int i = 0; i < DialogueLines.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(DialogueLines[i].Text) && DialogueLines[i].Length > 1)
                {
                    resxWriter.AddResource(new ResXDataNode($"{i:D4} ({Path.GetFileNameWithoutExtension(fileName)}) {DialogueLines[i].Speaker} ({DialogueLines[i].SpeakerName})",
                        DialogueLines[i].Text));
                }
            }
        }

        public void ImportResxFile(string fileName)
        {
            Edited = true;
            string resxContents = File.ReadAllText(fileName);
            resxContents = resxContents.Replace("System.Resources.ResXResourceWriter, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
                "System.Resources.NetStandard.ResXResourceWriter, System.Resources.NetStandard, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");
            resxContents = resxContents.Replace("System.Resources.ResXResourceReader, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
                "System.Resources.NetStandard.ResXResourceReader, System.Resources.NetStandard, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");
            TextReader textReader = new StringReader(resxContents);

            using ResXResourceReader resxReader = new(textReader);
            foreach (DictionaryEntry d in resxReader)
            {
                int dialogueIndex = int.Parse(((string)d.Key)[0..4]);
                bool datFile = ((string)d.Key).Contains("dat_");
                string dialogueText = (string)d.Value;

                // Replace all faux-ellipses with an ellipsis character
                dialogueText = dialogueText.Replace("...", "…");
                // Replace all faux-em-dashes with actual em-dash characters
                dialogueText = dialogueText.Replace("--", "—");
                // Consolidate Unix/Windows newlines to just \n
                dialogueText = dialogueText.Replace("\r\n", "\n");

                int lineLength = 0;
                bool operatorActive = false;
                for (int i = 0; i < dialogueText.Length; i++)
                {
                    if (operatorActive)
                    {
                        if (dialogueText[i] >= '0' && dialogueText[i] <= '9')
                        {
                            continue;
                        }
                        else
                        {
                            operatorActive = false;
                        }
                    }

                    if (dialogueText[i] == '$')
                    {
                        operatorActive = true;
                        continue;
                    }
                    else if (dialogueText[i] == '#')
                    {
                        if (dialogueText[(i + 1)..(i + 3)] == "DP" || dialogueText[(i + 1)..(i + 3)] == "SE" || dialogueText[(i + 1)..(i + 3)] == "SK" || dialogueText[(i + 1)..(i + 3)] == "sk")
                        {
                            i += 2;
                        }
                        else if (dialogueText[(i + 1)..(i + 4)] == "SSE")
                        {
                            i += 3;
                        }
                        else
                        {
                            i++; // skip initial control character
                        }
                        operatorActive = true;
                        continue;
                    }

                    if (FontReplacementMap.ContainsKey(dialogueText[i]))
                    {
                        char newCharacter = FontReplacementMap[dialogueText[i]].OriginalCharacter;
                        if (dialogueText[i] == '"' && (i == dialogueText.Length - 1
                            || dialogueText[i + 1] == ' ' || dialogueText[i + 1] == '!' || dialogueText[i + 1] == '?' || dialogueText[i + 1] == '.' || dialogueText[i + 1] == '…' || dialogueText[i + 1] == '\n' || dialogueText[i + 1] == '#'))
                        {
                            newCharacter = '”';
                        }
                        lineLength += FontReplacementMap[dialogueText[i]].Offset;
                        dialogueText = dialogueText.Remove(i, 1);
                        dialogueText = dialogueText.Insert(i, $"{newCharacter}");
                    }

                    if (dialogueText[i] == '\n')
                    {
                        lineLength = 0;
                    }

                    if (!datFile && dialogueText[i] != '　' && lineLength > DIALOGUE_LINE_LENGTH)
                    {
                        int indexOfMostRecentSpace = dialogueText[0..i].LastIndexOf('　'); // full-width space bc it's been replaced already
                        dialogueText = dialogueText.Remove(indexOfMostRecentSpace, 1);
                        dialogueText = dialogueText.Insert(indexOfMostRecentSpace, "\n");
                        lineLength = 0;
                    }
                }

                if ((!datFile && dialogueText.Count(c => c == '\n') > 1) || (DialogueLines[dialogueIndex].SpeakerName == "CHOICE" && dialogueText.Length > 256))
                {
                    string type = "dialogue line";
                    if (DialogueLines[dialogueIndex].SpeakerName == "CHOICE")
                    {
                        type = "choice";
                    }
                    Console.WriteLine($"File {Index} has {type} too long ({dialogueIndex}) (starting with: {dialogueText[0..Math.Min(15, dialogueText.Length - 1)]})");
                }

                EditDialogueLine(dialogueIndex, dialogueText);
            }
        }

        public override string ToString()
        {
            return $"{Index:X3} {Index:D3} 0x{Offset:X8} '{Name}'";
        }

        public string GetSource(Dictionary<string, IncludeEntry[]> includes)
        {
            if (Name == "CHESSS")
            {
                return "";
            }
            else if (Name == "EVTTBLS")
            {
                return "";
            }
            else if (Name == "SCENARIOS")
            {
                return "";
            }
            else if (Name == "TOPICS")
            {
                return "";
            }
            else
            {
                StringBuilder builder = new();
                builder.AppendLine(".include DATBIN.INC");
                builder.AppendLine(".include EVTBIN.INC");
                builder.AppendLine(".include GRPBIN.INC");
                builder.AppendLine(".include BGTBL.S");
                builder.AppendLine();
                builder.AppendLine($".word {NumSections}");
                builder.AppendLine(".word END_POINTERS");
                builder.AppendLine(".word FILE_START");

                builder.AppendLine(".word SETTINGS");
                builder.AppendLine(".word 1");
                if (Settings.UnknownSection01Pointer > 0)
                {
                    builder.AppendLine($".word 0");
                }

                return builder.ToString();
            }
        }
    }
    public class EventFileSection
    {
        public int Pointer { get; set; }
        public int ItemCount { get; set; }

        public override string ToString()
        {
            return $"0x{Pointer:X8}, {ItemCount}";
        }
    }

    public class EventFileSettings
    {
        public const int SETTINGS_LENGTH = 0x128;

        public int EventNamePointer { get; set; }
        public int NumUnknown01 { get; set; }
        public int UnknownSection01Pointer { get; set; } // probably straight up unused
        public int NumUnknown02 { get; set; }
        public int UnknownSection02Pointer { get; set; } // potentially something to do with flag setting after you've investigated something
        public int NumUnknown03 { get; set; }
        public int UnknownSection03Pointer { get; set; } // probably straight up unused
        public int NumUnknown04 { get; set; }
        public int UnknownSection04Pointer { get; set; } // array indices of some kind
        public int NumUnknown05 { get; set; }
        public int UnknownSection05Pointer { get; set; } // flag setting (investigation-related)
        public int NumUnknown06 { get; set; }
        public int UnknownSection06Pointer { get; set; } // probably straigt up unused
        public int NumUnknown07 { get; set; }
        public int UnknownSection07Pointer { get; set; } // more flags stuff (investigation-related)
        public int NumChoices { get; set; }
        public int ChoicesSectionPointer { get; set; }
        public int Unused44 { get; set; }
        public int Unused48 { get; set; }
        public int NumUnknown08 { get; set; }
        public int UnknownSection08Pointer { get; set; } // maybe unused
        public int NumUnknown09 { get; set; }
        public int UnknownSection09Pointer { get; set; } // seems unused
        public int NumFlags { get; set; }
        public int FlagsSectionPointer { get; set; }
        public int NumDialogueEntries { get; set; }
        public int DialogueSectionPointer { get; set; }
        public int NumUnknown10 { get; set; }
        public int UnknownSection10Pointer { get; set; }
        public int NumCodeSections { get; set; }
        public int CodeSectionsSectionPointer { get; set; }

        public EventFileSettings(IEnumerable<byte> data)
        {
            if (data.Count() < 0x128)
            {
                return;
            }
            EventNamePointer = BitConverter.ToInt32(data.Take(4).ToArray());
            NumUnknown01 = BitConverter.ToInt32(data.Skip(0x004).Take(4).ToArray());
            UnknownSection01Pointer = BitConverter.ToInt32(data.Skip(0x008).Take(4).ToArray());
            NumUnknown02 = BitConverter.ToInt32(data.Skip(0x00C).Take(4).ToArray());
            UnknownSection02Pointer = BitConverter.ToInt32(data.Skip(0x010).Take(4).ToArray());
            NumUnknown03 = BitConverter.ToInt32(data.Skip(0x014).Take(4).ToArray());
            UnknownSection03Pointer = BitConverter.ToInt32(data.Skip(0x018).Take(4).ToArray());
            NumUnknown04 = BitConverter.ToInt32(data.Skip(0x01C).Take(4).ToArray());
            UnknownSection04Pointer = BitConverter.ToInt32(data.Skip(0x020).Take(4).ToArray());
            NumUnknown05 = BitConverter.ToInt32(data.Skip(0x024).Take(4).ToArray());
            UnknownSection05Pointer = BitConverter.ToInt32(data.Skip(0x028).Take(4).ToArray());
            NumUnknown06 = BitConverter.ToInt32(data.Skip(0x02C).Take(4).ToArray());
            UnknownSection06Pointer = BitConverter.ToInt32(data.Skip(0x030).Take(4).ToArray());
            NumUnknown07 = BitConverter.ToInt32(data.Skip(0x034).Take(4).ToArray());
            UnknownSection07Pointer = BitConverter.ToInt32(data.Skip(0x038).Take(4).ToArray());
            NumChoices = BitConverter.ToInt32(data.Skip(0x03C).Take(4).ToArray());
            ChoicesSectionPointer = BitConverter.ToInt32(data.Skip(0x040).Take(4).ToArray());
            Unused44 = BitConverter.ToInt32(data.Skip(0x044).Take(4).ToArray());
            Unused48 = BitConverter.ToInt32(data.Skip(0x048).Take(4).ToArray());
            NumUnknown08 = BitConverter.ToInt32(data.Skip(0x04C).Take(4).ToArray());
            UnknownSection08Pointer = BitConverter.ToInt32(data.Skip(0x050).Take(4).ToArray());
            NumUnknown09 = BitConverter.ToInt32(data.Skip(0x054).Take(4).ToArray());
            UnknownSection09Pointer = BitConverter.ToInt32(data.Skip(0x058).Take(4).ToArray());
            NumFlags = BitConverter.ToInt32(data.Skip(0x05C).Take(4).ToArray());
            FlagsSectionPointer = BitConverter.ToInt32(data.Skip(0x060).Take(4).ToArray());
            NumDialogueEntries = BitConverter.ToInt32(data.Skip(0x064).Take(4).ToArray());
            DialogueSectionPointer = BitConverter.ToInt32(data.Skip(0x068).Take(4).ToArray());
            NumUnknown10 = BitConverter.ToInt32(data.Skip(0x06C).Take(4).ToArray());
            UnknownSection10Pointer = BitConverter.ToInt32(data.Skip(0x070).Take(4).ToArray());
            NumCodeSections = BitConverter.ToInt32(data.Skip(0x074).Take(4).ToArray());
            CodeSectionsSectionPointer = BitConverter.ToInt32(data.Skip(0x078).Take(4).ToArray());
        }
    }

    public class DialogueLine
    {
        public int Pointer { get; set; }
        public byte[] Data { get; set; }
        public int NumPaddingZeroes { get; set; }
        public string Text { get => Encoding.GetEncoding("Shift-JIS").GetString(Data); set => Data = Encoding.GetEncoding("Shift-JIS").GetBytes(value); }
        public int Length => Data.Length;

        public int SpeakerPointer { get; set; }
        public Speaker Speaker { get; set; }
        public string SpeakerName { get; set; }

        public DialogueLine(Speaker speaker, string speakerName, int speakerPointer, int pointer, byte[] file)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Speaker = speaker;
            SpeakerName = speakerName;
            SpeakerPointer = speakerPointer;
            Pointer = pointer;
            Data = file.Skip(pointer).TakeWhile(b => b != 0x00).ToArray();
            NumPaddingZeroes = 4 - (Length % 4);
        }

        public override string ToString()
        {
            return Text;
        }
    }

    public class TopicStruct
    {
        public int TopicDialogueIndex { get; set; }
        public string Title { get; set; }

        public short Id { get; set; }
        public short EventIndex { get; set; }
        public short[] UnknownShorts = new short[16];

        public TopicStruct(int dialogueIndex, string dialogueLine, byte[] data)
        {
            if (data.Length != 0x24)
            {
                throw new ArgumentException($"Topic struct data length must be 0x24, was 0x{data.Length:X2}");
            }

            TopicDialogueIndex = dialogueIndex;
            Title = dialogueLine;
            Id = BitConverter.ToInt16(data.Take(2).ToArray());
            EventIndex = BitConverter.ToInt16(data.Skip(2).Take(2).ToArray());
            for (int i = 0; i < UnknownShorts.Length; i++)
            {
                UnknownShorts[i] = BitConverter.ToInt16(data.Skip((i + 2) * 2).Take(2).ToArray());
            }
        }

        public override string ToString()
        {
            return $"0x{Id:X4} '{Title}'";
        }

        public string ToCsvLine()
        {
            return $"{TopicDialogueIndex},{Title},{Id:X4},{EventIndex}";
        }
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

    public class VoiceMapFile : EventFile
    {
        public int VoiceMapStructSectionOffset { get; set; }
        public int DialogueLinesPointer { get; set; }
        public List<VoiceMapStruct> VoiceMapStructs { get; set; } = new();

        public override void Initialize(byte[] decompressedData, int offset = 0)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Offset = offset;
            Data = decompressedData.ToList();

            int numFrontPointers = BitConverter.ToInt32(decompressedData.Take(4).ToArray());
            for (int i = 0; i < numFrontPointers; i++)
            {
                SectionPointersAndCounts.Add(new()
                {
                    Pointer = BitConverter.ToInt32(decompressedData.Skip(0x0C + (0x08 * i)).Take(4).ToArray()),
                    ItemCount = BitConverter.ToInt32(decompressedData.Skip(0x10 + (0x08 * i)).Take(4).ToArray()),
                });
            }
            Settings = new(new byte[0x128]);
            VoiceMapStructSectionOffset = SectionPointersAndCounts[0].Pointer;
            Settings.DialogueSectionPointer = SectionPointersAndCounts[1].Pointer;
            DialogueLinesPointer = Settings.DialogueSectionPointer + (SectionPointersAndCounts.Count - 1) * 12;
            Settings.NumDialogueEntries = (numFrontPointers - 2);

            for (int i = 2; i < SectionPointersAndCounts.Count; i++)
            {
                DramatisPersonae.Add(SectionPointersAndCounts[i].Pointer, Encoding.ASCII.GetString(Data.Skip(SectionPointersAndCounts[i].Pointer).TakeWhile(b => b != 0).ToArray()));
            }

            for (int i = 0; i < SectionPointersAndCounts.Count - 2; i++)
            {
                VoiceMapStructs.Add(new(Data.Skip(VoiceMapStructSectionOffset + i * VoiceMapStruct.VOICE_MAP_STRUCT_LENGTH).Take(VoiceMapStruct.VOICE_MAP_STRUCT_LENGTH)));
            }

            InitializeDialogueAndEndPointers(decompressedData, offset, @override: true);
        }

        public override void NewFile(string filename)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Data = new();
            string[] csvData = File.ReadAllLines(filename);
            Name = "VOICEMAPS";

            List<int> filenamePointers = new();
            List<byte> filenameSection = new();
            List<string> filenames = new();
            List<int> dialogueLinePointers = new();
            List<byte> dialogueLinesSection = new();
            foreach (string line in csvData)
            {
                string[] fields = line.Split(',');

                filenamePointers.Add(filenameSection.Count);
                dialogueLinePointers.Add(dialogueLinesSection.Count);

                filenames.Add(fields[0]);
                filenameSection.AddRange(Encoding.ASCII.GetBytes(fields[0]));
                dialogueLinesSection.AddRange(Encoding.GetEncoding("Shift-JIS").GetBytes(fields[1]));

                filenameSection.Add(0);
                dialogueLinesSection.Add(0);
                while (filenameSection.Count % 4 != 0)
                {
                    filenameSection.Add(0);
                }
                while (dialogueLinesSection.Count % 4 != 0)
                {
                    dialogueLinesSection.Add(0);
                }
            }

            int numFrontPointers = csvData.Length + 2;
            Data.AddRange(BitConverter.GetBytes(numFrontPointers));
            Data.AddRange(BitConverter.GetBytes(-1)); // Add placeholder for end pointers pointer
            Data.AddRange(BitConverter.GetBytes(1));
            Data.AddRange(BitConverter.GetBytes(-1)); // Add placeholder for struct section
            Data.AddRange(BitConverter.GetBytes(1));
            Data.AddRange(BitConverter.GetBytes(-1)); // Add placeholder for dialogue section
            Data.AddRange(BitConverter.GetBytes(1));

            int filenameSectionStart = Data.Count + filenamePointers.Count * 8;
            // add rest of front pointers (dramatis personae/filename pointers)
            Data.AddRange(filenamePointers.SelectMany(p =>
            { 
                List<byte> s = new(BitConverter.GetBytes(p + filenameSectionStart));
                s.AddRange(new byte[] { 1, 0, 0, 0 });
                return s;
            }));
            // And then add them to dramatis personae
            for (int i = 0; i < filenamePointers.Count; i++)
            {
                DramatisPersonae.Add(filenamePointers[i] + filenameSectionStart, filenames[i]);
            }

            Data.AddRange(filenameSection);

            // Go back and insert the pointer to the dialogue section
            int dialogueSectionStart = Data.Count;
            Data.RemoveRange(0x14, 4);
            Data.InsertRange(0x14, BitConverter.GetBytes(dialogueSectionStart));

            // Account for the dialogue section leadin
            DialogueLinesPointer = Data.Count + dialogueLinePointers.Count * 12 + 12;

            for (int i = 0; i < dialogueLinePointers.Count; i++)
            {
                Data.AddRange(BitConverter.GetBytes((int)SpeakerCodeMap[filenames[i].Split('_')[0]]));
                EndPointers.Add(Data.Count); // add this next pointer to the end pointers so it gets resolved
                EndPointerPointers.Add(filenamePointers[i] + filenameSectionStart);
                Data.AddRange(BitConverter.GetBytes(filenamePointers[i] + filenameSectionStart));
                EndPointers.Add(Data.Count); // add this next pointer to the end pointers so it gets resolved
                EndPointerPointers.Add(dialogueLinePointers[i] + DialogueLinesPointer);
                Data.AddRange(BitConverter.GetBytes(dialogueLinePointers[i] + DialogueLinesPointer));
            }
            Data.AddRange(new byte[12]);
            Data.AddRange(dialogueLinesSection);
            Data.AddRange(new byte[4]);

            // Initialize the dialogue lines
            for (int i = 0; i < dialogueLinePointers.Count; i++)
            {
                DialogueLines.Add(new(SpeakerCodeMap[filenames[i].Split('_')[0]], filenames[i], filenamePointers[i] + filenameSectionStart, dialogueLinePointers[i] + DialogueLinesPointer, Data.ToArray()));
            }

            // Go back and insert the pointer to the struct section
            VoiceMapStructSectionOffset = Data.Count;
            Data.RemoveRange(0x0C, 4);
            Data.InsertRange(0x0C, BitConverter.GetBytes(VoiceMapStructSectionOffset));

            for (int i = 0; i < csvData.Length; i++)
            {
                string[] fields = csvData[i].Split(',');
                short y = 0;
                int lineLength = DialogueLines[i].Text.Sum(c => FontReplacementMap.ReverseLookup(c)?.Offset ?? 15);

                switch (fields[2])
                {
                    default:
                    case "TOP":
                        y = 16;
                        break;
                    case "BELOW_TOP":
                        y = 40;
                        break;
                    case "ABOVE_BOTTOM":
                        y = 160;
                        break;
                    case "BOTTOM":
                        y = 176;
                        break;
                }

                EndPointers.AddRange(new int[] { Data.Count, Data.Count + 4 }); // Add the next two pointers to end pointers
                EndPointerPointers.AddRange(new int[] { filenamePointers[i] + filenameSectionStart, dialogueLinePointers[i] + DialogueLinesPointer });
                VoiceMapStruct vmStruct = new()
                {
                    VoiceFileNamePointer = filenamePointers[i] + filenameSectionStart,
                    SubtitlePointer = dialogueLinePointers[i] + DialogueLinesPointer,
                    X = CenterSubtitle(lineLength),
                    Y = y,
                    FontSize = 100,
                    TargetScreen = (VoiceMapStruct.Screen)Enum.Parse(typeof(VoiceMapStruct.Screen), fields[3]),
                    Timer = ushort.Parse(fields[4]),
                };

                VoiceMapStructs.Add(vmStruct);
                Data.AddRange(vmStruct.GetBytes());
            }
            Data.AddRange(new byte[VoiceMapStruct.VOICE_MAP_STRUCT_LENGTH]);

            // Go back and insert the pointer to the end pointers section
            int endPointersSectionStart = Data.Count;
            Data.RemoveRange(0x04, 4);
            Data.InsertRange(0x04, BitConverter.GetBytes(endPointersSectionStart));

            Data.AddRange(BitConverter.GetBytes(EndPointers.Count));
            Data.AddRange(EndPointers.SelectMany(e => BitConverter.GetBytes(e)));
        }

        public override void EditDialogueLine(int index, string newText)
        {
            newText = $"#P07{newText}";

            base.EditDialogueLine(index, newText);

            if (DialogueLines[index].Text.Contains('\n'))
            {
                Console.WriteLine($"File {Index} has subtitle too long ({index}) (starting with: {DialogueLines[index].Text[4..20]})");
            }

            string actualText = newText[4..];
            int lineLength = actualText.Sum(c => FontReplacementMap.ReverseLookup(c)?.Offset ?? 15);
            VoiceMapStructs[index].X = CenterSubtitle(lineLength);
            Data.RemoveRange(VoiceMapStructSectionOffset + VoiceMapStruct.VOICE_MAP_STRUCT_LENGTH * index + 8, 2); // Replace X in Data
            Data.InsertRange(VoiceMapStructSectionOffset + VoiceMapStruct.VOICE_MAP_STRUCT_LENGTH * index + 8, BitConverter.GetBytes(VoiceMapStructs[index].X));
        }

        public override void ShiftPointers(int shiftLocation, int shiftAmount)
        {
            base.ShiftPointers(shiftLocation, shiftAmount);

            if (VoiceMapStructSectionOffset > shiftLocation)
            {
                VoiceMapStructSectionOffset += shiftAmount;
            }
            if (DialogueLinesPointer > shiftLocation)
            {
                DialogueLinesPointer += shiftAmount;
            }

            foreach (VoiceMapStruct vmStruct in VoiceMapStructs)
            {
                if (vmStruct.VoiceFileNamePointer > shiftLocation)
                {
                    vmStruct.VoiceFileNamePointer += shiftAmount;
                }
                if (vmStruct.SubtitlePointer > shiftLocation)
                {
                    vmStruct.SubtitlePointer += shiftAmount;
                }
            }
        }

        private static short CenterSubtitle(int lineLength)
        {
            return (short)((256 - lineLength) / 2);
        }

        private static Dictionary<string, Speaker> SpeakerCodeMap = new()
        {
            { "ANZ", Speaker.GIRL },
            { "HRH", Speaker.HARUHI },
            { "KYN", Speaker.KYON },
            { "KZM", Speaker.KOIZUMI },
            { "MKR", Speaker.MIKURU },
            { "NGT", Speaker.NAGATO },
        };

        public class VoiceMapStruct
        {
            public const int VOICE_MAP_STRUCT_LENGTH = 20;

            public enum Screen
            {
                BOTTOM = 0,
                TOP = 1,
            }

            public List<byte> Data { get; set; } = new();
            public int VoiceFileNamePointer { get; set; }
            public int SubtitlePointer { get; set; }
            public short X { get; set; }
            public short Y { get; set; }
            public short FontSize { get; set; }
            public Screen TargetScreen { get; set; }
            public int Timer { get; set; }

            public VoiceMapStruct()
            {
            }

            public VoiceMapStruct(IEnumerable<byte> data)
            {
                if (data.Count() != VOICE_MAP_STRUCT_LENGTH)
                {
                    throw new ArgumentException($"Voice map struct data length must be 0x{VOICE_MAP_STRUCT_LENGTH:X2}, was 0x{data.Count():X2}");
                }

                VoiceFileNamePointer = BitConverter.ToInt32(data.Take(4).ToArray());
                SubtitlePointer = BitConverter.ToInt32(data.Skip(4).Take(4).ToArray());
                X = BitConverter.ToInt16(data.Skip(8).Take(2).ToArray());
                Y = BitConverter.ToInt16(data.Skip(10).Take(2).ToArray());
                FontSize = BitConverter.ToInt16(data.Skip(12).Take(2).ToArray());
                TargetScreen = (Screen)BitConverter.ToInt16(data.Skip(14).Take(2).ToArray());
                Timer = BitConverter.ToInt32(data.Skip(16).Take(4).ToArray());

                Data = data.ToList();
            }

            public byte[] GetBytes()
            {
                Data.Clear();

                Data.AddRange(BitConverter.GetBytes(VoiceFileNamePointer));
                Data.AddRange(BitConverter.GetBytes(SubtitlePointer));
                Data.AddRange(BitConverter.GetBytes(X));
                Data.AddRange(BitConverter.GetBytes(Y));
                Data.AddRange(BitConverter.GetBytes(FontSize));
                Data.AddRange(BitConverter.GetBytes((short)TargetScreen));
                Data.AddRange(BitConverter.GetBytes(Timer));

                return Data.ToArray();
            }
        }
    }

    public enum Speaker
    {
        KYON = 0x01,
        HARUHI = 0x02,
        MIKURU = 0x03,
        NAGATO = 0x04,
        KOIZUMI = 0x05,
        KYON_SIS = 0x06,
        TSURUYA = 0x07,
        TANIGUCHI = 0x08,
        KUNIKIDA = 0x09,
        CLUB_PRES = 0x0A,
        CLUB_MEM_A = 0x0B,
        CLUB_MEM_B = 0x0C,
        CLUB_MEM_C = 0x0D,
        CLUB_MEM_D = 0x0E,
        OKABE = 0x0F,
        BASEBALL_CAPTAIN = 0x10,
        GROCER = 0x11,
        GIRL = 0x12,
        OLD_LADY = 0x13,
        FAKE_HARUHI = 0x14,
        STRAY_CAT = 0x15,
        UNKNOWN = 0x16,
        INFO = 0x17,
        MONOLOGUE = 0x18,
        MAIL = 0x19,
    }
}
