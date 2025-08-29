﻿using HaruhiChokuretsuLib.Font;
using HaruhiChokuretsuLib.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Resources.NetStandard;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using HunspellSharp;
using LiteDB;

namespace HaruhiChokuretsuLib.Archive.Event;

/// <summary>
/// Representation of all the files contained within evt.bin (including special files)
/// Note: this class is frankly a mess and needs refactoring. I'm sorry in advance if you're trying to use it.
/// </summary>
public partial class EventFile : FileInArchive, ISourceFile
{
    private static readonly string[] s_specialFileNames = new[] { "CHESSS", "EVTTBLS", "SCENARIOS", "TOPICS", "TUTORIALS", "VOICEMAPS" };

    /// <summary>
    /// The number of sections in the event file
    /// </summary>
    public int NumSections { get; set; }

    /// <summary>
    /// Definition of an event file section as seen in data file headers
    /// </summary>
    /// <param name="Pointer">The pointer to the section</param>
    /// <param name="ItemCount">The number of items in the section</param>
    public record SectionDef(int Pointer, int ItemCount)
    {
        /// <summary>
        /// The pointer to the section
        /// </summary>
        public int Pointer { get; set; } = Pointer;
    }
    internal readonly List<SectionDef> SectionDefs = [];

    private int _pointerToEndPointerSection;
    internal List<int> EndPointers { get; set; } = [];
    internal List<int> EndPointerPointers { get; set; } = [];
    /// <summary>
    /// The internal title of the event file (if defined, same as name in archive)
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// The event file settings object
    /// </summary>
    public EventFileSettings Settings { get; set; }
    /// <summary>
    /// A list of "dramatis personae" (characters who appear in the event file)
    /// </summary>
    public Dictionary<int, string> DramatisPersonae { get; set; } = [];
    /// <summary>
    /// A list of dialogue lines which are used in this file
    /// </summary>
    public List<DialogueLine> DialogueLines { get; set; } = [];

    /// <summary>
    /// The section containing the name of the event file
    /// </summary>
    public EventNameSection EventNameSection { get; set; }
    /// <summary>
    /// Unknown
    /// </summary>
    public IntegerSection UnknownSection01 { get; set; }
    /// <summary>
    /// The section containing data on interactable objects (if the event has them)
    /// </summary>
    public InteractableObjectsSection InteractableObjectsSection { get; set; }
    /// <summary>
    /// Unknown
    /// </summary>
    public Unknown03Section UnknownSection03 { get; set; }
    /// <summary>
    /// The section containing the list of chibis that appear on the top screen at the start of the event
    /// </summary>
    public StartingChibisSection StartingChibisSection { get; set; }
    /// <summary>
    /// The section containing definitions of which characters appear on the map and where for investigation phase events
    /// </summary>
    public MapCharactersSection MapCharactersSection { get; set; }
    /// <summary>
    /// Unknown
    /// </summary>
    public IntegerSection UnknownSection06 { get; set; }
    /// <summary>
    /// Unknown
    /// </summary>
    public Unknown07Section UnknownSection07 { get; set; }
    /// <summary>
    /// The section containing the choices used by the SELECT command
    /// </summary>
    public ChoicesSection ChoicesSection { get; set; }
    /// <summary>
    /// Unknown
    /// </summary>
    public Unknown08Section UnknownSection08 { get; set; }
    /// <summary>
    /// Unknown
    /// </summary>
    public Unknown09Section UnknownSection09 { get; set; }
    /// <summary>
    /// Unknown
    /// </summary>
    public Unknown10Section UnknownSection10 { get; set; }
    /// <summary>
    /// The section which contains the labels for the script sections
    /// </summary>
    public LabelsSection LabelsSection { get; set; }
    /// <summary>
    /// The set of sections containing the dramatis personae (one section per character)
    /// </summary>
    public List<DramatisPersonaeSection> DramatisPersonaeSections { get; set; } = [];
    /// <summary>
    /// The section containing the dialogue lines
    /// </summary>
    public DialogueSection DialogueSection { get; set; }
    /// <summary>
    /// The section containing any conditionals used in the scripts
    /// </summary>
    public ConditionalSection ConditionalsSection { get; set; }
    /// <summary>
    /// The set of sections which contain the script commands
    /// </summary>
    public List<ScriptSection> ScriptSections { get; private set; } = [];
    /// <summary>
    /// The section defining settings for the event
    /// </summary>
    public SettingsSection SettingsSection { get; set; }

    /// <summary>
    /// A list defining the commands that can be used in scripts
    /// </summary>
    public static List<ScriptCommand> CommandsAvailable { get; set; } =
    [
        new(0x00, nameof(CommandVerb.INIT_READ_FLAG), []),
        new(0x01, nameof(CommandVerb.DIALOGUE),
        [
            "dialogueIndex", "spriteIndex", "spriteEntranceTransition", "spriteExitOrInternalTransition",
            "spriteShake", "voiceIndex", "textVoiceFont", "textSpeed", "textEntranceEffect", "spriteLayer",
            "dontClearText", "noLipFlap"
        ]),
        new(0x02, nameof(CommandVerb.KBG_DISP), ["kbgIndex"]),
        new(0x03, nameof(CommandVerb.PIN_MNL), ["dialogueIndex"]),
        new(0x04, nameof(CommandVerb.BG_DISP), ["bgIndex"]),
        new(0x05, nameof(CommandVerb.SCREEN_FADEIN), ["timeToFade", "unused", "fadeLocation", "fadeColor"]),
        new(0x06, nameof(CommandVerb.SCREEN_FADEOUT),
        [
            "timeToFade", "unknown1", "fadeColorRed", "fadeColorGreen", "fadeColorBlue", "fadeLocation", "unknown6"
        ]),
        new(0x07, nameof(CommandVerb.SCREEN_FLASH),
            ["fadeInTime", "holdTime", "fadeOutTime", "flashColorRed", "flashColorGreen", "flashColorBlue"]),
        new(0x08, nameof(CommandVerb.SND_PLAY), ["soundIndex", "mode", "volume", "crossfadeDupe", "crossfadeTime"]),
        new(0x09, nameof(CommandVerb.REMOVED), []),
        new(0x0A, nameof(CommandVerb.SND_STOP), []),
        new(0x0B, nameof(CommandVerb.BGM_PLAY), ["bgmIndex", "mode", "volume", "fadeInTime", "fadeOutTime"]),
        new(0x0C, nameof(CommandVerb.VCE_PLAY), ["vceIndex"]),
        new(0x0D, nameof(CommandVerb.FLAG), ["flag", "set"]),
        new(0x0E, nameof(CommandVerb.TOPIC_GET), ["topicId"]),
        new(0x0F, nameof(CommandVerb.TOGGLE_DIALOGUE), ["show"]),
        new(0x10, nameof(CommandVerb.SELECT),
            ["option1", "option2", "option3", "option4", "flag1", "flag2", "flag3", "flag4"]),
        new(0x11, nameof(CommandVerb.SCREEN_SHAKE), ["duration", "horizontalIntensity", "verticalIntensity"]),
        new(0x12, nameof(CommandVerb.SCREEN_SHAKE_STOP), []),
        new(0x13, nameof(CommandVerb.GOTO), ["blockId"]),
        new(0x14, nameof(CommandVerb.SCENE_GOTO), ["conditionalIndex"]),
        new(0x15, nameof(CommandVerb.WAIT), ["frames"]),
        new(0x16, nameof(CommandVerb.HOLD), []),
        new(0x17, nameof(CommandVerb.NOOP1), []),
        new(0x18, nameof(CommandVerb.VGOTO), ["conditionalIndex", "unused", "gotoId"]),
        new(0x19, nameof(CommandVerb.HARUHI_METER), ["unused", "addValue", "setValue"]),
        new(0x1A, nameof(CommandVerb.HARUHI_METER_NOSHOW), ["addValue"]),
        new(0x1B, nameof(CommandVerb.PALEFFECT), ["paletteMode", "transitionTime", "unknownBool"]),
        new(0x1C, nameof(CommandVerb.BG_FADE), ["bgIndex", "bgIndexSuper", "fadeTime"]),
        new(0x1D, nameof(CommandVerb.TRANS_OUT), ["index"]),
        new(0x1E, nameof(CommandVerb.TRANS_IN), ["index"]),
        new(0x1F, nameof(CommandVerb.SET_PLACE), ["display", "placeIndex"]),
        new(0x20, nameof(CommandVerb.ITEM_DISPIMG), ["itemIndex", "x", "y"]),
        new(0x21, nameof(CommandVerb.BACK), []),
        new(0x22, nameof(CommandVerb.STOP), []),
        new(0x23, nameof(CommandVerb.NOOP2), []),
        new(0x24, nameof(CommandVerb.LOAD_ISOMAP), ["mapFileIndex"]),
        new(0x25, nameof(CommandVerb.INVEST_START),
            ["unknown00", "unknown01", "unknown02", "unknown03", "endScriptBlock"]),
        new(0x26, nameof(CommandVerb.INVEST_END), []),
        new(0x27, nameof(CommandVerb.CHIBI_EMOTE), ["chibiIndex", "emoteIndex"]),
        new(0x28, nameof(CommandVerb.NEXT_SCENE), []),
        new(0x29, nameof(CommandVerb.SKIP_SCENE), ["scenesToSkip"]),
        new(0x2A, nameof(CommandVerb.MODIFY_FRIENDSHIP), ["flIndex", "value"]),
        new(0x2B, nameof(CommandVerb.CHIBI_ENTEREXIT), ["chibiIndex", "mode", "delay"]),
        new(0x2C, nameof(CommandVerb.AVOID_DISP), []),
        new(0x2D, nameof(CommandVerb.GLOBAL2D), ["value"]),
        new(0x2E, nameof(CommandVerb.CHESS_LOAD), ["chessFileIndex"]),
        new(0x2F, nameof(CommandVerb.CHESS_VGOTO), ["clearBlock", "missBlock", "miss2Block"]),
        new(0x30, nameof(CommandVerb.CHESS_MOVE),
            ["whiteSpaceBegin", "whiteSpaceEnd", "blackSpaceBegin", "blackSpaceEnd"]),
        new(0x31, nameof(CommandVerb.CHESS_TOGGLE_GUIDE), ["piece1", "piece2", "piece3", "piece4"]),
        new(0x32, nameof(CommandVerb.CHESS_TOGGLE_HIGHLIGHT),
        [
            "space1", "space2", "space3", "space4", "space5", "space6", "space7", "space8", "space9", "space10",
            "space11", "space12", "space13", "space14", "space15", "space16"
        ]),
        new(0x33, nameof(CommandVerb.CHESS_TOGGLE_CROSS),
        [
            "space1", "space2", "space3", "space4", "space5", "space6", "space7", "space8", "space9", "space10",
            "space11", "space12", "space13", "space14", "space15", "space16"
        ]),
        new(0x34, nameof(CommandVerb.CHESS_CLEAR_ANNOTATIONS), []),
        new(0x35, nameof(CommandVerb.CHESS_RESET), []),
        new(0x36, nameof(CommandVerb.SCENE_GOTO_CHESS), ["conditionalIndex"]),
        new(0x37, nameof(CommandVerb.EPHEADER), ["headerIndex"]),
        new(0x38, nameof(CommandVerb.NOOP3), []),
        new(0x39, nameof(CommandVerb.CONFETTI), ["on"]),
        new(0x3A, nameof(CommandVerb.BG_DISPCG), ["bgIndex", "displayBottom"]),
        new(0x3B, nameof(CommandVerb.BG_SCROLL), ["scrollDirection", "scrollSpeed"]),
        new(0x3C, nameof(CommandVerb.OP_MODE), []),
        new(0x3D, nameof(CommandVerb.WAIT_CANCEL), ["frames"]),
        new(0x3E, nameof(CommandVerb.BG_REVERT), []),
        new(0x3F, nameof(CommandVerb.BG_DISP2), ["bgIndex"]),
    ];

