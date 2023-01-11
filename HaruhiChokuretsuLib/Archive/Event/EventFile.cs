﻿using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Font;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Resources.NetStandard;
using System.Text;
using System.Text.RegularExpressions;

namespace HaruhiChokuretsuLib.Archive.Event
{
    public partial class EventFile : FileInArchive, ISourceFile
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
        public List<ScriptSection> ScriptSections { get; private set; } = new();
        public static List<ScriptCommand> CommandsAvailable { get; set; } = new()
        {
            new(0x00, "UNKNOWN00", Array.Empty<string>()),
            new(0x01, "DIALOGUE", new string[] { "dialogueIndex", "spriteIndex", "spriteEntranceTransition", "spriteExitOrInternalTransition", "spriteShake", "voiceIndex", "textVoiceFont", "textSpeed" }),
            new(0x02, "KBG_DISP", new string[] { "kbgIndex" }),
            new(0x03, "UNKNOWN03", Array.Empty<string>()),
            new(0x04, "BG_DISP", new string[] { "bgIndex" }),
            new(0x05, "SCREEN_FADEIN", new string[] { "timeToFade", "unused", "fadeLocation", "fadeColor" }),
            new(0x06, "SCREEN_FADEOUT", new string[] { "timeToFade", "unused", "fadeColorRed", "fadeColorGreen", "fadeColorBlue", "fadeLocation" }),
            new(0x07, "UNKNOWN07", Array.Empty<string>()),
            new(0x08, "SND_PLAY", new string[] { "soundIndex", "mode", "volume", "crossfadeDupe", "crossfadeTime" }),
            new(0x09, "REMOVED", Array.Empty<string>()),
            new(0x0A, "UNKNOWN0A", Array.Empty<string>()),
            new(0x0B, "BGM_PLAY", new string[] { "bgmIndex", "mode", "volume", "fadeInTime", "fadeOutTime" }),
            new(0x0C, "UNKNOWN0C", Array.Empty<string>()),
            new(0x0D, "UNKNOWN0D", Array.Empty<string>()),
            new(0x0E, "UNKNOWN0E", Array.Empty<string>()),
            new(0x0F, "TOGGLE_DIALOGUE", new string[] { "show" }),
            new(0x10, "SELECT", new string[] { "option1", "option2", "option3", "option4", "unknown04", "unknown05", "unknown06", "unknown07" }),
            new(0x11, "SCREEN_SHAKE", new string[] { "duration", "horizontalIntensity", "verticalIntensity" }),
            new(0x12, "UNKNOWN12", Array.Empty<string>()),
            new(0x13, "UNKNOWN13", Array.Empty<string>()),
            new(0x14, "UNKNOWN14", Array.Empty<string>()),
            new(0x15, "WAIT", new string[] { "frames" }),
            new(0x16, "UNKNOWN16", Array.Empty<string>()),
            new(0x17, "NOOP1", Array.Empty<string>()),
            new(0x18, "UNKNOWN18", Array.Empty<string>()),
            new(0x19, "HARUHI_METER", new string[] { "unused", "addValue", "setValue" }),
            new(0x1A, "UNKNOWN1A", Array.Empty<string>()),
            new(0x1B, "BG_PALEFFECT", new string[] { "paletteMode", "transitionTime", "unknownBool" }),
            new(0x1C, "BG_FADE", new string[] { "bgIndex", "bgIndexSuper", "fadeTime" }),
            new(0x1D, "TRANS_OUT", new string[] { "index" }),
            new(0x1E, "TRANS_IN", new string[] { "index" }),
            new(0x1F, "SET_PLACE", new string[] { "display", "placeIndex" }),
            new(0x20, "UNKNOWN20", Array.Empty<string>()),
            new(0x21, "SET_READ_FLAG", Array.Empty<string>()),
            new(0x22, "UNKNOWN22", Array.Empty<string>()),
            new(0x23, "UNKNOWN23", Array.Empty<string>()),
            new(0x24, "UNKNOWN24", Array.Empty<string>()),
            new(0x25, "UNKNOWN25", Array.Empty<string>()),
            new(0x26, "UNKNOWN26", Array.Empty<string>()),
            new(0x27, "UNKNOWN27", new string[] { "unknown00", "unknown02" }),
            new(0x28, "NEXT_SCENE", Array.Empty<string>()),
            new(0x29, "UNKNOWN29", Array.Empty<string>()),
            new(0x2A, "UNKNOWN2A", Array.Empty<string>()),
            new(0x2B, "CHIBI_ENTEREXIT", new string[] { "chibiIndex", "mode", "delay" }),
            new(0x2C, "UNKNOWN2C", Array.Empty<string>()),
            new(0x2D, "UNKNOWN2D", Array.Empty<string>()),
            new(0x2E, "UNKNOWN2E", Array.Empty<string>()),
            new(0x2F, "UNKNOWN2F", Array.Empty<string>()),
            new(0x30, "UNKNOWN30", Array.Empty<string>()),
            new(0x31, "UNKNOWN31", Array.Empty<string>()),
            new(0x32, "UNKNOWN32", Array.Empty<string>()),
            new(0x33, "UNKNOWN33", Array.Empty<string>()),
            new(0x34, "UNKNOWN34", Array.Empty<string>()),
            new(0x35, "UNKNOWN35", Array.Empty<string>()),
            new(0x36, "UNKNOWN36", Array.Empty<string>()),
            new(0x37, "EPHEADER", new string[] { "headerIndex" }),
            new(0x38, "NOOP2", Array.Empty<string>()),
            new(0x39, "UNKNOWN39", Array.Empty<string>()),
            new(0x3A, "BG_DISPTEMP", new string[] { "bgIndex" }),
            new(0x3B, "UNKNOWN3B", Array.Empty<string>()),
            new(0x3C, "OP_MODE", Array.Empty<string>()),
            new(0x3D, "WAIT_CANCEL", new string[] { "frames" }),
            new(0x3E, "BG_REVERT", Array.Empty<string>()),
            new(0x3F, "UNKNOWN3F", Array.Empty<string>()),
        };

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
                    Pointer = BitConverter.ToInt32(decompressedData.Skip(0x0C + 0x08 * i).Take(4).ToArray()),
                    ItemCount = BitConverter.ToInt32(decompressedData.Skip(0x10 + 0x08 * i).Take(4).ToArray()),
                });
            }

            SettingsSection settingsSection = new();
            settingsSection.Initialize(decompressedData.Skip(SectionPointersAndCounts[0].Pointer).Take(EventFileSettings.SETTINGS_LENGTH), 1, "SETTINGS", SectionPointersAndCounts[0].Pointer);
            Settings = settingsSection.Objects[0];
            SectionPointersAndCounts[0].Section = settingsSection.GetGeneric();
            int dialogueSectionPointerIndex = SectionPointersAndCounts.FindIndex(s => s.Pointer == Settings.DialogueSectionPointer);

            if (Name == "BGTESTS" || Regex.IsMatch(Name, @"[MCT][AHKNTR]\d{2}S") || Regex.IsMatch(Name, @"CHS_\w{3}_\d{2}S") || Regex.IsMatch(Name, @"E[VD]\d?_\d{3}S"))
            {
                if (Settings.UnknownSection01Pointer > 0)
                {
                    string name = "UNKNOWNSECTION01";

                    (int pointerSectionIndex, PointerSection pointerSection) = PointerSection.ParseSection(SectionPointersAndCounts, Settings.UnknownSection01Pointer, name, Data);
                    SectionPointersAndCounts[pointerSectionIndex].Section = pointerSection.GetGeneric();

                    int unknownSection01Index = SectionPointersAndCounts.FindIndex(s => s.Pointer == pointerSection.Objects[0].Pointer);
                    IntegerSection unknown01Section = new();
                    unknown01Section.Initialize(Data.Skip(SectionPointersAndCounts[unknownSection01Index].Pointer)
                        .Take(SectionPointersAndCounts[unknownSection01Index + 1].Pointer - SectionPointersAndCounts[unknownSection01Index].Pointer),
                        SectionPointersAndCounts[unknownSection01Index].ItemCount,
                        name, SectionPointersAndCounts[unknownSection01Index].Pointer);
                    SectionPointersAndCounts[unknownSection01Index].Section = unknown01Section.GetGeneric();
                }

                if (Settings.UnknownSection02Pointer > 0)
                {
                    string name = "UNKNOWNSECTION02";

                    (int pointerSectionIndex, PointerSection pointerSection) = PointerSection.ParseSection(SectionPointersAndCounts, Settings.UnknownSection02Pointer, name, Data);
                    SectionPointersAndCounts[pointerSectionIndex].Section = pointerSection.GetGeneric();

                    int unknownSection02Index = SectionPointersAndCounts.FindIndex(s => s.Pointer == pointerSection.Objects[0].Pointer);
                    Unknown02Section unknown02Section = new();
                    unknown02Section.Initialize(Data.Skip(SectionPointersAndCounts[unknownSection02Index].Pointer)
                        .Take(SectionPointersAndCounts[unknownSection02Index + 1].Pointer - SectionPointersAndCounts[unknownSection02Index].Pointer),
                        SectionPointersAndCounts[unknownSection02Index].ItemCount,
                        name, SectionPointersAndCounts[unknownSection02Index].Pointer);
                    SectionPointersAndCounts[unknownSection02Index].Section = unknown02Section.GetGeneric();
                }

                if (Settings.UnknownSection03Pointer > 0)
                {
                    string name = "UNKNOWNSECTION03";

                    (int pointerSectionIndex, PointerSection pointerSection) = PointerSection.ParseSection(SectionPointersAndCounts, Settings.UnknownSection03Pointer, name, Data);
                    SectionPointersAndCounts[pointerSectionIndex].Section = pointerSection.GetGeneric();

                    int unknownSection03Index = SectionPointersAndCounts.FindIndex(s => s.Pointer == pointerSection.Objects[0].Pointer);
                    Unknown03Section unknown03Section = new();
                    unknown03Section.Initialize(Data.Skip(SectionPointersAndCounts[unknownSection03Index].Pointer)
                        .Take(SectionPointersAndCounts[unknownSection03Index + 1].Pointer - SectionPointersAndCounts[unknownSection03Index].Pointer),
                        SectionPointersAndCounts[unknownSection03Index].ItemCount,
                        name, SectionPointersAndCounts[unknownSection03Index].Pointer);
                    SectionPointersAndCounts[unknownSection03Index].Section = unknown03Section.GetGeneric();
                }

                if (Settings.UnknownSection04Pointer > 0)
                {
                    string name = "UNKNOWNSECTION04";

                    (int pointerSectionIndex, PointerSection pointerSection) = PointerSection.ParseSection(SectionPointersAndCounts, Settings.UnknownSection04Pointer, name, Data);
                    SectionPointersAndCounts[pointerSectionIndex].Section = pointerSection.GetGeneric();

                    int unknownSection04Index = SectionPointersAndCounts.FindIndex(s => s.Pointer == pointerSection.Objects[0].Pointer);
                    Unknown04Section unknown04Section = new();
                    unknown04Section.Initialize(Data.Skip(SectionPointersAndCounts[unknownSection04Index].Pointer)
                        .Take(SectionPointersAndCounts[unknownSection04Index + 1].Pointer - SectionPointersAndCounts[unknownSection04Index].Pointer),
                        SectionPointersAndCounts[unknownSection04Index].ItemCount,
                        name, SectionPointersAndCounts[unknownSection04Index].Pointer);
                    SectionPointersAndCounts[unknownSection04Index].Section = unknown04Section.GetGeneric();
                }

                if (Settings.UnknownSection05Pointer > 0)
                {
                    string name = "UNKNOWNSECTION05";

                    (int pointerSectionIndex, PointerSection pointerSection) = PointerSection.ParseSection(SectionPointersAndCounts, Settings.UnknownSection05Pointer, name, Data);
                    SectionPointersAndCounts[pointerSectionIndex].Section = pointerSection.GetGeneric();

                    int unknownSection05Index = SectionPointersAndCounts.FindIndex(s => s.Pointer == pointerSection.Objects[0].Pointer);
                    Unknown05Section unknown05Section = new();
                    unknown05Section.Initialize(Data.Skip(SectionPointersAndCounts[unknownSection05Index].Pointer)
                        .Take(SectionPointersAndCounts[unknownSection05Index + 1].Pointer - SectionPointersAndCounts[unknownSection05Index].Pointer),
                        SectionPointersAndCounts[unknownSection05Index].ItemCount,
                        name, SectionPointersAndCounts[unknownSection05Index].Pointer);
                    SectionPointersAndCounts[unknownSection05Index].Section = unknown05Section.GetGeneric();
                }

                if (Settings.UnknownSection06Pointer > 0)
                {
                    string name = "UNKNOWNSECTION06";

                    (int pointerSectionIndex, PointerSection pointerSection) = PointerSection.ParseSection(SectionPointersAndCounts, Settings.UnknownSection06Pointer, name, Data);
                    SectionPointersAndCounts[pointerSectionIndex].Section = pointerSection.GetGeneric();

                    int unknownSection06Index = SectionPointersAndCounts.FindIndex(s => s.Pointer == pointerSection.Objects[0].Pointer);
                    IntegerSection unknown06Section = new();
                    unknown06Section.Initialize(Data.Skip(SectionPointersAndCounts[unknownSection06Index].Pointer)
                        .Take(SectionPointersAndCounts[unknownSection06Index + 1].Pointer - SectionPointersAndCounts[unknownSection06Index].Pointer),
                        SectionPointersAndCounts[unknownSection06Index].ItemCount,
                        name, SectionPointersAndCounts[unknownSection06Index].Pointer);
                    SectionPointersAndCounts[unknownSection06Index].Section = unknown06Section.GetGeneric();
                }

                if (Settings.UnknownSection07Pointer > 0)
                {
                    string name = "UNKNOWNSECTION07";

                    (int pointerSectionIndex, PointerSection pointerSection) = PointerSection.ParseSection(SectionPointersAndCounts, Settings.UnknownSection07Pointer, name, Data);
                    SectionPointersAndCounts[pointerSectionIndex].Section = pointerSection.GetGeneric();

                    int unknownSection07Index = SectionPointersAndCounts.FindIndex(s => s.Pointer == pointerSection.Objects[0].Pointer);
                    Unknown07Section unknown07Section = new();
                    unknown07Section.Initialize(Data.Skip(SectionPointersAndCounts[unknownSection07Index].Pointer)
                        .Take(SectionPointersAndCounts[unknownSection07Index + 1].Pointer - SectionPointersAndCounts[unknownSection07Index].Pointer),
                        SectionPointersAndCounts[unknownSection07Index].ItemCount,
                        name, SectionPointersAndCounts[unknownSection07Index].Pointer);
                    SectionPointersAndCounts[unknownSection07Index].Section = unknown07Section.GetGeneric();
                }

                int choicesSectionIndex = SectionPointersAndCounts.FindIndex(s => s.Pointer == Settings.ChoicesSectionPointer);
                if (Settings.ChoicesSectionPointer > 0)
                {
                    string name = "CHOICES";

                    ChoicesSection choicesSection = new();
                    choicesSection.Initialize(Data.Skip(SectionPointersAndCounts[choicesSectionIndex].Pointer)
                        .Take(SectionPointersAndCounts[choicesSectionIndex + 1].Pointer - SectionPointersAndCounts[choicesSectionIndex].Pointer),
                        SectionPointersAndCounts[choicesSectionIndex].ItemCount,
                        name, SectionPointersAndCounts[choicesSectionIndex].Pointer);
                    SectionPointersAndCounts[choicesSectionIndex].Section = choicesSection.GetGeneric();
                }

                int unknownSection09Index = SectionPointersAndCounts.FindIndex(s => s.Pointer == Settings.UnknownSection09Pointer);
                if (unknownSection09Index > choicesSectionIndex + 1)
                {
                    string name = "UNKNOWNSECTION08";

                    (int pointerSectionIndex, PointerSection pointerSection) = PointerSection.ParseSection(SectionPointersAndCounts, SectionPointersAndCounts[choicesSectionIndex + 2].Pointer, name, Data);
                    SectionPointersAndCounts[pointerSectionIndex].Section = pointerSection.GetGeneric();

                    Unknown08Section unknown08Section = new();
                    unknown08Section.Initialize(Data.Skip(SectionPointersAndCounts[choicesSectionIndex + 1].Pointer)
                        .Take(SectionPointersAndCounts[choicesSectionIndex + 2].Pointer - SectionPointersAndCounts[choicesSectionIndex + 1].Pointer),
                        SectionPointersAndCounts[choicesSectionIndex + 1].ItemCount,
                        name, SectionPointersAndCounts[choicesSectionIndex + 1].Pointer);
                    SectionPointersAndCounts[choicesSectionIndex + 1].Section = unknown08Section.GetGeneric();
                }

                if (Settings.UnknownSection09Pointer > 0)
                {
                    string name = "UNKNOWNSECTION09";

                    Unknown09Section unknown09Section = new();
                    unknown09Section.Initialize(Data.Skip(SectionPointersAndCounts[unknownSection09Index].Pointer)
                        .Take(SectionPointersAndCounts[unknownSection09Index + 1].Pointer - SectionPointersAndCounts[unknownSection09Index].Pointer),
                        SectionPointersAndCounts[unknownSection09Index].ItemCount,
                        name, SectionPointersAndCounts[unknownSection09Index].Pointer);
                    SectionPointersAndCounts[unknownSection09Index].Section = unknown09Section.GetGeneric();
                }

                if (Settings.UnknownSection10Pointer > 0)
                {
                    string name = "UNKNOWNSECTION10";

                    int unknownSection10Index = SectionPointersAndCounts.FindIndex(s => s.Pointer == Settings.UnknownSection10Pointer);
                    Unknown09Section unknown10Section = new();
                    unknown10Section.Initialize(Data.Skip(SectionPointersAndCounts[unknownSection10Index].Pointer)
                        .Take(SectionPointersAndCounts[unknownSection10Index + 1].Pointer - SectionPointersAndCounts[unknownSection10Index].Pointer),
                        SectionPointersAndCounts[unknownSection10Index].ItemCount,
                        name, SectionPointersAndCounts[unknownSection10Index].Pointer);
                    SectionPointersAndCounts[unknownSection10Index].Section = unknown10Section.GetGeneric();
                }

                int labelsSectionPointerIndex = SectionPointersAndCounts.FindIndex(s => s.Pointer == Settings.LabelsSectionPointer);
                LabelsSection labelsSection = new();
                if (Settings.LabelsSectionPointer > 0)
                {
                    string name = "LABELS";

                    labelsSection.Initialize(Data.Skip(SectionPointersAndCounts[labelsSectionPointerIndex].Pointer)
                        .Take(SectionPointersAndCounts[labelsSectionPointerIndex + 1].Pointer - SectionPointersAndCounts[labelsSectionPointerIndex].Pointer),
                        SectionPointersAndCounts[labelsSectionPointerIndex].ItemCount,
                        name, SectionPointersAndCounts[labelsSectionPointerIndex].Pointer);
                    SectionPointersAndCounts[labelsSectionPointerIndex].Section = labelsSection.GetGeneric();
                }

                List<DramatisPersonaeSection> dramatisPersonae = new();
                if (dialogueSectionPointerIndex > labelsSectionPointerIndex + 1)
                {
                    for (int i = labelsSectionPointerIndex + 1; i < dialogueSectionPointerIndex; i++)
                    {
                        string name = $"DRAMATISPERSONAE{i - labelsSectionPointerIndex}";

                        DramatisPersonaeSection dramatisPersonaeSection = new() { Index = i - labelsSectionPointerIndex };
                        dramatisPersonaeSection.Initialize(Data.Skip(SectionPointersAndCounts[i].Pointer)
                            .Take(SectionPointersAndCounts[i + 1].Pointer - SectionPointersAndCounts[i].Pointer),
                            SectionPointersAndCounts[i].ItemCount,
                            name, SectionPointersAndCounts[i].Pointer);
                        SectionPointersAndCounts[i].Section = dramatisPersonaeSection.GetGeneric();
                        dramatisPersonae.Add(dramatisPersonaeSection);
                    }
                }

                if (Settings.DialogueSectionPointer > 0)
                {
                    string name = "DIALOGUESECTION";

                    DialogueSection dialogueSection = new();
                    dialogueSection.Initialize(Data,
                        SectionPointersAndCounts[dialogueSectionPointerIndex].ItemCount,
                        name, SectionPointersAndCounts[dialogueSectionPointerIndex].Pointer);
                    dialogueSection.InitializeDramatisPersonaeIndices(dramatisPersonae);
                    SectionPointersAndCounts[dialogueSectionPointerIndex].Section = dialogueSection.GetGeneric();
                }

                int unknownSection11Index = SectionPointersAndCounts.FindIndex(s => s.Pointer == Settings.UnknownSection11Pointer);
                if (Settings.UnknownSection11Pointer > 0)
                {
                    string name = "UNKNOWNSECTION11";

                    Unknown11Section unknown11Section = new();
                    unknown11Section.Initialize(Data,
                        SectionPointersAndCounts[unknownSection11Index].ItemCount,
                        name, SectionPointersAndCounts[unknownSection11Index].Pointer);
                    SectionPointersAndCounts[unknownSection11Index].Section = unknown11Section.GetGeneric();
                }

                int scriptSectionDefinitionsSectionIndex = SectionPointersAndCounts.FindIndex(s => s.Pointer == Settings.ScriptSectionDefinitionsSectionPointer);
                ScriptSectionDefinitionsSection scriptSectionDefinitionsSection = new() { Labels = labelsSection.Objects.Select(l => l.Name.Replace("NONE/", "")).ToList() };
                if (Settings.ScriptSectionDefinitionsSectionPointer > 0)
                {
                    string name = "SCRIPTDEFINITIONS";

                    scriptSectionDefinitionsSection.Initialize(Data
                        .Skip(SectionPointersAndCounts[scriptSectionDefinitionsSectionIndex].Pointer),
                        Settings.NumScriptSections,
                        name,
                        SectionPointersAndCounts[scriptSectionDefinitionsSectionIndex].Pointer);
                    SectionPointersAndCounts[scriptSectionDefinitionsSectionIndex].Section = scriptSectionDefinitionsSection.GetGeneric();
                }

                for (int i = 0; i < scriptSectionDefinitionsSection.Objects.Count; i++)
                {
                    if (scriptSectionDefinitionsSection.Objects[i].Pointer > 0)
                    {
                        ScriptSection scriptSection = new() { CommandsAvailable = CommandsAvailable };
                        scriptSection.Initialize(Data.Skip(scriptSectionDefinitionsSection.Objects[i].Pointer).Take(scriptSectionDefinitionsSection.Objects[i].NumCommands * 0x24),
                            scriptSectionDefinitionsSection.Objects[i].NumCommands,
                            string.IsNullOrEmpty(labelsSection.Objects[i].Name) ? $"SCRIPT{i:D2}" : labelsSection.Objects[i].Name.Replace("NONE/", ""),
                            scriptSectionDefinitionsSection.Objects[i].Pointer);
                        SectionPointersAndCounts[SectionPointersAndCounts.IndexOf(SectionPointersAndCounts.First(s => s.Pointer == scriptSectionDefinitionsSection.Objects[i].Pointer))].Section = scriptSection.GetGeneric();
                        ScriptSections.Add(scriptSection);
                    }
                }

                int eventNameSectionIndex = SectionPointersAndCounts.FindIndex(s => s.Pointer == Settings.EventNamePointer);
                if (Settings.EventNamePointer > 0)
                {
                    string name = "EVENTNAME";

                    EventNameSection eventNameSection = new();
                    eventNameSection.Initialize(Data.Skip(SectionPointersAndCounts[eventNameSectionIndex].Pointer).TakeWhile(b => b != 0),
                        SectionPointersAndCounts[eventNameSectionIndex].ItemCount, name, SectionPointersAndCounts[eventNameSectionIndex].Pointer);
                    SectionPointersAndCounts[eventNameSectionIndex].Section = eventNameSection.GetGeneric();
                }
            }

            for (int i = SectionPointersAndCounts.FindIndex(f => f.Pointer == Settings.LabelsSectionPointer) + 1; i < dialogueSectionPointerIndex; i++)
            {
                DramatisPersonae.Add(SectionPointersAndCounts[i].Pointer,
                    Encoding.GetEncoding("Shift-JIS").GetString(decompressedData.Skip(SectionPointersAndCounts[i].Pointer).TakeWhile(b => b != 0x00).ToArray()));
            }

            InitializeDialogueAndEndPointers(decompressedData, offset);
        }

        protected void InitializeDialogueAndEndPointers(byte[] decompressedData, int offset, bool @override = false)
        {
            if (Name != "CHESSS" && Name != "EVTTBLS" && Name != "TOPICS" && Name != "SCENARIOS" && Name != "VOICEMAPS"
                && Name != "MESSS"
                && Settings.DialogueSectionPointer < decompressedData.Length || @override)
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
                EndPointers.Add(BitConverter.ToInt32(decompressedData.Skip(PointerToEndPointerSection + 0x04 * (i + 1)).Take(4).ToArray()));
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
            DialogueLines[index].NumPaddingZeroes = 4 - DialogueLines[index].Length % 4;
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
                    Data.RemoveRange(0x0C + 0x08 * i, 4);
                    Data.InsertRange(0x0C + 0x08 * i, BitConverter.GetBytes(SectionPointersAndCounts[i].Pointer));
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

                if (!datFile && dialogueText.Count(c => c == '\n') > 1 || DialogueLines[dialogueIndex].SpeakerName == "CHOICE" && dialogueText.Length > 256)
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
            else if (Name == "VOICEMAPS")
            {
                return "";
            }
            else
            {
                StringBuilder builder = new();
                builder.AppendLine(".include \"COMMANDS.INC\"");
                //builder.AppendLine(".include \"DATBIN.INC\"");
                //builder.AppendLine(".include \"EVTBIN.INC\"");
                //builder.AppendLine(".include"GRPBIN.INC\"");
                //builder.AppendLine(".include \"BGTBL.S\"");
                builder.AppendLine();
                builder.AppendLine($".word {NumSections}");
                builder.AppendLine(".word END_POINTERS");
                builder.AppendLine(".word FILE_START");

                foreach (EventFileSection section in SectionPointersAndCounts)
                {
                    builder.AppendLine($".word {section.Section?.Name ?? "UNRECOGNIZED_SECTION"}");
                    builder.AppendLine($".word {section.ItemCount}");
                }

                builder.AppendLine();
                builder.AppendLine("FILE_START:");

                int currentPointer = 0;
                foreach (EventFileSection section in SectionPointersAndCounts.OrderBy(s => s.Pointer))
                {
                    if (section.Section is not null)
                    {
                        dynamic typedSection = Convert.ChangeType(section.Section, section.Section.SectionType);
                        builder.AppendLine(typedSection.GetAsm(0, ref currentPointer));
                    }
                    else
                    {
                        builder.AppendLine();
                    }
                }

                builder.AppendLine("END_POINTERS:");
                builder.AppendLine($"    .word {currentPointer}");
                for (int i = 0; i < currentPointer; i++)
                {
                    builder.AppendLine($"    .word POINTER{i}");
                }

                return builder.ToString();
            }
        }
    }

    public class EventFileSection
    {
        public int Pointer { get; set; }
        public int ItemCount { get; set; }
        public IEventSection<object> Section { get; set; }

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
        public int NumUnknown09 { get; set; }
        public int UnknownSection09Pointer { get; set; } // maybe unused
        public int NumUnknown10 { get; set; }
        public int UnknownSection10Pointer { get; set; } // seems unused
        public int NumLabels { get; set; }
        public int LabelsSectionPointer { get; set; }
        public int NumDialogueEntries { get; set; }
        public int DialogueSectionPointer { get; set; }
        public int NumUnknown11 { get; set; }
        public int UnknownSection11Pointer { get; set; }
        public int NumScriptSections { get; set; }
        public int ScriptSectionDefinitionsSectionPointer { get; set; }

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
            NumUnknown09 = BitConverter.ToInt32(data.Skip(0x04C).Take(4).ToArray());
            UnknownSection09Pointer = BitConverter.ToInt32(data.Skip(0x050).Take(4).ToArray());
            NumUnknown10 = BitConverter.ToInt32(data.Skip(0x054).Take(4).ToArray());
            UnknownSection10Pointer = BitConverter.ToInt32(data.Skip(0x058).Take(4).ToArray());
            NumLabels = BitConverter.ToInt32(data.Skip(0x05C).Take(4).ToArray());
            LabelsSectionPointer = BitConverter.ToInt32(data.Skip(0x060).Take(4).ToArray());
            NumDialogueEntries = BitConverter.ToInt32(data.Skip(0x064).Take(4).ToArray());
            DialogueSectionPointer = BitConverter.ToInt32(data.Skip(0x068).Take(4).ToArray());
            NumUnknown11 = BitConverter.ToInt32(data.Skip(0x06C).Take(4).ToArray());
            UnknownSection11Pointer = BitConverter.ToInt32(data.Skip(0x070).Take(4).ToArray());
            NumScriptSections = BitConverter.ToInt32(data.Skip(0x074).Take(4).ToArray());
            ScriptSectionDefinitionsSectionPointer = BitConverter.ToInt32(data.Skip(0x078).Take(4).ToArray());
        }
    }

    public class DialogueLine
    {
        public int Pointer { get; set; }
        public byte[] Data { get; set; }
        public int NumPaddingZeroes { get; set; }
        public string Text { get => Encoding.GetEncoding("Shift-JIS").GetString(Data); set => Data = Encoding.GetEncoding("Shift-JIS").GetBytes(value); }
        public int Length => Data.Length;

        public int SpeakerIndex { get; set; }
        public int SpeakerPointer { get; set; }
        public Speaker Speaker { get; set; }
        public string SpeakerName { get; set; }

        public int CorrectedIndex { get; set; }

        public DialogueLine(Speaker speaker, string speakerName, int speakerPointer, int pointer, byte[] file)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Speaker = speaker;
            SpeakerName = speakerName;
            SpeakerPointer = speakerPointer;
            Pointer = pointer;
            Data = file.Skip(pointer).TakeWhile(b => b != 0x00).ToArray();
            NumPaddingZeroes = 4 - Length % 4;
        }

        public override string ToString()
        {
            return Text;
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