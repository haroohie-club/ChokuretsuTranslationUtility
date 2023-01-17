using HaruhiChokuretsuLib.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace HaruhiChokuretsuLib.Archive.Event
{
    public class VoiceMapFile : EventFile
    {
        public int VoiceMapStructSectionOffset { get; set; }
        public int DialogueLinesPointer { get; set; }
        public List<VoiceMapStruct> VoiceMapStructs { get; set; } = new();

        public override void Initialize(byte[] decompressedData, int offset, ILogger log)
        {
            _log = log;
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Offset = offset;
            Data = decompressedData.ToList();

            int numFrontPointers = BitConverter.ToInt32(decompressedData.Take(4).ToArray());
            for (int i = 0; i < numFrontPointers; i++)
            {
                SectionPointersAndCounts.Add(new()
                {
                    Pointer = BitConverter.ToInt32(decompressedData.Skip(0x0C + 0x08 * i).Take(4).ToArray()),
                    ItemCount = BitConverter.ToInt32(decompressedData.Skip(0x10 + 0x08 * i).Take(4).ToArray()),
                });
            }
            Settings = new(new byte[0x128]);
            VoiceMapStructSectionOffset = SectionPointersAndCounts[0].Pointer;
            Settings.DialogueSectionPointer = SectionPointersAndCounts[1].Pointer;
            DialogueLinesPointer = Settings.DialogueSectionPointer + (SectionPointersAndCounts.Count - 1) * 12;
            Settings.NumDialogueEntries = numFrontPointers - 2;

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

        public override void NewFile(string filename, ILogger log)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Data = new();
            string[] csvData = File.ReadAllLines(filename);
            Name = "VOICEMAPS";
            _log = log;

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
                _log.LogWarning($"File {Index} has subtitle too long ({index}) (starting with: {DialogueLines[index].Text[4..20]})");
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
}