    // ReSharper disable InconsistentNaming
    /// <summary>
    /// An enum defining the names of the commands
    /// </summary>
    public enum CommandVerb
    {
        /// <summary>
        /// https://github.com/haroohie-club/ChokuretsuTranslationUtility/wiki/Event-File-Commands#init_read_flag-0x00
        /// </summary>
        INIT_READ_FLAG,
        /// <summary>
        /// https://github.com/haroohie-club/ChokuretsuTranslationUtility/wiki/Event-File-Commands#dialogue-0x01
        /// </summary>
        DIALOGUE,
        /// <summary>
        /// https://github.com/haroohie-club/ChokuretsuTranslationUtility/wiki/Event-File-Commands#kbg_disp-0x02
        /// </summary>
        KBG_DISP,
        /// <summary>
        /// https://github.com/haroohie-club/ChokuretsuTranslationUtility/wiki/Event-File-Commands#pin_mnl-0x03
        /// </summary>
        PIN_MNL,
        /// <summary>
        /// https://github.com/haroohie-club/ChokuretsuTranslationUtility/wiki/Event-File-Commands#bg_disp-0x04
        /// </summary>
        BG_DISP,
        /// <summary>
        /// https://github.com/haroohie-club/ChokuretsuTranslationUtility/wiki/Event-File-Commands#screen_fadein-0x05
        /// </summary>
        SCREEN_FADEIN,
        /// <summary>
        /// https://github.com/haroohie-club/ChokuretsuTranslationUtility/wiki/Event-File-Commands#screen_fadeout-0x06
        /// </summary>
        SCREEN_FADEOUT,
        /// <summary>
        /// https://github.com/haroohie-club/ChokuretsuTranslationUtility/wiki/Event-File-Commands#screen_flash-0x07
        /// </summary>
        SCREEN_FLASH,
        /// <summary>
        /// https://github.com/haroohie-club/ChokuretsuTranslationUtility/wiki/Event-File-Commands#snd_play-0x08
        /// </summary>
        SND_PLAY,
        /// <summary>
        /// https://github.com/haroohie-club/ChokuretsuTranslationUtility/wiki/Event-File-Commands#removed-0x09
        /// </summary>
        REMOVED,
        /// <summary>
        /// https://github.com/haroohie-club/ChokuretsuTranslationUtility/wiki/Event-File-Commands#unknown0a-0x0a
        /// </summary>
        SND_STOP,
        /// <summary>
        /// https://github.com/haroohie-club/ChokuretsuTranslationUtility/wiki/Event-File-Commands#bgm_play-0x0b
        /// </summary>
        BGM_PLAY,
        /// <summary>
        /// https://github.com/haroohie-club/ChokuretsuTranslationUtility/wiki/Event-File-Commands#vce_play-0x0c
        /// </summary>
        VCE_PLAY,
        /// <summary>
        /// https://github.com/haroohie-club/ChokuretsuTranslationUtility/wiki/Event-File-Commands#flag-0x0d
        /// </summary>
        FLAG,
        /// <summary>
        /// https://github.com/haroohie-club/ChokuretsuTranslationUtility/wiki/Event-File-Commands#topic_get-0x0e
        /// </summary>
        TOPIC_GET,
        /// <summary>
        /// https://github.com/haroohie-club/ChokuretsuTranslationUtility/wiki/Event-File-Commands#toggle_dialogue-0x0f
        /// </summary>
        TOGGLE_DIALOGUE,
        /// <summary>
        /// https://github.com/haroohie-club/ChokuretsuTranslationUtility/wiki/Event-File-Commands#select-0x10
        /// </summary>
        SELECT,
        /// <summary>
        /// https://github.com/haroohie-club/ChokuretsuTranslationUtility/wiki/Event-File-Commands#screen_shake-0x11
        /// </summary>
        SCREEN_SHAKE,
        /// <summary>
        /// https://github.com/haroohie-club/ChokuretsuTranslationUtility/wiki/Event-File-Commands#screen_shake_stop-0x12
        /// </summary>
        SCREEN_SHAKE_STOP,
        /// <summary>
        /// https://github.com/haroohie-club/ChokuretsuTranslationUtility/wiki/Event-File-Commands#goto-0x13
        /// </summary>
        GOTO,
        /// <summary>
        /// https://github.com/haroohie-club/ChokuretsuTranslationUtility/wiki/Event-File-Commands#scene_goto-0x14
        /// </summary>
        SCENE_GOTO,
        /// <summary>
        /// https://github.com/haroohie-club/ChokuretsuTranslationUtility/wiki/Event-File-Commands#wait-0x15
        /// </summary>
        WAIT,
        /// <summary>
        /// https://github.com/haroohie-club/ChokuretsuTranslationUtility/wiki/Event-File-Commands#hold-0x16
        /// </summary>
        HOLD,
        /// <summary>
        /// https://github.com/haroohie-club/ChokuretsuTranslationUtility/wiki/Event-File-Commands#noop1-0x17
        /// </summary>
        NOOP1,
        /// <summary>
        /// https://github.com/haroohie-club/ChokuretsuTranslationUtility/wiki/Event-File-Commands#vgoto-0x18
        /// </summary>
        VGOTO,
        /// <summary>
        /// https://github.com/haroohie-club/ChokuretsuTranslationUtility/wiki/Event-File-Commands#haruhi_meter-0x19
        /// </summary>
        HARUHI_METER,
        /// <summary>
        /// https://github.com/haroohie-club/ChokuretsuTranslationUtility/wiki/Event-File-Commands#haruhi_meter_noshow-0x1a
        /// </summary>
        HARUHI_METER_NOSHOW,
        /// <summary>
        /// https://github.com/haroohie-club/ChokuretsuTranslationUtility/wiki/Event-File-Commands#paleffect-0x1b
        /// </summary>
        PALEFFECT,
        /// <summary>
        /// https://github.com/haroohie-club/ChokuretsuTranslationUtility/wiki/Event-File-Commands#bg_fade-0x1c
        /// </summary>
        BG_FADE,
        /// <summary>
        /// https://github.com/haroohie-club/ChokuretsuTranslationUtility/wiki/Event-File-Commands#trans_out-0x1d
        /// </summary>
        TRANS_OUT,
        /// <summary>
        /// https://github.com/haroohie-club/ChokuretsuTranslationUtility/wiki/Event-File-Commands#trans_in-0x1e
        /// </summary>
        TRANS_IN,
        /// <summary>
        /// https://github.com/haroohie-club/ChokuretsuTranslationUtility/wiki/Event-File-Commands#set_place-0x1f
        /// </summary>
        SET_PLACE,
        /// <summary>
        /// https://github.com/haroohie-club/ChokuretsuTranslationUtility/wiki/Event-File-Commands#item_dispimg-0x20
        /// </summary>
        ITEM_DISPIMG,
        /// <summary>
        /// https://github.com/haroohie-club/ChokuretsuTranslationUtility/wiki/Event-File-Commands#back-0x21
        /// </summary>
        BACK,
        /// <summary>
        /// https://github.com/haroohie-club/ChokuretsuTranslationUtility/wiki/Event-File-Commands#stop-0x22
        /// </summary>
        STOP,
        /// <summary>
        /// https://github.com/haroohie-club/ChokuretsuTranslationUtility/wiki/Event-File-Commands#noop2-0x23
        /// </summary>
        NOOP2,
        /// <summary>
        /// https://github.com/haroohie-club/ChokuretsuTranslationUtility/wiki/Event-File-Commands#load_isomap-0x24
        /// </summary>
        LOAD_ISOMAP,
        /// <summary>
        /// https://github.com/haroohie-club/ChokuretsuTranslationUtility/wiki/Event-File-Commands#invest_start-0x25
        /// </summary>
        INVEST_START,
        /// <summary>
        /// https://github.com/haroohie-club/ChokuretsuTranslationUtility/wiki/Event-File-Commands#invest_end-0x26
        /// </summary>
        INVEST_END,
        /// <summary>
        /// https://github.com/haroohie-club/ChokuretsuTranslationUtility/wiki/Event-File-Commands#chibi_emote-0x27
        /// </summary>
        CHIBI_EMOTE,
        /// <summary>
        /// https://github.com/haroohie-club/ChokuretsuTranslationUtility/wiki/Event-File-Commands#next_scene-0x28
        /// </summary>
        NEXT_SCENE,
        /// <summary>
        /// https://github.com/haroohie-club/ChokuretsuTranslationUtility/wiki/Event-File-Commands#skip_scene-0x29
        /// </summary>
        SKIP_SCENE,
        /// <summary>
        /// https://github.com/haroohie-club/ChokuretsuTranslationUtility/wiki/Event-File-Commands#modify_friendship-0x2a
        /// </summary>
        MODIFY_FRIENDSHIP,
        /// <summary>
        /// https://github.com/haroohie-club/ChokuretsuTranslationUtility/wiki/Event-File-Commands#chibi_enterexit-0x2b
        /// </summary>
        CHIBI_ENTEREXIT,
        /// <summary>
        /// https://github.com/haroohie-club/ChokuretsuTranslationUtility/wiki/Event-File-Commands#avoid_disp-0x2c
        /// </summary>
        AVOID_DISP,
        /// <summary>
        /// https://github.com/haroohie-club/ChokuretsuTranslationUtility/wiki/Event-File-Commands#global2d-0x2d
        /// </summary>
        GLOBAL2D,
        /// <summary>
        /// https://github.com/haroohie-club/ChokuretsuTranslationUtility/wiki/Event-File-Commands#chess_load-0x2e
        /// </summary>
        CHESS_LOAD,
        /// <summary>
        /// https://github.com/haroohie-club/ChokuretsuTranslationUtility/wiki/Event-File-Commands#chess_vgoto-0x2f
        /// </summary>
        CHESS_VGOTO,
        /// <summary>
        /// https://github.com/haroohie-club/ChokuretsuTranslationUtility/wiki/Event-File-Commands#chess_move-0x30
        /// </summary>
        CHESS_MOVE,
        /// <summary>
        /// https://github.com/haroohie-club/ChokuretsuTranslationUtility/wiki/Event-File-Commands#chess_toggle_guide-0x31
        /// </summary>
        CHESS_TOGGLE_GUIDE,
        /// <summary>
        /// https://github.com/haroohie-club/ChokuretsuTranslationUtility/wiki/Event-File-Commands#chess_toggle_highlight-0x32
        /// </summary>
        CHESS_TOGGLE_HIGHLIGHT,
        /// <summary>
        /// https://github.com/haroohie-club/ChokuretsuTranslationUtility/wiki/Event-File-Commands#chess_toggle_cross-0x33
        /// </summary>
        CHESS_TOGGLE_CROSS,
        /// <summary>
        /// https://github.com/haroohie-club/ChokuretsuTranslationUtility/wiki/Event-File-Commands#chess_clear_annotations-0x34
        /// </summary>
        CHESS_CLEAR_ANNOTATIONS,
        /// <summary>
        /// https://github.com/haroohie-club/ChokuretsuTranslationUtility/wiki/Event-File-Commands#chess_reset-0x35
        /// </summary>
        CHESS_RESET,
        /// <summary>
        /// https://github.com/haroohie-club/ChokuretsuTranslationUtility/wiki/Event-File-Commands#scene_goto2-0x36
        /// </summary>
        SCENE_GOTO_CHESS,
        /// <summary>
        /// https://github.com/haroohie-club/ChokuretsuTranslationUtility/wiki/Event-File-Commands#epheader-0x37
        /// </summary>
        EPHEADER,
        /// <summary>
        /// https://github.com/haroohie-club/ChokuretsuTranslationUtility/wiki/Event-File-Commands#noop3-0x38
        /// </summary>
        NOOP3,
        /// <summary>
        /// https://github.com/haroohie-club/ChokuretsuTranslationUtility/wiki/Event-File-Commands#confetti-0x39
        /// </summary>
        CONFETTI,
        /// <summary>
        /// https://github.com/haroohie-club/ChokuretsuTranslationUtility/wiki/Event-File-Commands#bg_dispcg-0x3a
        /// </summary>
        BG_DISPCG,
        /// <summary>
        /// https://github.com/haroohie-club/ChokuretsuTranslationUtility/wiki/Event-File-Commands#bg_scroll-0x3b
        /// </summary>
        BG_SCROLL,
        /// <summary>
        /// https://github.com/haroohie-club/ChokuretsuTranslationUtility/wiki/Event-File-Commands#op_mode-0x3c
        /// </summary>
        OP_MODE,
        /// <summary>
        /// https://github.com/haroohie-club/ChokuretsuTranslationUtility/wiki/Event-File-Commands#wait_cancel-0x3d
        /// </summary>
        WAIT_CANCEL,
        /// <summary>
        /// https://github.com/haroohie-club/ChokuretsuTranslationUtility/wiki/Event-File-Commands#bg_revert-0x3e
        /// </summary>
        BG_REVERT,
        /// <summary>
        /// https://github.com/haroohie-club/ChokuretsuTranslationUtility/wiki/Event-File-Commands#bg_disp2-0x3f
        /// </summary>
        BG_DISP2,
    }
    // ReSharper restore InconsistentNaming

