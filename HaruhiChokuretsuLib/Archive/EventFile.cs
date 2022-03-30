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
    public class EventFile : FileInArchive
    {
        public List<int> FrontPointers { get; set; } = new();
        public int PointerToEndPointerSection { get; set; }
        public List<int> EndPointers { get; set; } = new();
        public List<int> EndPointerPointers { get; set; } = new();
        public string Title { get; set; }

        public Dictionary<int, string> DramatisPersonae { get; set; } = new();
        public int DialogueSectionPointer { get; set; }
        public List<DialogueLine> DialogueLines { get; set; } = new();

        public List<TopicStruct> TopicStructs { get; set; } = new();

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

            int numFrontPointers = BitConverter.ToInt32(decompressedData.Take(4).ToArray());
            bool reachedDramatisPersonae = false;
            for (int i = 0; i < numFrontPointers; i++)
            {
                FrontPointers.Add(BitConverter.ToInt32(decompressedData.Skip(0x0C + (0x08 * i)).Take(4).ToArray()));
                uint pointerValue = BitConverter.ToUInt32(decompressedData.Skip(FrontPointers[i]).Take(4).ToArray());
                if (pointerValue > 0x10000000 || pointerValue == 0x8596) // 8596 is 妹 which is a valid character name, sadly lol
                {
                    reachedDramatisPersonae = true;
                    DramatisPersonae.Add(FrontPointers[i],
                        Encoding.GetEncoding("Shift-JIS").GetString(decompressedData.Skip(FrontPointers[i]).TakeWhile(b => b != 0x00).ToArray()));
                }
                else if (reachedDramatisPersonae)
                {
                    reachedDramatisPersonae = false;
                    DialogueSectionPointer = FrontPointers[i];
                }
            }

            InitializeDialogueAndEndPointers(decompressedData, offset);
        }

        protected void InitializeDialogueAndEndPointers(byte[] decompressedData, int offset)
        {
            for (int i = 0; DialogueSectionPointer + i < decompressedData.Length - 0x0C; i += 0x0C)
            {
                int character = BitConverter.ToInt32(decompressedData.Skip(DialogueSectionPointer + i).Take(4).ToArray());
                int speakerPointer = BitConverter.ToInt32(decompressedData.Skip(DialogueSectionPointer + i + 4).Take(4).ToArray());
                int dialoguePointer = BitConverter.ToInt32(decompressedData.Skip(DialogueSectionPointer + i + 8).Take(4).ToArray());

                if (character == 0 && speakerPointer == 0 && dialoguePointer == 0)
                {
                    break;
                }

                DramatisPersonae.TryGetValue(speakerPointer, out string speakerName);
                DialogueLines.Add(new DialogueLine((Speaker)character, speakerName, speakerPointer, dialoguePointer, Data.ToArray()));
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
            int topicsSectionPointer = FrontPointers.Where(f => f > DialogueLines.Last(d => d.SpeakerName != "CHOICE").Pointer).ToArray()[3]; // third pointer after dialogue section
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
            for (int i = 0; i < FrontPointers.Count; i++)
            {
                if (FrontPointers[i] > shiftLocation)
                {
                    FrontPointers[i] += shiftAmount;
                    Data.RemoveRange(0x0C + (0x08 * i), 4);
                    Data.InsertRange(0x0C + (0x08 * i), BitConverter.GetBytes(FrontPointers[i]));
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
                            || dialogueText[i + 1] == ' ' || dialogueText[i + 1] == '!' || dialogueText[i + 1] == '?' || dialogueText[i + 1] == '.' || dialogueText[i + 1] == '…'))
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
            if (!string.IsNullOrWhiteSpace(Title))
            {
                return $"{Index:X3} {Index:D3} 0x{Offset:X8} '{Title}'";
            }
            else if (DialogueLines.Count > 0)
            {
                return $"{Index:X3} {Index:D3} 0x{Offset:X8}, Line 1: {DialogueLines[0].Text}";
            }
            else
            {
                return $"{Index:X3} {Index:D3} 0x{Offset:X8}";
            }
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
                FrontPointers.Add(BitConverter.ToInt32(decompressedData.Skip(0x0C + (0x08 * i)).Take(4).ToArray()));
            }
            VoiceMapStructSectionOffset = FrontPointers[0];
            DialogueSectionPointer = FrontPointers[1];
            DialogueLinesPointer = DialogueSectionPointer + (FrontPointers.Count - 1) * 12;

            for (int i = 2; i < FrontPointers.Count; i++)
            {
                DramatisPersonae.Add(FrontPointers[i], Encoding.ASCII.GetString(Data.Skip(FrontPointers[i]).TakeWhile(b => b != 0).ToArray()));
            }

            for (int i = 0; i < FrontPointers.Count - 2; i++)
            {
                VoiceMapStructs.Add(new(Data.Skip(VoiceMapStructSectionOffset + i * 16).Take(16)));
            }

            InitializeDialogueAndEndPointers(decompressedData, offset);
        }

        public override void NewFile(string filename)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Data = new();
            string[] csvData = File.ReadAllLines(filename);

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
                    case "BOTTOM":
                        y = 160;
                        break;
                }

                EndPointers.AddRange(new int[] { Data.Count, Data.Count + 4 }); // Add the next two pointers to end pointers
                EndPointerPointers.AddRange(new int[] { filenamePointers[i] + filenameSectionStart, dialogueLinePointers[i] + DialogueLinesPointer });
                VoiceMapStruct vmStruct = new()
                {
                    VoiceFileNamePointer = filenamePointers[i] + filenameSectionStart,
                    SubtitlePointer = dialogueLinePointers[i] + DialogueLinesPointer,
                    X = (short)((256 - lineLength) / 2),
                    Y = y,
                    FontSize = 130,
                    Timer = ushort.Parse(fields[3]),
                };

                VoiceMapStructs.Add(vmStruct);
                Data.AddRange(vmStruct.GetBytes());
            }
            Data.AddRange(new byte[16]);

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

            int lineLength = newText.Sum(c => FontReplacementMap.ReverseLookup(c)?.Offset ?? 15);
            VoiceMapStructs[index].X = (short)((256 - lineLength) / 2);
            Data.RemoveRange(VoiceMapStructSectionOffset + 16 * index + 8, 2); // Replace X in Data
            Data.InsertRange(VoiceMapStructSectionOffset + 16 * index + 8, BitConverter.GetBytes(VoiceMapStructs[index].X));
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
            public List<byte> Data { get; set; } = new();
            public int VoiceFileNamePointer { get; set; }
            public int SubtitlePointer { get; set; }
            public short X { get; set; }
            public short Y { get; set; }
            public short FontSize { get; set; }
            public ushort Timer { get; set; }

            public VoiceMapStruct()
            {
            }

            public VoiceMapStruct(IEnumerable<byte> data)
            {
                if (data.Count() != 0x10)
                {
                    throw new ArgumentException($"Voice map struct data length must be 0x10, was 0x{data.Count():X2}");
                }

                VoiceFileNamePointer = BitConverter.ToInt32(data.Take(4).ToArray());
                SubtitlePointer = BitConverter.ToInt32(data.Skip(4).Take(4).ToArray());
                X = BitConverter.ToInt16(data.Skip(8).Take(2).ToArray());
                Y = BitConverter.ToInt16(data.Skip(10).Take(2).ToArray());
                FontSize = BitConverter.ToInt16(data.Skip(12).Take(2).ToArray());
                Timer = BitConverter.ToUInt16(data.Skip(14).Take(2).ToArray());

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
