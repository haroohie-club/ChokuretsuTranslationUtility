using HaruhiChokuretsuLib.Util;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HaruhiChokuretsuLib.Archive.Event;

public partial class EventFile
{
    /// <summary>
    /// In the topic file (TOPIC.S), the list of topics defined in that file
    /// </summary>
    public List<Topic> Topics { get; set; } = [];

    /// <summary>
    /// Initializes the topic file (TOPIC.S)
    /// </summary>
    public void InitializeTopicFile()
    {
        InitializeDialogueForSpecialFiles();
        for (int i = 0; i < DialogueLines.Count; i += 2)
        {
            Topics.Add(new(i, DialogueLines[i].Text, Data.Skip(0x14 + i / 2 * 0x24).Take(0x24).ToArray(), Log));
            Topics[^1].Description = Encoding.GetEncoding("Shift-JIS").GetString(Data.Skip(Topics[^1].TopicDescriptionPointer).TakeWhile(b => b != 0).ToArray());
        }
    }
}

/// <summary>
/// Represents a topic defined in TOPIC.S
/// </summary>
public class Topic
{
    internal int TopicDialogueIndex { get; set; }
    /// <summary>
    /// The title of the topic
    /// </summary>
    public string Title { get; set; }
    /// <summary>
    /// The description of the topic
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// The topic's ID/flag
    /// </summary>
    public short Id { get; set; }
    /// <summary>
    /// For character and (hidden) main topics, the evt.bin index of the event file triggered by this topic
    /// </summary>
    public short EventIndex { get; set; }
    /// <summary>
    /// The "episode group" of the topic
    /// </summary>
    public byte EpisodeGroup { get; set; }
    /// <summary>
    /// The "puzzle phase group" of this topic (referenced by puzzle files)
    /// </summary>
    public byte PuzzlePhaseGroup { get; set; }
    /// <summary>
    /// The category of the topic as filterable in the extra menu
    /// </summary>
    public TopicCategory Category { get; set; }
    /// <summary>
    /// The base time gain (modified by the various time percentages depending on the character accompanying Haruhi)
    /// </summary>
    public short BaseTimeGain { get; set; }
    /// <summary>
    /// Unknown
    /// </summary>
    public short UnknownShort03 { get; set; }
    /// <summary>
    /// Unknown
    /// </summary>
    public short UnknownShort04 { get; set; }
    /// <summary>
    /// If Kyon is accompanying Haruhi, the time gain for this topic will be this parameter * BaseTimeGain / 100.0
    /// </summary>
    public short KyonTimePercentage { get; set; }
    /// <summary>
    /// If Mikuru is accompanying Haruhi, the time gain for this topic will be this parameter * BaseTimeGain / 100.0
    /// </summary>
    public short MikuruTimePercentage { get; set; }
    /// <summary>
    /// If Nagato is accompanying Haruhi, the time gain for this topic will be this parameter * BaseTimeGain / 100.0
    /// </summary>
    public short NagatoTimePercentage { get; set; }
    /// <summary>
    /// If Koizumi is accompanying Haruhi, the time gain for this topic will be this parameter * BaseTimeGain / 100.0
    /// </summary>
    public short KoizumiTimePercentage { get; set; }
    /// <summary>
    /// Just padding
    /// </summary>
    public short Padding { get; set; }
    internal int TopicTitlePointer { get; set; }
    internal int TopicDescriptionPointer { get; set; }
    /// <summary>
    /// The type of card displayed for the topic
    /// </summary>
    public TopicCardType CardType { get; set; }
    /// <summary>
    /// The type of the topic
    /// </summary>
    public TopicType Type { get; set; }

    /// <summary>
    /// Parameterless constructor for serialization
    /// </summary>
    public Topic()
    {
    }
    
