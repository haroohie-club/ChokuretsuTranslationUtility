using HaruhiChokuretsuLib.Font;
using HaruhiChokuretsuLib.Util;
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

        public ChoicesSection ChoicesSection { get; set; }
        public StartingChibisSection StartingChibisSection { get; set; }
        public MapCharactersSection MapCharactersSection { get; set; }
        public LabelsSection LabelsSection { get; set; }
        public ConditionalSection ConditionalsSection { get; set; }
        public List<ScriptSection> ScriptSections { get; private set; } = new();

        public static List<ScriptCommand> CommandsAvailable { get; set; } = new()
        {
            new(0x00, nameof(CommandVerb.INIT_READ_FLAG), Array.Empty<string>()),
            new(0x01, nameof(CommandVerb.DIALOGUE), new string[] { "dialogueIndex", "spriteIndex", "spriteEntranceTransition", "spriteExitOrInternalTransition", "spriteShake", "voiceIndex", "textVoiceFont", "textSpeed", "unknown8", "unknown9", "unknown10", "unknown11" }),
            new(0x02, nameof(CommandVerb.KBG_DISP), new string[] { "kbgIndex" }),
            new(0x03, nameof(CommandVerb.PIN_MNL), new string[] { "dialogueIndex" }),
            new(0x04, nameof(CommandVerb.BG_DISP), new string[] { "bgIndex" }),
            new(0x05, nameof(CommandVerb.SCREEN_FADEIN), new string[] { "timeToFade", "unused", "fadeLocation", "fadeColor" }),
            new(0x06, nameof(CommandVerb.SCREEN_FADEOUT), new string[] { "timeToFade", "unknown1", "fadeColorRed", "fadeColorGreen", "fadeColorBlue", "fadeLocation", "unknown6" }),
            new(0x07, nameof(CommandVerb.SCREEN_FLASH), new string[] { "fadeInTime", "holdTime", "fadeOutTime", "flashColorRed", "flashColorGreen", "flashColorBlue" }),
            new(0x08, nameof(CommandVerb.SND_PLAY), new string[] { "soundIndex", "mode", "volume", "crossfadeDupe", "crossfadeTime" }),
            new(0x09, nameof(CommandVerb.REMOVED), Array.Empty<string>()),
            new(0x0A, nameof(CommandVerb.UNKNOWN0A), Array.Empty<string>()),
            new(0x0B, nameof(CommandVerb.BGM_PLAY), new string[] { "bgmIndex", "mode", "volume", "fadeInTime", "fadeOutTime" }),
            new(0x0C, nameof(CommandVerb.VCE_PLAY), new string[] { "vceIndex" }),
            new(0x0D, nameof(CommandVerb.FLAG), new string[] { "flag", "set" }),
            new(0x0E, nameof(CommandVerb.TOPIC_GET), new string[] { "topicId" }),
            new(0x0F, nameof(CommandVerb.TOGGLE_DIALOGUE), new string[] { "show" }),
            new(0x10, nameof(CommandVerb.SELECT), new string[] { "option1", "option2", "option3", "option4", "unknown04", "unknown05", "unknown06", "unknown07" }),
            new(0x11, nameof(CommandVerb.SCREEN_SHAKE), new string[] { "duration", "horizontalIntensity", "verticalIntensity" }),
            new(0x12, nameof(CommandVerb.SCREEN_SHAKE_STOP), Array.Empty<string>()),
            new(0x13, nameof(CommandVerb.GOTO), new string[] { "blockId" }),
            new(0x14, nameof(CommandVerb.SCENE_GOTO), new string[] { "conditionalIndex" }),
            new(0x15, nameof(CommandVerb.WAIT), new string[] { "frames" }),
            new(0x16, nameof(CommandVerb.HOLD), Array.Empty<string>()),
            new(0x17, nameof(CommandVerb.NOOP1), Array.Empty<string>()),
            new(0x18, nameof(CommandVerb.VGOTO), new string[] { "conditionalIndex", "unused", "gotoId" }),
            new(0x19, nameof(CommandVerb.HARUHI_METER), new string[] { "unused", "addValue", "setValue" }),
            new(0x1A, nameof(CommandVerb.HARUHI_METER_NOSHOW), new string[] { "addValue" }),
            new(0x1B, nameof(CommandVerb.BG_PALEFFECT), new string[] { "paletteMode", "transitionTime", "unknownBool" }),
            new(0x1C, nameof(CommandVerb.BG_FADE), new string[] { "bgIndex", "bgIndexSuper", "fadeTime" }),
            new(0x1D, nameof(CommandVerb.TRANS_OUT), new string[] { "index" }),
            new(0x1E, nameof(CommandVerb.TRANS_IN), new string[] { "index" }),
            new(0x1F, nameof(CommandVerb.SET_PLACE), new string[] { "display", "placeIndex" }),
            new(0x20, nameof(CommandVerb.ITEM_DISPIMG), new string[] { "itemIndex", "x", "y" }),
            new(0x21, nameof(CommandVerb.SET_READ_FLAG), Array.Empty<string>()),
            new(0x22, nameof(CommandVerb.STOP), Array.Empty<string>()),
            new(0x23, nameof(CommandVerb.NOOP2), Array.Empty<string>()),
            new(0x24, nameof(CommandVerb.LOAD_ISOMAP), new string[] { "mapFileIndex" }),
            new(0x25, nameof(CommandVerb.INVEST_START), new string[] { "unknown00", "unknown01", "unknown02", "unknown03", "endScriptBlock" }),
            new(0x26, nameof(CommandVerb.INVEST_END), Array.Empty<string>()),
            new(0x27, nameof(CommandVerb.CHIBI_EMOTE), new string[] { "chibiIndex", "emoteIndex" }),
            new(0x28, nameof(CommandVerb.NEXT_SCENE), Array.Empty<string>()),
            new(0x29, nameof(CommandVerb.SKIP_SCENE), new string[] { "scenesToSkip" }),
            new(0x2A, nameof(CommandVerb.GLOBAL), new string[] { "globalIndex", "value" }),
            new(0x2B, nameof(CommandVerb.CHIBI_ENTEREXIT), new string[] { "chibiIndex", "mode", "delay" }),
            new(0x2C, nameof(CommandVerb.AVOID_DISP), Array.Empty<string>()),
            new(0x2D, nameof(CommandVerb.GLOBAL2D), new string[] { "value" }),
            new(0x2E, nameof(CommandVerb.CHESS_LOAD), new string[] { "chessFileIndex" }),
            new(0x2F, nameof(CommandVerb.CHESS_VGOTO), new string[] { "clearBlock", "missBlock" , "miss2Block" }),
            new(0x30, nameof(CommandVerb.CHESS_MOVE), new string[] { "whiteSpaceBegin", "whiteSpaceEnd", "blackSpaceBegin", "blackSpaceEnd" }),
            new(0x31, nameof(CommandVerb.CHESS_TOGGLE_GUIDE), new string[] { "piece1", "piece2", "piece3", "piece4" }),
            new(0x32, nameof(CommandVerb.CHESS_TOGGLE_HIGHLIGHT), new string[] { "space1", "space2", "space3", "space4", "space5", "space6", "space7", "space8", "space9", "space10", "space11", "space12", "space13", "space14", "space15", "space16" }),
            new(0x33, nameof(CommandVerb.CHESS_TOGGLE_CROSS), new string[] { "space1", "space2", "space3", "space4", "space5", "space6", "space7", "space8", "space9", "space10", "space11", "space12", "space13", "space14", "space15", "space16" }),
            new(0x34, nameof(CommandVerb.CHESS_CLEAR_ANNOTATIONS), Array.Empty<string>()),
            new(0x35, nameof(CommandVerb.CHESS_RESET), Array.Empty<string>()),
            new(0x36, nameof(CommandVerb.SCENE_GOTO2), new string[] { "conditionalIndex" }),
            new(0x37, nameof(CommandVerb.EPHEADER), new string[] { "headerIndex" }),
            new(0x38, nameof(CommandVerb.NOOP3), Array.Empty<string>()),
            new(0x39, nameof(CommandVerb.CONFETTI), new string[] { "on" }),
            new(0x3A, nameof(CommandVerb.BG_DISPTEMP), new string[] { "bgIndex", "unknown1" }),
            new(0x3B, nameof(CommandVerb.BG_SCROLL), Array.Empty<string>()),
            new(0x3C, nameof(CommandVerb.OP_MODE), Array.Empty<string>()),
            new(0x3D, nameof(CommandVerb.WAIT_CANCEL), new string[] { "frames" }),
            new(0x3E, nameof(CommandVerb.BG_REVERT), Array.Empty<string>()),
            new(0x3F, nameof(CommandVerb.BG_DISP2), new string[] { "bgIndex" }),
        };

        public enum CommandVerb
        {
            INIT_READ_FLAG,
            DIALOGUE,
            KBG_DISP,
            PIN_MNL,
            BG_DISP,
            SCREEN_FADEIN,
            SCREEN_FADEOUT,
            SCREEN_FLASH,
            SND_PLAY,
            REMOVED,
            UNKNOWN0A,
            BGM_PLAY,
            VCE_PLAY,
            FLAG,
            TOPIC_GET,
            TOGGLE_DIALOGUE,
            SELECT,
            SCREEN_SHAKE,
            SCREEN_SHAKE_STOP,
            GOTO,
            SCENE_GOTO,
            WAIT,
            HOLD,
            NOOP1,
            VGOTO,
            HARUHI_METER,
            HARUHI_METER_NOSHOW,
            BG_PALEFFECT,
            BG_FADE,
            TRANS_OUT,
            TRANS_IN,
            SET_PLACE,
            ITEM_DISPIMG,
            SET_READ_FLAG,
            STOP,
            NOOP2,
            LOAD_ISOMAP,
            INVEST_START,
            INVEST_END,
            CHIBI_EMOTE,
            NEXT_SCENE,
            SKIP_SCENE,
            GLOBAL,
            CHIBI_ENTEREXIT,
            AVOID_DISP,
            GLOBAL2D,
            CHESS_LOAD,
            CHESS_VGOTO,
            CHESS_MOVE,
            CHESS_TOGGLE_GUIDE,
            CHESS_TOGGLE_HIGHLIGHT,
            CHESS_TOGGLE_CROSS,
            CHESS_CLEAR_ANNOTATIONS,
            CHESS_RESET,
            SCENE_GOTO2,
            EPHEADER,
            NOOP3,
            CONFETTI,
            BG_DISPTEMP,
            BG_SCROLL,
            OP_MODE,
            WAIT_CANCEL,
            BG_REVERT,
            BG_DISP2
        }

        public EventFile()
        {
        }

        public FontReplacementDictionary FontReplacementMap { get; set; } = new();

        private const int DIALOGUE_LINE_LENGTH = 230;

        public override void Initialize(byte[] decompressedData, int offset, ILogger log)
        {
            _log = log;
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
            settingsSection.Initialize(decompressedData.Skip(SectionPointersAndCounts[0].Pointer).Take(EventFileSettings.SETTINGS_LENGTH), 1, "SETTINGS", log, SectionPointersAndCounts[0].Pointer);
            Settings = settingsSection.Objects[0];
            SectionPointersAndCounts[0].Section = settingsSection.GetGeneric();
            int dialogueSectionPointerIndex = SectionPointersAndCounts.FindIndex(s => s.Pointer == Settings.DialogueSectionPointer);

            if (Name == "BGTESTS" || Regex.IsMatch(Name, @"[MCT][AHKNTR]\d{2}S") || Regex.IsMatch(Name, @"CHS_\w{3}_\d{2}S") || Regex.IsMatch(Name, @"E[VD]\d?_[HKMNT]?\d{2,3}S"))
            {
                if (Settings.UnknownSection01Pointer > 0)
                {
                    string name = "UNKNOWNSECTION01";

                    (int pointerSectionIndex, PointerSection pointerSection) = PointerSection.ParseSection(SectionPointersAndCounts, Settings.UnknownSection01Pointer, name, Data, log);
                    SectionPointersAndCounts[pointerSectionIndex].Section = pointerSection.GetGeneric();

                    int unknownSection01Index = SectionPointersAndCounts.FindIndex(s => s.Pointer == pointerSection.Objects[0].Pointer);
                    IntegerSection unknown01Section = new();
                    unknown01Section.Initialize(Data.Skip(SectionPointersAndCounts[unknownSection01Index].Pointer)
                        .Take(SectionPointersAndCounts[unknownSection01Index + 1].Pointer - SectionPointersAndCounts[unknownSection01Index].Pointer),
                        SectionPointersAndCounts[unknownSection01Index].ItemCount,
                        name, log, SectionPointersAndCounts[unknownSection01Index].Pointer);
                    SectionPointersAndCounts[unknownSection01Index].Section = unknown01Section.GetGeneric();
                }

                if (Settings.UnknownSection02Pointer > 0)
                {
                    string name = "UNKNOWNSECTION02";

                    (int pointerSectionIndex, PointerSection pointerSection) = PointerSection.ParseSection(SectionPointersAndCounts, Settings.UnknownSection02Pointer, name, Data, log);
                    SectionPointersAndCounts[pointerSectionIndex].Section = pointerSection.GetGeneric();

                    int unknownSection02Index = SectionPointersAndCounts.FindIndex(s => s.Pointer == pointerSection.Objects[0].Pointer);
                    Unknown02Section unknown02Section = new();
                    unknown02Section.Initialize(Data.Skip(SectionPointersAndCounts[unknownSection02Index].Pointer)
                        .Take(SectionPointersAndCounts[unknownSection02Index + 1].Pointer - SectionPointersAndCounts[unknownSection02Index].Pointer),
                        SectionPointersAndCounts[unknownSection02Index].ItemCount,
                        name, log, SectionPointersAndCounts[unknownSection02Index].Pointer);
                    SectionPointersAndCounts[unknownSection02Index].Section = unknown02Section.GetGeneric();
                }

                if (Settings.UnknownSection03Pointer > 0)
                {
                    string name = "UNKNOWNSECTION03";

                    (int pointerSectionIndex, PointerSection pointerSection) = PointerSection.ParseSection(SectionPointersAndCounts, Settings.UnknownSection03Pointer, name, Data, log);
                    SectionPointersAndCounts[pointerSectionIndex].Section = pointerSection.GetGeneric();

                    int unknownSection03Index = SectionPointersAndCounts.FindIndex(s => s.Pointer == pointerSection.Objects[0].Pointer);
                    Unknown03Section unknown03Section = new();
                    unknown03Section.Initialize(Data.Skip(SectionPointersAndCounts[unknownSection03Index].Pointer)
                        .Take(SectionPointersAndCounts[unknownSection03Index + 1].Pointer - SectionPointersAndCounts[unknownSection03Index].Pointer),
                        SectionPointersAndCounts[unknownSection03Index].ItemCount,
                        name, log, SectionPointersAndCounts[unknownSection03Index].Pointer);
                    SectionPointersAndCounts[unknownSection03Index].Section = unknown03Section.GetGeneric();
                }

                if (Settings.StartingChibisSectionPointer > 0)
                {
                    string name = "STARTINGCHIBIS";

                    (int pointerSectionIndex, PointerSection pointerSection) = PointerSection.ParseSection(SectionPointersAndCounts, Settings.StartingChibisSectionPointer, name, Data, log);
                    SectionPointersAndCounts[pointerSectionIndex].Section = pointerSection.GetGeneric();

                    int startingChibisSectionIndex = SectionPointersAndCounts.FindIndex(s => s.Pointer == pointerSection.Objects[0].Pointer);
                    StartingChibisSection = new();
                    StartingChibisSection.Initialize(Data.Skip(SectionPointersAndCounts[startingChibisSectionIndex].Pointer)
                        .Take(SectionPointersAndCounts[startingChibisSectionIndex + 1].Pointer - SectionPointersAndCounts[startingChibisSectionIndex].Pointer),
                        SectionPointersAndCounts[startingChibisSectionIndex].ItemCount,
                        name, log, SectionPointersAndCounts[startingChibisSectionIndex].Pointer);
                    SectionPointersAndCounts[startingChibisSectionIndex].Section = StartingChibisSection.GetGeneric();
                }

                if (Settings.MapCharactersSectionPointer > 0)
                {
                    string name = "MAPCHARACTERS";

                    (int pointerSectionIndex, PointerSection pointerSection) = PointerSection.ParseSection(SectionPointersAndCounts, Settings.MapCharactersSectionPointer, name, Data, log);
                    SectionPointersAndCounts[pointerSectionIndex].Section = pointerSection.GetGeneric();

                    int mapCharactersSectionIndex = SectionPointersAndCounts.FindIndex(s => s.Pointer == pointerSection.Objects[0].Pointer);
                    MapCharactersSection = new();
                    MapCharactersSection.Initialize(Data.Skip(SectionPointersAndCounts[mapCharactersSectionIndex].Pointer)
                        .Take(SectionPointersAndCounts[mapCharactersSectionIndex + 1].Pointer - SectionPointersAndCounts[mapCharactersSectionIndex].Pointer),
                        SectionPointersAndCounts[mapCharactersSectionIndex].ItemCount,
                        name, log, SectionPointersAndCounts[mapCharactersSectionIndex].Pointer);
                    SectionPointersAndCounts[mapCharactersSectionIndex].Section = MapCharactersSection.GetGeneric();
                }

                if (Settings.UnknownSection06Pointer > 0)
                {
                    string name = "UNKNOWNSECTION06";

                    (int pointerSectionIndex, PointerSection pointerSection) = PointerSection.ParseSection(SectionPointersAndCounts, Settings.UnknownSection06Pointer, name, Data, log);
                    SectionPointersAndCounts[pointerSectionIndex].Section = pointerSection.GetGeneric();

                    int unknownSection06Index = SectionPointersAndCounts.FindIndex(s => s.Pointer == pointerSection.Objects[0].Pointer);
                    IntegerSection unknown06Section = new();
                    unknown06Section.Initialize(Data.Skip(SectionPointersAndCounts[unknownSection06Index].Pointer)
                        .Take(SectionPointersAndCounts[unknownSection06Index + 1].Pointer - SectionPointersAndCounts[unknownSection06Index].Pointer),
                        SectionPointersAndCounts[unknownSection06Index].ItemCount,
                        name, log, SectionPointersAndCounts[unknownSection06Index].Pointer);
                    SectionPointersAndCounts[unknownSection06Index].Section = unknown06Section.GetGeneric();
                }

                if (Settings.UnknownSection07Pointer > 0)
                {
                    string name = "UNKNOWNSECTION07";

                    (int pointerSectionIndex, PointerSection pointerSection) = PointerSection.ParseSection(SectionPointersAndCounts, Settings.UnknownSection07Pointer, name, Data, log);
                    SectionPointersAndCounts[pointerSectionIndex].Section = pointerSection.GetGeneric();

                    int unknownSection07Index = SectionPointersAndCounts.FindIndex(s => s.Pointer == pointerSection.Objects[0].Pointer);
                    Unknown07Section unknown07Section = new();
                    unknown07Section.Initialize(Data.Skip(SectionPointersAndCounts[unknownSection07Index].Pointer)
                        .Take(SectionPointersAndCounts[unknownSection07Index + 1].Pointer - SectionPointersAndCounts[unknownSection07Index].Pointer),
                        SectionPointersAndCounts[unknownSection07Index].ItemCount,
                        name, log, SectionPointersAndCounts[unknownSection07Index].Pointer);
                    SectionPointersAndCounts[unknownSection07Index].Section = unknown07Section.GetGeneric();
                }

                int choicesSectionIndex = SectionPointersAndCounts.FindIndex(s => s.Pointer == Settings.ChoicesSectionPointer);
                if (Settings.ChoicesSectionPointer > 0)
                {
                    string name = "CHOICES";

                    ChoicesSection = new();
                    ChoicesSection.Initialize(Data.Skip(SectionPointersAndCounts[choicesSectionIndex].Pointer)
                        .Take(SectionPointersAndCounts[choicesSectionIndex + 1].Pointer - SectionPointersAndCounts[choicesSectionIndex].Pointer),
                        SectionPointersAndCounts[choicesSectionIndex].ItemCount,
                        name, log, SectionPointersAndCounts[choicesSectionIndex].Pointer);
                    SectionPointersAndCounts[choicesSectionIndex].Section = ChoicesSection.GetGeneric();
                }

                int unknownSection09Index = SectionPointersAndCounts.FindIndex(s => s.Pointer == Settings.UnknownSection09Pointer);
                if (unknownSection09Index > choicesSectionIndex + 1)
                {
                    string name = "UNKNOWNSECTION08";

                    (int pointerSectionIndex, PointerSection pointerSection) = PointerSection.ParseSection(SectionPointersAndCounts, SectionPointersAndCounts[choicesSectionIndex + 2].Pointer, name, Data, log);
                    SectionPointersAndCounts[pointerSectionIndex].Section = pointerSection.GetGeneric();

                    Unknown08Section unknown08Section = new();
                    unknown08Section.Initialize(Data.Skip(SectionPointersAndCounts[choicesSectionIndex + 1].Pointer)
                        .Take(SectionPointersAndCounts[choicesSectionIndex + 2].Pointer - SectionPointersAndCounts[choicesSectionIndex + 1].Pointer),
                        SectionPointersAndCounts[choicesSectionIndex + 1].ItemCount,
                        name, log, SectionPointersAndCounts[choicesSectionIndex + 1].Pointer);
                    SectionPointersAndCounts[choicesSectionIndex + 1].Section = unknown08Section.GetGeneric();
                }

                if (Settings.UnknownSection09Pointer > 0)
                {
                    string name = "UNKNOWNSECTION09";

                    Unknown09Section unknown09Section = new();
                    unknown09Section.Initialize(Data.Skip(SectionPointersAndCounts[unknownSection09Index].Pointer)
                        .Take(SectionPointersAndCounts[unknownSection09Index + 1].Pointer - SectionPointersAndCounts[unknownSection09Index].Pointer),
                        SectionPointersAndCounts[unknownSection09Index].ItemCount,
                        name, log, SectionPointersAndCounts[unknownSection09Index].Pointer);
                    SectionPointersAndCounts[unknownSection09Index].Section = unknown09Section.GetGeneric();
                }

                if (Settings.UnknownSection10Pointer > 0)
                {
                    string name = "UNKNOWNSECTION10";

                    int unknownSection10Index = SectionPointersAndCounts.FindIndex(s => s.Pointer == Settings.UnknownSection10Pointer);
                    Unknown10Section unknown10Section = new();
                    unknown10Section.Initialize(Data.Skip(SectionPointersAndCounts[unknownSection10Index].Pointer)
                        .Take(SectionPointersAndCounts[unknownSection10Index + 1].Pointer - SectionPointersAndCounts[unknownSection10Index].Pointer),
                        SectionPointersAndCounts[unknownSection10Index].ItemCount,
                        name, log, SectionPointersAndCounts[unknownSection10Index].Pointer);
                    SectionPointersAndCounts[unknownSection10Index].Section = unknown10Section.GetGeneric();
                }

                int labelsSectionPointerIndex = SectionPointersAndCounts.FindIndex(s => s.Pointer == Settings.LabelsSectionPointer);
                LabelsSection = new();
                if (Settings.LabelsSectionPointer > 0)
                {
                    string name = "LABELS";

                    LabelsSection.Initialize(Data.Skip(SectionPointersAndCounts[labelsSectionPointerIndex].Pointer)
                        .Take(SectionPointersAndCounts[labelsSectionPointerIndex + 1].Pointer - SectionPointersAndCounts[labelsSectionPointerIndex].Pointer),
                        SectionPointersAndCounts[labelsSectionPointerIndex].ItemCount,
                        name, log, SectionPointersAndCounts[labelsSectionPointerIndex].Pointer);
                    SectionPointersAndCounts[labelsSectionPointerIndex].Section = LabelsSection.GetGeneric();
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
                            name, log, SectionPointersAndCounts[i].Pointer);
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
                        name, log, SectionPointersAndCounts[dialogueSectionPointerIndex].Pointer);
                    dialogueSection.InitializeDramatisPersonaeIndices(dramatisPersonae);
                    SectionPointersAndCounts[dialogueSectionPointerIndex].Section = dialogueSection.GetGeneric();
                }

                int conditionalSectionIndex = SectionPointersAndCounts.FindIndex(s => s.Pointer == Settings.ConditionalsSectionPointer);
                if (Settings.ConditionalsSectionPointer > 0)
                {
                    string name = "CONDITIONALS";

                    ConditionalsSection = new();
                    ConditionalsSection.Initialize(Data,
                        SectionPointersAndCounts[conditionalSectionIndex].ItemCount,
                        name, log, SectionPointersAndCounts[conditionalSectionIndex].Pointer);
                    SectionPointersAndCounts[conditionalSectionIndex].Section = ConditionalsSection.GetGeneric();
                }

                int scriptSectionDefinitionsSectionIndex = SectionPointersAndCounts.FindIndex(s => s.Pointer == Settings.ScriptSectionDefinitionsSectionPointer);
                ScriptSectionDefinitionsSection scriptSectionDefinitionsSection = new() { Labels = LabelsSection.Objects.Select(l => l.Name.Replace("/", "")).ToList() };
                if (Settings.ScriptSectionDefinitionsSectionPointer > 0)
                {
                    string name = "SCRIPTDEFINITIONS";

                    scriptSectionDefinitionsSection.Initialize(Data
                        .Skip(SectionPointersAndCounts[scriptSectionDefinitionsSectionIndex].Pointer),
                        Settings.NumScriptSections,
                        name, log,
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
                            string.IsNullOrEmpty(LabelsSection.Objects[i].Name) ? $"SCRIPT{i:D2}" : LabelsSection.Objects[i].Name.Replace("/", ""),
                            log, scriptSectionDefinitionsSection.Objects[i].Pointer);
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
                        SectionPointersAndCounts[eventNameSectionIndex].ItemCount, name, log, SectionPointersAndCounts[eventNameSectionIndex].Pointer);
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
            Scenario = new(Data, DialogueLines, SectionPointersAndCounts);
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
                    _log.LogWarning($"File {Index} has {type} too long ({dialogueIndex}) (starting with: {dialogueText[0..Math.Min(15, dialogueText.Length - 1)]})");
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
                InitializeScenarioFile();
                return Scenario.GetSource(includes, _log);
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
                StringBuilder sb = new();
                sb.AppendLine(".include \"COMMANDS.INC\"");
                sb.AppendLine();
                sb.AppendLine($".word {NumSections}");
                sb.AppendLine(".word END_POINTERS");
                sb.AppendLine(".word FILE_START");

                foreach (EventFileSection section in SectionPointersAndCounts)
                {
                    sb.AppendLine($".word {section.Section?.Name ?? "UNRECOGNIZED_SECTION"}");
                    sb.AppendLine($".word {section.ItemCount}");
                }

                sb.AppendLine();
                sb.AppendLine("FILE_START:");

                int currentPointer = 0;
                foreach (EventFileSection section in SectionPointersAndCounts.OrderBy(s => s.Pointer))
                {
                    if (section.Section is not null)
                    {
                        dynamic typedSection = Convert.ChangeType(section.Section, section.Section.SectionType);
                        sb.AppendLine(typedSection.GetAsm(0, ref currentPointer));
                    }
                    else
                    {
                        sb.AppendLine();
                    }
                }

                sb.AppendLine("END_POINTERS:");
                sb.AppendLine($"   .word {currentPointer}");
                for (int i = 0; i < currentPointer; i++)
                {
                    sb.AppendLine($"   .word POINTER{i}");
                }

                return sb.ToString();
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
        public int NumStartingChibisSections { get; set; }
        public int StartingChibisSectionPointer { get; set; } // array indices of some kind
        public int NumMapCharacterSections { get; set; }
        public int MapCharactersSectionPointer { get; set; } // flag setting (investigation-related)
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
        public int NumConditionals { get; set; }
        public int ConditionalsSectionPointer { get; set; }
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
            NumStartingChibisSections = BitConverter.ToInt32(data.Skip(0x01C).Take(4).ToArray());
            StartingChibisSectionPointer = BitConverter.ToInt32(data.Skip(0x020).Take(4).ToArray());
            NumMapCharacterSections = BitConverter.ToInt32(data.Skip(0x024).Take(4).ToArray());
            MapCharactersSectionPointer = BitConverter.ToInt32(data.Skip(0x028).Take(4).ToArray());
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
            NumConditionals = BitConverter.ToInt32(data.Skip(0x06C).Take(4).ToArray());
            ConditionalsSectionPointer = BitConverter.ToInt32(data.Skip(0x070).Take(4).ToArray());
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

        public DialogueLine(Speaker speaker, string speakerName, int speakerPointer, int pointer, byte[] file)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Speaker = speaker;
            SpeakerName = speakerName;
            SpeakerPointer = speakerPointer;
            Pointer = pointer;
            if (pointer > 0)
            {
                Data = file.Skip(pointer).TakeWhile(b => b != 0x00).ToArray();
            }
            else
            {
                Data = Array.Empty<byte>();
            }
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