    /// <summary>
    /// Creates an empty event file
    /// </summary>
    public EventFile()
    {
    }

    /// <summary>
    /// The font replacement map to be used during dialogue replacement (this is specific to the translated ROMs)
    /// </summary>
    [JsonIgnore]
    [BsonIgnore]
    public FontReplacementDictionary FontReplacementMap { get; set; } = [];

    private const int DIALOGUE_LINE_LENGTH = 240;

    /// <inheritdoc/>
    public override void Initialize(byte[] decompressedData, int offset, ILogger log)
    {
        Log = log;
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Offset = offset;
        Data = [.. decompressedData];

        NumSections = BitConverter.ToInt32(decompressedData.Take(4).ToArray());
        for (int i = 0; i < NumSections; i++)
        {
            SectionDefs.Add(new(IO.ReadInt(decompressedData, 0x0C + 0x08 * i), IO.ReadInt(decompressedData, 0x10 + 0x08 * i)));
        }

        if (!s_specialFileNames.Contains(Name) && !Generic)
        {
            SettingsSection = new();
            SettingsSection.Initialize(decompressedData[SectionDefs[0].Pointer..(SectionDefs[0].Pointer + EventFileSettings.SETTINGS_LENGTH)], 1, "SETTINGS", log, SectionDefs[0].Pointer);
            Settings = SettingsSection.Objects[0];
            int dialogueSectionPointerIndex = SectionDefs.FindIndex(s => s.Pointer == Settings.DialogueSectionPointer);

            if (Settings.UnknownSection01Pointer > 0)
            {
                string name = "UNKNOWNSECTION01";

                (int _, PointerSection pointerSection) = PointerSection.ParseSection(SectionDefs, Settings.UnknownSection01Pointer, name, decompressedData, log);

                int unknownSection01Index = SectionDefs.FindIndex(s => s.Pointer == pointerSection.Objects[0].Pointer);
                UnknownSection01 = new();
                UnknownSection01.Initialize(decompressedData[SectionDefs[unknownSection01Index].Pointer..SectionDefs[unknownSection01Index + 1].Pointer],
                    SectionDefs[unknownSection01Index].ItemCount,
                    name, log, SectionDefs[unknownSection01Index].Pointer);
            }

            if (Settings.InteractableObjectsPointer > 0)
            {
                string name = "INTERACTABLEOBJECTS";

                (int _, PointerSection pointerSection) = PointerSection.ParseSection(SectionDefs, Settings.InteractableObjectsPointer, name, decompressedData, log);
                    
                int interactableObjectsPointer = SectionDefs.FindIndex(s => s.Pointer == pointerSection.Objects[0].Pointer);
                InteractableObjectsSection = new();
                InteractableObjectsSection.Initialize(decompressedData[SectionDefs[interactableObjectsPointer].Pointer..SectionDefs[interactableObjectsPointer + 1].Pointer],
                    SectionDefs[interactableObjectsPointer].ItemCount,
                    name, log, SectionDefs[interactableObjectsPointer].Pointer);
            }

            if (Settings.UnknownSection03Pointer > 0)
            {
                string name = "UNKNOWNSECTION03";

                (int _, PointerSection pointerSection) = PointerSection.ParseSection(SectionDefs, Settings.UnknownSection03Pointer, name, decompressedData, log);

                int unknownSection03Index = SectionDefs.FindIndex(s => s.Pointer == pointerSection.Objects[0].Pointer);
                UnknownSection03 = new();
                UnknownSection03.Initialize(decompressedData[SectionDefs[unknownSection03Index].Pointer..SectionDefs[unknownSection03Index + 1].Pointer],
                    SectionDefs[unknownSection03Index].ItemCount,
                    name, log, SectionDefs[unknownSection03Index].Pointer);
            }

            if (Settings.StartingChibisSectionPointer > 0)
            {
                string name = "STARTINGCHIBIS";

                (int _, PointerSection pointerSection) = PointerSection.ParseSection(SectionDefs, Settings.StartingChibisSectionPointer, name, decompressedData, log);

                int startingChibisSectionIndex = SectionDefs.FindIndex(s => s.Pointer == pointerSection.Objects[0].Pointer);
                StartingChibisSection = new();
                StartingChibisSection.Initialize(decompressedData[SectionDefs[startingChibisSectionIndex].Pointer..SectionDefs[startingChibisSectionIndex + 1].Pointer],
                    SectionDefs[startingChibisSectionIndex].ItemCount,
                    name, log, SectionDefs[startingChibisSectionIndex].Pointer);
            }

            if (Settings.MapCharactersSectionPointer > 0)
            {
                string name = "MAPCHARACTERS";

                (int _, PointerSection pointerSection) = PointerSection.ParseSection(SectionDefs, Settings.MapCharactersSectionPointer, name, decompressedData, log);

                int mapCharactersSectionIndex = SectionDefs.FindIndex(s => s.Pointer == pointerSection.Objects[0].Pointer);
                MapCharactersSection = new();
                MapCharactersSection.Initialize(decompressedData[SectionDefs[mapCharactersSectionIndex].Pointer..SectionDefs[mapCharactersSectionIndex + 1].Pointer],
                    SectionDefs[mapCharactersSectionIndex].ItemCount,
                    name, log, SectionDefs[mapCharactersSectionIndex].Pointer);
            }

            if (Settings.UnknownSection06Pointer > 0)
            {
                string name = "UNKNOWNSECTION06";

                (int _, PointerSection pointerSection) = PointerSection.ParseSection(SectionDefs, Settings.UnknownSection06Pointer, name, decompressedData, log);

                int unknownSection06Index = SectionDefs.FindIndex(s => s.Pointer == pointerSection.Objects[0].Pointer);
                UnknownSection06 = new();
                UnknownSection06.Initialize(decompressedData[SectionDefs[unknownSection06Index].Pointer..SectionDefs[unknownSection06Index + 1].Pointer],
                    SectionDefs[unknownSection06Index].ItemCount,
                    name, log, SectionDefs[unknownSection06Index].Pointer);
            }

            if (Settings.UnknownSection07Pointer > 0)
            {
                string name = "UNKNOWNSECTION07";

                (int pointerSectionIndex, PointerSection pointerSection) = PointerSection.ParseSection(SectionDefs, Settings.UnknownSection07Pointer, name, decompressedData, log);

                int unknownSection07Index = SectionDefs.FindIndex(s => s.Pointer == pointerSection.Objects[0].Pointer);
                UnknownSection07 = new();
                UnknownSection07.Initialize(decompressedData[SectionDefs[unknownSection07Index].Pointer..SectionDefs[unknownSection07Index + 1].Pointer],
                    SectionDefs[unknownSection07Index].ItemCount,
                    name, log, SectionDefs[unknownSection07Index].Pointer);
            }

            int choicesSectionIndex = SectionDefs.FindIndex(s => s.Pointer == Settings.ChoicesSectionPointer);
            if (Settings.ChoicesSectionPointer > 0)
            {
                string name = "CHOICES";

                ChoicesSection = new();
                ChoicesSection.Initialize(decompressedData[SectionDefs[choicesSectionIndex].Pointer..SectionDefs[choicesSectionIndex + 1].Pointer],
                    SectionDefs[choicesSectionIndex].ItemCount,
                    name, log, SectionDefs[choicesSectionIndex].Pointer);
            }

            int unknownSection09Index = SectionDefs.FindIndex(s => s.Pointer == Settings.UnknownSection09Pointer);
            if (unknownSection09Index > choicesSectionIndex + 1)
            {
                string name = "UNKNOWNSECTION08";
                    
                UnknownSection08 = new();
                UnknownSection08.Initialize(decompressedData[SectionDefs[choicesSectionIndex + 1].Pointer..SectionDefs[choicesSectionIndex + 2].Pointer],
                    SectionDefs[choicesSectionIndex + 1].ItemCount,
                    name, log, SectionDefs[choicesSectionIndex + 1].Pointer);
            }

            if (Settings.UnknownSection09Pointer > 0)
            {
                string name = "UNKNOWNSECTION09";

                UnknownSection09 = new();
                UnknownSection09.Initialize(decompressedData[SectionDefs[unknownSection09Index].Pointer..SectionDefs[unknownSection09Index + 1].Pointer],
                    SectionDefs[unknownSection09Index].ItemCount,
                    name, log, SectionDefs[unknownSection09Index].Pointer);
            }

            if (Settings.UnknownSection10Pointer > 0)
            {
                string name = "UNKNOWNSECTION10";

                int unknownSection10Index = SectionDefs.FindIndex(s => s.Pointer == Settings.UnknownSection10Pointer);
                UnknownSection10 = new();
                UnknownSection10.Initialize(decompressedData[SectionDefs[unknownSection10Index].Pointer..SectionDefs[unknownSection10Index + 1].Pointer],
                    SectionDefs[unknownSection10Index].ItemCount,
                    name, log, SectionDefs[unknownSection10Index].Pointer);
            }

            int labelsSectionPointerIndex = SectionDefs.FindIndex(s => s.Pointer == Settings.LabelsSectionPointer);
            LabelsSection = new();
            if (Settings.LabelsSectionPointer > 0)
            {
                string name = "LABELS";

                LabelsSection.Initialize(decompressedData[SectionDefs[labelsSectionPointerIndex].Pointer..SectionDefs[labelsSectionPointerIndex + 1].Pointer],
                    SectionDefs[labelsSectionPointerIndex].ItemCount,
                    name, log, SectionDefs[labelsSectionPointerIndex].Pointer);
            }

            if (dialogueSectionPointerIndex > labelsSectionPointerIndex + 1)
            {
                for (int i = labelsSectionPointerIndex + 1; i < dialogueSectionPointerIndex; i++)
                {
                    string name = $"DRAMATISPERSONAE{i - labelsSectionPointerIndex}";

                    DramatisPersonaeSection dramatisPersonaeSection = new() { Index = i - labelsSectionPointerIndex };
                    dramatisPersonaeSection.Initialize(decompressedData[SectionDefs[i].Pointer..SectionDefs[i + 1].Pointer],
                        SectionDefs[i].ItemCount,
                        name, log, SectionDefs[i].Pointer);
                    DramatisPersonaeSections.Add(dramatisPersonaeSection);
                }
            }

            if (Settings.DialogueSectionPointer > 0)
            {
                string name = "DIALOGUESECTION";

                DialogueSection = new();
                DialogueSection.Initialize(decompressedData,
                    SectionDefs[dialogueSectionPointerIndex].ItemCount,
                    name, log, SectionDefs[dialogueSectionPointerIndex].Pointer);
                DialogueSection.InitializeDramatisPersonaeIndices(DramatisPersonaeSections);
            }

            int conditionalSectionIndex = SectionDefs.FindIndex(s => s.Pointer == Settings.ConditionalsSectionPointer);
            if (Settings.ConditionalsSectionPointer > 0)
            {
                string name = "CONDITIONALS";

                ConditionalsSection = new();
                ConditionalsSection.Initialize(decompressedData,
                    SectionDefs[conditionalSectionIndex].ItemCount,
                    name, log, SectionDefs[conditionalSectionIndex].Pointer);
            }

            int scriptSectionDefinitionsSectionIndex = SectionDefs.FindIndex(s => s.Pointer == Settings.ScriptSectionDefinitionsSectionPointer);
            ScriptSectionDefinitionsSection scriptSectionDefinitionsSection = new() { Labels = LabelsSection.Objects.Select(l => l.Name.Replace("/", "")).ToList() };
            if (Settings.ScriptSectionDefinitionsSectionPointer > 0)
            {
                string name = "SCRIPTDEFINITIONS";

                scriptSectionDefinitionsSection.Initialize(decompressedData[SectionDefs[scriptSectionDefinitionsSectionIndex].Pointer..],
                    Settings.NumScriptSections,
                    name, log,
                    SectionDefs[scriptSectionDefinitionsSectionIndex].Pointer);
            }

            for (int i = 0; i < scriptSectionDefinitionsSection.Objects.Count; i++)
            {
                if (scriptSectionDefinitionsSection.Objects[i].Pointer > 0)
                {
                    ScriptSection scriptSection = new() { CommandsAvailable = CommandsAvailable };
                    scriptSection.Initialize(decompressedData[scriptSectionDefinitionsSection.Objects[i].Pointer..(scriptSectionDefinitionsSection.Objects[i].Pointer + scriptSectionDefinitionsSection.Objects[i].NumCommands * 0x24)],
                        scriptSectionDefinitionsSection.Objects[i].NumCommands,
                        string.IsNullOrEmpty(LabelsSection.Objects[i].Name) ? $"SCRIPT{i:D2}" : LabelsSection.Objects[i].Name.Replace("/", ""),
                        log, scriptSectionDefinitionsSection.Objects[i].Pointer);
                    ScriptSections.Add(scriptSection);
                }
            }
            if (Name.StartsWith("CHS_") && ScriptSections.Count == 5)
            {
                for (int i = 4; i > 1; i--)
                {
                    ScriptSections[i].Name = ScriptSections[i - 1].Name;
                }
                ScriptSections[1].Name = "SCRIPT01";
            }

            int eventNameSectionIndex = SectionDefs.FindIndex(s => s.Pointer == Settings.EventNamePointer);
            if (Settings.EventNamePointer > 0)
            {
                string name = "EVENTNAME";

                EventNameSection = new();
                EventNameSection.Initialize(Encoding.ASCII.GetBytes(IO.ReadAsciiString(decompressedData, SectionDefs[eventNameSectionIndex].Pointer)),
                    SectionDefs[eventNameSectionIndex].ItemCount, name, log, SectionDefs[eventNameSectionIndex].Pointer);
            }

            for (int i = SectionDefs.FindIndex(f => f.Pointer == Settings.LabelsSectionPointer) + 1; i < dialogueSectionPointerIndex; i++)
            {
                DramatisPersonae.Add(SectionDefs[i].Pointer,
                    Encoding.GetEncoding("Shift-JIS").GetString(decompressedData.Skip(SectionDefs[i].Pointer).TakeWhile(b => b != 0x00).ToArray()));
            }
        }
        
        InitializeDialogueAndEndPointers(decompressedData, offset);
    }