    /// <summary>
    /// Creates a topic from data
    /// </summary>
    /// <param name="dialogueIndex">Index of the dialogue line for this topic</param>
    /// <param name="dialogueLine">Title of the topic</param>
    /// <param name="data">Topic data</param>
    /// <param name="log">ILogger instance for error logging</param>
    public Topic(int dialogueIndex, string dialogueLine, byte[] data, ILogger log)
    {
        if (data.Length != 0x24)
        {
            log.LogError($"Topic struct data length must be 0x24, was 0x{data.Length:X2}");
            return;
        }

        TopicDialogueIndex = dialogueIndex;
        Title = dialogueLine;
        CardType = (TopicCardType)IO.ReadShort(data, 0x00);
        Type = (TopicType)IO.ReadShort(data, 0x02);
        Id = IO.ReadShort(data, 0x04);
        EventIndex = IO.ReadShort(data, 0x06);
        EpisodeGroup = data[0x08];
        PuzzlePhaseGroup = data[0x09];
        Category = (TopicCategory)IO.ReadShort(data, 0x0A);
        BaseTimeGain = IO.ReadShort(data, 0x0C);
        UnknownShort03 = IO.ReadShort(data, 0x0E);
        UnknownShort04 = IO.ReadShort(data, 0x10);
        KyonTimePercentage = IO.ReadShort(data, 0x12);
        MikuruTimePercentage = IO.ReadShort(data, 0x14);
        NagatoTimePercentage = IO.ReadShort(data, 0x16);
        KoizumiTimePercentage = IO.ReadShort(data, 0x18);
        Padding = IO.ReadShort(data, 0x1A);
        TopicTitlePointer = IO.ReadInt(data, 0x1C);
        TopicDescriptionPointer = IO.ReadInt(data, 0x20);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"0x{Id:X4} '{Title}'";
    }

    internal string GetSource(int currentTopic, ref int endPointerIndex)
    {
        StringBuilder sb = new();

        sb.AppendLine($"TOPIC{currentTopic:D3}:");
        sb.AppendLine($"   .short {(short)CardType}");
        sb.AppendLine($"   .short {(short)Type}");
        sb.AppendLine($"   .short {Id}");
        sb.AppendLine($"   .short {EventIndex}");
        sb.AppendLine($"   .byte {EpisodeGroup}");
        sb.AppendLine($"   .byte {PuzzlePhaseGroup}");
        sb.AppendLine($"   .short {(short)Category}");
        sb.AppendLine($"   .short {BaseTimeGain}");
        sb.AppendLine($"   .short {UnknownShort03}");
        sb.AppendLine($"   .short {UnknownShort04}");
        sb.AppendLine($"   .short {KyonTimePercentage}");
        sb.AppendLine($"   .short {MikuruTimePercentage}");
        sb.AppendLine($"   .short {NagatoTimePercentage}");
        sb.AppendLine($"   .short {KoizumiTimePercentage}");
        sb.AppendLine($"   .short {Padding}");
        sb.AppendLine($"   POINTER{endPointerIndex++}: .word TOPICTITL{currentTopic:D3}");
        sb.AppendLine($"   POINTER{endPointerIndex++}: .word TOPICDESC{currentTopic:D3}");

        return sb.ToString();
    }

    /// <summary>
    /// Converts the topic data to a line in a CSV file
    /// </summary>
    /// <returns>A string containing the CSV formatted information about this topic</returns>
    public string ToCsvLine()
    {
        return $"{TopicDialogueIndex},{Title},{Id:X4},{EventIndex}";
    }
}

/// <summary>
/// Enum representing the type of the topic card
/// </summary>
public enum TopicCardType : short
{
    /// <summary>
    /// Main topic
    /// </summary>
    Main = 0x00,
    /// <summary>
    /// Haruhi topic
    /// </summary>
    Haruhi = 0x01,
    /// <summary>
    /// Mikuru topic
    /// </summary>
    Mikuru = 0x02,
    /// <summary>
    /// Nagato topic
    /// </summary>
    Nagato = 0x03,
    /// <summary>
    /// Koizumi topic
    /// </summary>
    Koizumi = 0x04,
    /// <summary>
    /// Sub-topic
    /// </summary>
    Sub = 0x05,
}

/// <summary>
/// Enum representing the type of the topic
/// </summary>
public enum TopicType : short
{
    /// <summary>
    /// Main topic
    /// </summary>
    Main = 0x00,
    /// <summary>
    /// Haruhi topic
    /// </summary>
    Haruhi = 0x01,
    /// <summary>
    /// Character topic
    /// </summary>
    Character = 0x02,
    /// <summary>
    /// Sub-topic
    /// </summary>
    Sub = 0x03,
}

/// <summary>
/// Enum representing the extras menu category of the topic
/// </summary>
public enum TopicCategory : short
{
    /// <summary>
    /// Main topic
    /// </summary>
    Main = 0x00,
    /// <summary>
    /// Sub-topic
    /// </summary>
    Sub = 0x01,
    /// <summary>
    /// Character topic
    /// </summary>
    Character = 0x03,
    /// <summary>
    /// Haruhi topic
    /// </summary>
    Haruhi = 0x04,
    /// <summary>
    /// Hidden topic
    /// </summary>
    Hidden = 0x05,
}