    internal void InitializeDialogueAndEndPointers(byte[] decompressedData, int offset, bool @override = false)
    {
        if (Name != "CHESSS" && Name != "EVTTBLS" && Name != "TOPICS" && Name != "SCENARIOS" && Name != "TUTORIALS" && Name != "VOICEMAPS"
            && Name != "MESSS" && !Generic
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
                DialogueLines.Add(new((Speaker)character, speakerName, speakerPointer, dialoguePointer, [.. Data]));
            }
        }

        _pointerToEndPointerSection = BitConverter.ToInt32(decompressedData.Skip(4).Take(4).ToArray());
        int numEndPointers = BitConverter.ToInt32(decompressedData.Skip(_pointerToEndPointerSection).Take(4).ToArray());
        for (int i = 0; i < numEndPointers; i++)
        {
            EndPointers.Add(BitConverter.ToInt32(decompressedData.Skip(_pointerToEndPointerSection + 0x04 * (i + 1)).Take(4).ToArray()));
        }

        EndPointerPointers = EndPointers.Select(p => { return BitConverter.ToInt32(decompressedData.Skip(p).Take(4).ToArray()); }).ToList();

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

    /// <summary>
    /// Collates a list of topics used in given out in the event file
    /// </summary>
    /// <param name="availableTopics">A list of all topics from the TOPIC.S file in evt.bin</param>
    public void IdentifyEventFileTopics(IList<Topic> availableTopics)
    {
        foreach (ScriptSection scriptSection in ScriptSections)
        {
            foreach (ScriptCommandInvocation command in scriptSection.Objects)
            {
                if (command.Command.Mnemonic == CommandVerb.TOPIC_GET.ToString())
                {
                    Topic topic = availableTopics.FirstOrDefault(t => t.Id == command.Parameters[0]);
                    if (topic is not null)
                    {
                        Topics.Add(topic);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Initializes "dialogue lines" for special files (honestly this should be deprecated but currently isn't for...reasons)
    /// </summary>
    public void InitializeDialogueForSpecialFiles()
    {
        DialogueLines.Clear();
        foreach (int endPointerPointer in EndPointerPointers)
        {
            DialogueLines.Add(new(Speaker.INFO, "INFO", 0, endPointerPointer, [.. Data]));
        }
    }

    /// <summary>
    /// Initializes SCENARIO.S (should be its own class but alas, here we are)
    /// </summary>
    public void InitializeScenarioFile()
    {
        InitializeDialogueForSpecialFiles();
        Scenario = new([.. Data], DialogueLines, SectionDefs);
    }

    /// <summary>
    /// Initializes EVTTBL.S (should be its own class but alas, here we are)
    /// </summary>
    public void InitializeEventTableFile()
    {
        EvtTbl = new([.. Data]);
    }

    /// <summary>
    /// Initializes CHESS.S (should be its own class but alas, here we are)
    /// </summary>
    public void InitializeChessFile()
    {
        InitializeDialogueForSpecialFiles();
        ChessFile = new([.. Data]);
    }

    /// <summary>
    /// Returns the binary data representing this file
    /// </summary>
    /// <returns>Byte array of file data</returns>
    public override byte[] GetBytes() => Data.ToArray();

    /// <summary>
    /// Replaces a specific dialogue line
    /// </summary>
    /// <param name="index">The index of the dialogue line to edit</param>
    /// <param name="newText">The new text to replace the line with (in the original Shift-JIS charset)</param>
    public virtual void EditDialogueLine(int index, string newText)
    {
        Edited = true;
        int oldLength = DialogueLines[index].Length + DialogueLines[index].NumPaddingZeroes;
        DialogueLines[index].Text = newText;
        DialogueLines[index].NumPaddingZeroes = 4 - DialogueLines[index].Length % 4;
        int lengthDifference = DialogueLines[index].Length + DialogueLines[index].NumPaddingZeroes - oldLength;

        List<byte> toWrite = [.. DialogueLines[index].Data];
        for (int i = 0; i < DialogueLines[index].NumPaddingZeroes; i++)
        {
            toWrite.Add(0);
        }

        Data.RemoveRange(DialogueLines[index].Pointer, oldLength);
        Data.InsertRange(DialogueLines[index].Pointer, toWrite);

        ShiftPointers(DialogueLines[index].Pointer, lengthDifference);
    }

    internal virtual void ShiftPointers(int shiftLocation, int shiftAmount)
    {
        for (int i = 0; i < SectionDefs.Count; i++)
        {
            if (SectionDefs[i].Pointer > shiftLocation)
            {
                SectionDefs[i].Pointer += shiftAmount;
                Data.RemoveRange(0x0C + 0x08 * i, 4);
                Data.InsertRange(0x0C + 0x08 * i, BitConverter.GetBytes(SectionDefs[i].Pointer));
            }
        }
        if (_pointerToEndPointerSection > shiftLocation)
        {
            _pointerToEndPointerSection += shiftAmount;
            Data.RemoveRange(0x04, 4);
            Data.InsertRange(0x04, BitConverter.GetBytes(_pointerToEndPointerSection));
        }
        for (int i = 0; i < EndPointers.Count; i++)
        {
            if (EndPointers[i] > shiftLocation)
            {
                EndPointers[i] += shiftAmount;
                Data.RemoveRange(_pointerToEndPointerSection + 0x04 * (i + 1), 4);
                Data.InsertRange(_pointerToEndPointerSection + 0x04 * (i + 1), BitConverter.GetBytes(EndPointers[i]));
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

    /// <summary>
    /// Writes a RESX file containing the file's dialogue to disk
    /// </summary>
    /// <param name="fileName">The location to output the RESX file</param>
    public void WriteResxFile(string fileName)
    {
        using ResXResourceWriter resxWriter = new(fileName);
        for (int i = 0; i < DialogueLines.Count; i++)
        {
            if (!string.IsNullOrWhiteSpace(DialogueLines[i].Text) && DialogueLines[i].Length > 1)
            {
                resxWriter.AddResource(new($"{i:D4} ({Path.GetFileNameWithoutExtension(fileName)}) {DialogueLines[i].Speaker} ({DialogueLines[i].SpeakerName})",
                    DialogueLines[i].Text));
            }
        }
    }

    /// <summary>
    /// Loads a RESX file from disk and replaces all the dialogue lines in the files with ones from the RESX
    /// </summary>
    /// <param name="fileName">The RESX file on disk to load</param>
    /// <param name="spellCheck">Optionally, an initialized Hunspell instance for checking spellings</param>
    /// <param name="warningOut">A TextWriter to write warnings to</param>
    public void ImportResxFile(string fileName, Hunspell spellCheck = null, TextWriter warningOut = null)
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
            int dialogueIndex = int.Parse(((string)d.Key)[..4]);
            bool datFile = ((string)d.Key).Contains("dat_");
            string dialogueText = ((string)d.Value).GetUnShapedUnicode().DecodeEncodedNonAsciiCharacters();

            // Replace all faux-ellipses with an ellipsis character
            dialogueText = dialogueText.Replace("...", "…");
            // Replace all faux-em-dashes with actual em-dash characters
            dialogueText = dialogueText.Replace("--", "—");
            // Consolidate Unix/Windows newlines to just \n
            dialogueText = dialogueText.Replace("\r\n", "\n");

            if (spellCheck is not null && !Regex.IsMatch(dialogueText, @"[\u3040-\u30ff]"))
            {
                // For arabic, we want to use the shaped text, so we do it this way unfortunately
                string clearText = ((string)d.Value).Replace("...", "…");
                clearText = clearText.Replace("--", "—");
                clearText = clearText.Replace("\r\n", "\n");
                clearText = Regex.Replace(clearText, @"[.,;:“”""!?()*<>—…~%&=/♪\u060c\u061b\u061f]", " ");
                clearText = clearText.Replace('’', '\'');
                clearText = Regex.Replace(clearText, @"(\w)-\s(\w)", "$1$2");
                clearText = Regex.Replace(clearText, @"#(?:[PSWQ][KE]?\d{1,3}|DP|sk)", "");
                foreach (string word in Regex.Split(clearText, @"\s+"))
                {
                    if (!string.IsNullOrWhiteSpace(word) && !spellCheck.Spell(word))
                    {
                        warningOut?.WriteLine($"Spellcheck in file {Index} line {dialogueIndex}: word '{word}' does not exist in dictionary. " +
                                              $"Did you mean {string.Join(", ", spellCheck.Suggest(word))}");
                    }
                }
            }

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

                if (dialogueText[i] == '\n')
                {
                    lineLength = 0;
                    continue;
                }

                if (FontReplacementMap.ContainsKey(dialogueText[i]))
                {
                    char newCharacter = FontReplacementMap[dialogueText[i]].OriginalCharacter;
                    if (dialogueText[i] == '"' && (i == dialogueText.Length - 1
                                                   || dialogueText[i + 1] == ' ' || dialogueText[i + 1] == '!' || dialogueText[i + 1] == '?' || dialogueText[i + 1] == '.'
                                                   || dialogueText[i + 1] == '…' || dialogueText[i + 1] == '\n' || dialogueText[i + 1] == '#' || dialogueText[i + 1] == '—'))
                    {
                        newCharacter = '”';
                    }
                    lineLength += FontReplacementMap[dialogueText[i]].Offset;
                    if (i < dialogueText.Length - 1 && FontReplacementMap.ContainsKey(dialogueText[i + 1]) && 
                        FontReplacementMap[dialogueText[i + 1]].CauseOffsetAdjust && FontReplacementMap[dialogueText[i]].TakeOffsetAdjust)
                    {
                        lineLength--;
                    }
                    dialogueText = dialogueText.Remove(i, 1);
                    dialogueText = dialogueText.Insert(i, $"{newCharacter}");
                }

                if (!datFile && dialogueText[i] != '　' && lineLength > DIALOGUE_LINE_LENGTH)
                {
                    int indexOfMostRecentSpace = dialogueText[..i].LastIndexOf(FontReplacementMap[' '].OriginalCharacter); // original space bc it's been replaced already
                    if (indexOfMostRecentSpace < 0)
                    {
                        // If we don't have any spaces in the line, just give up and put a space right here
                        dialogueText = dialogueText.Insert(i - 1, "\n");
                    }
                    else
                    {
                        dialogueText = dialogueText.Remove(indexOfMostRecentSpace, 1);
                        dialogueText = dialogueText.Insert(indexOfMostRecentSpace, "\n");
                    }
                    lineLength = dialogueText[(indexOfMostRecentSpace + 1)..(i + 1)].Sum(c => FontReplacementMap.ReverseLookup(c)?.Offset ?? 0);
                }
            }
            
            if (!datFile && (dialogueText.Count(c => c == '\n') > 1 || dialogueText.Count(c => c == '\n') == 1 && lineLength > DIALOGUE_LINE_LENGTH - 7) 
                || DialogueLines[dialogueIndex].SpeakerName == "CHOICE" && dialogueText.Length > 256)
            {
                string type = "dialogue line";
                if (DialogueLines[dialogueIndex].SpeakerName == "CHOICE")
                {
                    type = "choice";
                }
                warningOut?.WriteLine($"File {Index} has {type} too long ({dialogueIndex}) (starting with: {dialogueText[..Math.Min(15, dialogueText.Length - 1)]})");
            }

            EditDialogueLine(dialogueIndex, dialogueText);
        }
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"{Index:X3} {Index:D3} 0x{Offset:X8} '{Name}'";
    }

    /// <inheritdoc/>
    public string GetSource(Dictionary<string, IncludeEntry[]> includes)
    {
        switch (Name)
        {
            case "CHESSS":
            {
                if (ChessFile is null)
                {
                    InitializeChessFile();
                }

                return ChessFile.GetSource();
            }
            case "EVTTBLS":
            {
                if (EvtTbl is null)
                {
                    InitializeEventTableFile();
                }
                return EvtTbl.GetSource(includes, Log);
            }
            case "SCENARIOS":
            {
                if (Scenario is null)
                {
                    InitializeScenarioFile();
                }
                return Scenario.GetSource(includes, Log);
            }
            case "TOPICS":
            {
                if (Topics.Count == 0)
                {
                    InitializeTopicFile();
                }
                StringBuilder sb = new();

                sb.AppendLine($".word 1");
                sb.AppendLine(".word END_POINTERS");
                sb.AppendLine(".word FILE_START");
                sb.AppendLine(".word TOPICS");
                sb.AppendLine($".word {Topics.Count + 1}");
                sb.AppendLine();

                sb.AppendLine("FILE_START:");
                sb.AppendLine("TOPICS:");
                int numEndPointers = 0;
                for (int i = 0; i < Topics.Count; i++)
                {
                    sb.AppendLine(Topics[i].GetSource(i, ref numEndPointers));
                }
                sb.AppendLine(".skip 0x24");

                for (int i = 0; i < Topics.Count; i++)
                {
                    sb.AppendLine($"TOPICTITL{i:D3}: .string \"{Topics[i].Title.EscapeShiftJIS()}\"");
                    sb.AsmPadString(Topics[i].Title, Encoding.GetEncoding("Shift-JIS"));
                    sb.AppendLine($"TOPICDESC{i:D3}: .string \"{Topics[i].Description.EscapeShiftJIS()}\"");
                    sb.AsmPadString(Topics[i].Description, Encoding.GetEncoding("Shift-JIS"));
                }

                sb.AppendLine("END_POINTERS:");
                sb.AppendLine($".word {numEndPointers}");
                for (int i = 0; i < numEndPointers; i++)
                {
                    sb.AppendLine($".word POINTER{i}");
                }

                return sb.ToString();
            }
            case "TUTORIALS":
            {
                if (Tutorials.Count == 0)
                {
                    InitializeTutorialFile();
                }
                StringBuilder sb = new();

                sb.AppendLine(".word 1");
                sb.AppendLine(".word END_POINTERS");
                sb.AppendLine(".word FILE_START");
                sb.AppendLine(".word TUTORIALS");
                sb.AppendLine($".word {Tutorials.Count}");
                sb.AppendLine();
                sb.AppendLine("FILE_START:");
                sb.AppendLine("TUTORIALS:");

                foreach (Tutorial tutorial in Tutorials)
                {
                    sb.AppendLine($".short {tutorial.Id}");
                    sb.AppendLine($".short {tutorial.AssociatedScript}");
                }

                sb.AppendLine("END_POINTERS:");
                sb.AppendLine(".word 0");

                return sb.ToString();
            }
            case "VOICEMAPS":
            {
                VoiceMapFile voiceMapFile = CastTo<VoiceMapFile>();
                return voiceMapFile.GetSource();
            }
            default:
            {
                StringBuilder sb = new();
                sb.AppendLine(".include \"COMMANDS.INC\"");
                sb.AppendLine();
                sb.AppendLine($".word {NumSections}");
                sb.AppendLine(".word END_POINTERS");
                sb.AppendLine(".word FILE_START");

                sb.AppendLine($".word {SettingsSection.Name}");
                sb.AppendLine($".word {SettingsSection.Objects.Count}");
                if (UnknownSection01 is not null)
                {
                    sb.AppendLine($".word {UnknownSection01.Name}");
                    sb.AppendLine($".word {UnknownSection01.Objects.Count}");
                    sb.AppendLine($".word {UnknownSection01.Name}_POINTER");
                    sb.AppendLine($".word 2");
                }
                if (InteractableObjectsSection is not null)
                {
                    sb.AppendLine($".word {InteractableObjectsSection.Name}");
                    sb.AppendLine($".word {InteractableObjectsSection.Objects.Count}");
                    sb.AppendLine($".word {InteractableObjectsSection.Name}_POINTER");
                    sb.AppendLine($".word 2");
                }
                if (UnknownSection03 is not null)
                {
                    sb.AppendLine($".word {UnknownSection03.Name}");
                    sb.AppendLine($".word {UnknownSection03.Objects.Count}");
                    sb.AppendLine($".word {UnknownSection03.Name}_POINTER");
                    sb.AppendLine($".word 2");
                }
                if (StartingChibisSection is not null)
                {
                    sb.AppendLine($".word {StartingChibisSection.Name}");
                    sb.AppendLine($".word {StartingChibisSection.Objects.Count}");
                    sb.AppendLine($".word {StartingChibisSection.Name}_POINTER");
                    sb.AppendLine($".word 2");
                }
                if (MapCharactersSection is not null)
                {
                    sb.AppendLine($".word {MapCharactersSection.Name}");
                    sb.AppendLine($".word {MapCharactersSection.Objects.Count}");
                    sb.AppendLine($".word {MapCharactersSection.Name}_POINTER");
                    sb.AppendLine($".word 2");
                }
                if (UnknownSection06 is not null)
                {
                    sb.AppendLine($".word {UnknownSection06.Name}");
                    sb.AppendLine($".word {UnknownSection06.Objects.Count}");
                    sb.AppendLine($".word {UnknownSection06.Name}_POINTER");
                    sb.AppendLine($".word 2");
                }
                if (UnknownSection07 is not null)
                {
                    sb.AppendLine($".word {UnknownSection07.Name}");
                    sb.AppendLine($".word {UnknownSection07.Objects.Count}");
                    sb.AppendLine($".word {UnknownSection07.Name}_POINTER");
                    sb.AppendLine($".word 2");
                }
                if (ChoicesSection is not null)
                {
                    sb.AppendLine($".word {ChoicesSection.Name}");
                    sb.AppendLine($".word {ChoicesSection.Objects.Count}");
                }
                if (UnknownSection08 is not null)
                {
                    sb.AppendLine($".word {UnknownSection08.Name}");
                    sb.AppendLine($".word {UnknownSection08.Objects.Count}");
                    sb.AppendLine($".word {UnknownSection08.Name}_POINTER");
                    sb.AppendLine($".word 2");
                }
                if (UnknownSection09 is not null)
                {
                    sb.AppendLine($".word {UnknownSection09.Name}");
                    sb.AppendLine($".word {UnknownSection09.Objects.Count}");
                }
                if (UnknownSection10 is not null)
                {
                    sb.AppendLine($".word {UnknownSection10.Name}");
                    sb.AppendLine($".word {UnknownSection10.Objects.Count}");
                }
                if (LabelsSection is not null)
                {
                    sb.AppendLine($".word {LabelsSection.Name}");
                    sb.AppendLine($".word {LabelsSection.Objects.Count}");
                }
                foreach (DramatisPersonaeSection dramatisPersonae in DramatisPersonaeSections)
                {
                    sb.AppendLine($".word {dramatisPersonae.Name}");
                    sb.AppendLine($".word 0");
                }
                if (DialogueSection is not null)
                {
                    sb.AppendLine($".word {DialogueSection.Name}");
                    sb.AppendLine($".word {DialogueSection.Objects.Count}");
                }
                if (ConditionalsSection is not null)
                {
                    sb.AppendLine($".word {ConditionalsSection.Name}");
                    sb.AppendLine($".word {ConditionalsSection.Objects.Count}");
                }
                foreach (ScriptSection scriptSection in ScriptSections)
                {
                    sb.AppendLine($".word {scriptSection.Name}");
                    sb.AppendLine($".word {scriptSection.Objects.Count + 1}");
                }
                sb.AppendLine($".word SCRIPTDEFINITIONS");
                sb.AppendLine($".word {ScriptSections.Count + 1}");
                if (EventNameSection is not null)
                {
                    sb.AppendLine($".word {EventNameSection.Name}");
                    sb.AppendLine($".word {EventNameSection.Objects.Count}");
                }

                sb.AppendLine();
                sb.AppendLine("FILE_START:");

                int currentPointer = 0;
                if (EventNameSection is not null)
                {
                    sb.AppendLine(EventNameSection.GetAsm(0, ref currentPointer));
                }
                if (UnknownSection01 is not null)
                {
                    sb.AppendLine(UnknownSection01.GetAsm(0, ref currentPointer));
                    sb.AppendLine(PointerSection.GetForSection(UnknownSection01).GetAsm(0, ref currentPointer));
                }
                if (InteractableObjectsSection is not null)
                {
                    sb.AppendLine(InteractableObjectsSection.GetAsm(0, ref currentPointer));
                    sb.AppendLine(PointerSection.GetForSection(InteractableObjectsSection).GetAsm(0, ref currentPointer));
                }
                if (UnknownSection03 is not null)
                {
                    sb.AppendLine(UnknownSection03.GetAsm(0, ref currentPointer));
                    sb.AppendLine(PointerSection.GetForSection(UnknownSection03).GetAsm(0, ref currentPointer));
                }
                if (StartingChibisSection is not null)
                {
                    sb.AppendLine(StartingChibisSection.GetAsm(0, ref currentPointer));
                    sb.AppendLine(PointerSection.GetForSection(StartingChibisSection).GetAsm(0, ref currentPointer));
                }
                if (MapCharactersSection is not null)
                {
                    sb.AppendLine(MapCharactersSection.GetAsm(0, ref currentPointer));
                    sb.AppendLine(PointerSection.GetForSection(MapCharactersSection).GetAsm(0, ref currentPointer));
                }
                if (UnknownSection06 is not null)
                {
                    sb.AppendLine(UnknownSection06.GetAsm(0, ref currentPointer));
                    sb.AppendLine(PointerSection.GetForSection(UnknownSection06).GetAsm(0, ref currentPointer));
                }
                if (UnknownSection07 is not null)
                {
                    sb.AppendLine(UnknownSection07.GetAsm(0, ref currentPointer));
                    sb.AppendLine(PointerSection.GetForSection(UnknownSection07).GetAsm(0, ref currentPointer));
                }
                if (ChoicesSection is not null)
                {
                    sb.AppendLine(ChoicesSection.GetAsm(0, ref currentPointer));
                }
                if (UnknownSection08 is not null)
                {
                    sb.AppendLine(UnknownSection08.GetAsm(0, ref currentPointer));
                    sb.AppendLine(PointerSection.GetForSection(UnknownSection08).GetAsm(0, ref currentPointer));
                }
                if (UnknownSection09 is not null)
                {
                    sb.AppendLine(UnknownSection09.GetAsm(0, ref currentPointer));
                }
                if (UnknownSection10 is not null)
                {
                    sb.AppendLine(UnknownSection10.GetAsm(0, ref currentPointer));
                }
                if (LabelsSection is not null)
                {
                    sb.AppendLine(LabelsSection.GetAsm(0, ref currentPointer));
                }
                foreach (DramatisPersonaeSection dramatisPersonae in DramatisPersonaeSections)
                {
                    sb.AppendLine(dramatisPersonae.GetAsm(0, ref currentPointer));
                }
                if (DialogueSection is not null)
                {
                    sb.AppendLine(DialogueSection.GetAsm(0, ref currentPointer));
                }
                if (ConditionalsSection is not null)
                {
                    sb.AppendLine(ConditionalsSection.GetAsm(0, ref currentPointer));
                }
                foreach (ScriptSection scriptSection in ScriptSections)
                {
                    sb.AppendLine(scriptSection.GetAsm(0, ref currentPointer));
                }
                sb.AppendLine(new ScriptSectionDefinitionsSection() { Name = "SCRIPTDEFINITIONS" }.GetAsm(0, ref currentPointer, this)); // we've basically made this into a static method. sorry.
                sb.AppendLine(SettingsSection.GetAsm(0, ref currentPointer, this));

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
}

/// <summary>
/// The settings for a standard event file
/// </summary>
public class EventFileSettings
{
    internal const int SETTINGS_LENGTH = 0x128;

    internal int EventNamePointer { get; set; }
    /// <summary>
    /// Unknown
    /// </summary>
    public int NumUnknown01 { get; private set; }
    internal int UnknownSection01Pointer { get; set; } // probably straight up unused
    /// <summary>
    /// Number of interactable objects sections defined in the event file
    /// </summary>
    public int NumInteractableObjects { get; private set; }
    internal int InteractableObjectsPointer { get; set; } // potentially something to do with flag setting after you've investigated something
    /// <summary>
    /// Unknown
    /// </summary>
    public int NumUnknown03 { get; private set; }
    internal int UnknownSection03Pointer { get; set; } // probably straight up unused
    /// <summary>
    /// Number of "starting chibis" sections define in the file
    /// </summary>
    public int NumStartingChibisSections { get; private set; }
    internal int StartingChibisSectionPointer { get; set; }
    /// <summary>
    /// Number of map character sections defined in the file
    /// </summary>
    public int NumMapCharacterSections { get; private set; }
    internal int MapCharactersSectionPointer { get; set; }
    /// <summary>
    /// Unknown
    /// </summary>
    public int NumUnknown06 { get; private set; }
    internal int UnknownSection06Pointer { get; set; } // probably straight up unused
    /// <summary>
    /// Unknown
    /// </summary>
    public int NumUnknown07 { get; private set; }
    internal int UnknownSection07Pointer { get; set; } // more flags stuff (investigation-related)
    /// <summary>
    /// Number of choices defined in the file
    /// </summary>
    public int NumChoices { get; private set; }
    internal int ChoicesSectionPointer { get; set; }
    /// <summary>
    /// Unused
    /// </summary>
    public int Unused44 { get; set; }
    /// <summary>
    /// Unused
    /// </summary>
    public int Unused48 { get; set; }
    /// <summary>
    /// Unknown
    /// </summary>
    public int NumUnknown09 { get; private set; }
    internal int UnknownSection09Pointer { get; set; } // maybe unused
    /// <summary>
    /// Unknown
    /// </summary>
    public int NumUnknown10 { get; private set; }
    internal int UnknownSection10Pointer { get; set; } // seems unused
    /// <summary>
    /// Number of labels defined in the file
    /// </summary>
    public int NumLabels { get; private set; }
    internal int LabelsSectionPointer { get; set; }
    /// <summary>
    /// Number of dialogue entries defined in the file
    /// </summary>
    public int NumDialogueEntries { get; internal set; }
    internal int DialogueSectionPointer { get; set; }
    /// <summary>
    /// Number of conditionals defined in the file
    /// </summary>
    public int NumConditionals { get; private set; }
    internal int ConditionalsSectionPointer { get; set; }
    /// <summary>
    /// Number of script sections defined in the file
    /// </summary>
    public int NumScriptSections { get; private set; }
    internal int ScriptSectionDefinitionsSectionPointer { get; set; }

    /// <summary>
    /// Parameterless constructor for serialization
    /// </summary>
    public EventFileSettings()
    {
    }
    
    /// <summary>
    /// Creates an event file settings instance
    /// </summary>
    /// <param name="data">Data from the event file</param>
    public EventFileSettings(IEnumerable<byte> data)
    {
        if (data.Count() < 0x128)
        {
            return;
        }
        EventNamePointer = BitConverter.ToInt32(data.Take(4).ToArray());
        NumUnknown01 = BitConverter.ToInt32(data.Skip(0x004).Take(4).ToArray());
        UnknownSection01Pointer = BitConverter.ToInt32(data.Skip(0x008).Take(4).ToArray());
        NumInteractableObjects = BitConverter.ToInt32(data.Skip(0x00C).Take(4).ToArray());
        InteractableObjectsPointer = BitConverter.ToInt32(data.Skip(0x010).Take(4).ToArray());
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

/// <summary>
/// Representation of a line of dialogue from an event file
/// </summary>
public class DialogueLine
{
    /// <summary>
    /// The pointer to the dialogue line. This will eventually be internal, but for now it needs to be public due to quirks in how the dialogue replacement engine works
    /// </summary>
    public int Pointer { get; set; }
    internal byte[] Data { get; set; }
    internal int NumPaddingZeroes { get; set; }
    /// <summary>
    /// The text of the line (Shift-JIS encoded)
    /// </summary>
    public string Text { get => Encoding.GetEncoding("Shift-JIS").GetString(Data); set => Data = Encoding.GetEncoding("Shift-JIS").GetBytes(value ?? string.Empty); }
    /// <summary>
    /// The length of the line (in Shift-JIS encoded bytes)
    /// </summary>
    public int Length => Data.Length;

    internal int SpeakerIndex { get; set; }
    internal int SpeakerPointer { get; set; }
    /// <summary>
    /// The speaker of the line
    /// </summary>
    public Speaker Speaker { get; set; }
    /// <summary>
    /// The name of the speaker (derived from the dramatis personae section)
    /// </summary>
    public string SpeakerName { get; set; }

    /// <summary>
    /// Parameterless constructor for serialization
    /// </summary>
    public DialogueLine()
    {
    }
    
    /// <summary>
    /// Extracts a dialogue line from an event file
    /// </summary>
    /// <param name="speaker">The speaker of the line</param>
    /// <param name="speakerName">The name of the speaker as a string</param>
    /// <param name="speakerPointer">The pointer to the speaker in the dramatis personae section</param>
    /// <param name="pointer">The pointer to the string data in the file</param>
    /// <param name="file">The binary representation of the event file</param>
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
            Data = [];
        }
        NumPaddingZeroes = 4 - Length % 4;
    }
    /// <summary>
    /// Creates a dialogue line from scratch
    /// </summary>
    /// <param name="line">The text of the line</param>
    /// <param name="script">The event file it should be inserted into</param>
    public DialogueLine(string line, EventFile script)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Speaker = Speaker.KYON;
        SpeakerName = "何でもない";
        SpeakerPointer = 1;
        Pointer = 1;
        Text = line;

        if (!script.DramatisPersonaeSections.Any(d => d.Objects[0] == SpeakerName))
        {
            if (script.DramatisPersonaeSections.Count == 0)
            {
                script.DramatisPersonaeSections.Add(new() { Index = 1 });
            }
            else
            {
                script.DramatisPersonaeSections.Add(new() { Index = script.DramatisPersonaeSections.Max(d => d.Index) + 1 });
            }
            script.NumSections++;
            script.DramatisPersonaeSections.Last().Name = $"DRAMATISPERSONAE{script.DramatisPersonaeSections.Last().Index}";
            script.DramatisPersonaeSections.Last().Objects.Add(SpeakerName);
        }
        SpeakerIndex = script.DramatisPersonaeSections.Count;
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return Text;
    }
}

/// <summary>
/// The enum which defines speakers
/// </summary>
public enum Speaker
{
    /// <summary>
    /// Kyon
    /// </summary>
    KYON = 0x01,
    /// <summary>
    /// Haruhi Suzumiya
    /// </summary>
    HARUHI = 0x02,
    /// <summary>
    /// Mikuru Asahina
    /// </summary>
    MIKURU = 0x03,
    /// <summary>
    /// Yuki Nagato
    /// </summary>
    NAGATO = 0x04,
    /// <summary>
    /// Itsuki Koizumi
    /// </summary>
    KOIZUMI = 0x05,
    /// <summary>
    /// Kyon's Little Sister
    /// </summary>
    KYON_SIS = 0x06,
    /// <summary>
    /// Tsuruya-san
    /// </summary>
    TSURUYA = 0x07,
    /// <summary>
    /// Taniguchi
    /// </summary>
    TANIGUCHI = 0x08,
    /// <summary>
    /// Kunikida
    /// </summary>
    KUNIKIDA = 0x09,
    /// <summary>
    /// The Computer Research Society President
    /// </summary>
    CLUB_PRES = 0x0A,
    /// <summary>
    /// Computer Research Society Member A
    /// </summary>
    CLUB_MEM_A = 0x0B,
    /// <summary>
    /// Computer Research Society Member B
    /// </summary>
    CLUB_MEM_B = 0x0C,
    /// <summary>
    /// Computer Research Society Member C
    /// </summary>
    CLUB_MEM_C = 0x0D,
    /// <summary>
    /// Computer Research Society Member D
    /// </summary>
    CLUB_MEM_D = 0x0E,
    /// <summary>
    /// Okabe-sensei
    /// </summary>
    OKABE = 0x0F,
    /// <summary>
    /// Captain of the baseball team
    /// </summary>
    BASEBALL_CAPTAIN = 0x10,
    /// <summary>
    /// Grocer
    /// </summary>
    GROCER = 0x11,
    /// <summary>
    /// Mystery Girl
    /// </summary>
    GIRL = 0x12,
    /// <summary>
    /// Old Lady
    /// </summary>
    OLD_LADY = 0x13,
    /// <summary>
    /// Fake Haruhi
    /// </summary>
    FAKE_HARUHI = 0x14,
    /// <summary>
    /// A stray cat
    /// </summary>
    STRAY_CAT = 0x15,
    /// <summary>
    /// ???
    /// </summary>
    UNKNOWN = 0x16,
    /// <summary>
    /// Info
    /// </summary>
    INFO = 0x17,
    /// <summary>
    /// Kyon's monologue
    /// </summary>
    MONOLOGUE = 0x18,
    /// <summary>
    /// Text message
    /// </summary>
    MAIL = 0x19,
}

/// <summary>
/// The color for dialogue -- maps to the #P0_ dialogue parameter
/// </summary>
public enum DialogueColor
{
    /// <summary>
    /// White dialogue color (#P00)
    /// </summary>
    WHITE,
    /// <summary>
    /// Yellow (monologue) dialogue color (#P01)
    /// </summary>
    YELLOW,
    /// <summary>
    /// Off-white dialogue color (#P02)
    /// </summary>
    OFF_WHITE,
    /// <summary>
    /// Gray (whisper) dialogue color (#P03)
    /// </summary>
    GRAY,
    /// <summary>
    /// Lavender (Information) dialogue color (#P04)
    /// </summary>
    LAVENDER,
    /// <summary>
    /// Red (topic) dialogue color (#P05)
    /// </summary>
    RED,
    /// <summary>
    /// Faded gray dialogue color (#P06)
    /// </summary>
    FADED_GRAY,
    /// <summary>
    /// Black (#P07) dialogue color
    /// </summary>
    BLACK,
}