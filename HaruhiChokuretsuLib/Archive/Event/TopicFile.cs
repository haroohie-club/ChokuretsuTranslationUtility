using HaruhiChokuretsuLib.Util;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HaruhiChokuretsuLib.Archive.Event
{
    public partial class EventFile
    {
        public List<TopicStruct> TopicStructs { get; set; } = new();

        public void InitializeTopicFile()
        {
            InitializeDialogueForSpecialFiles();
            for (int i = 0; i < DialogueLines.Count; i += 2)
            {
                TopicStructs.Add(new(i, DialogueLines[i].Text, Data.Skip(0x14 + i / 2 * 0x24).Take(0x24).ToArray(), _log));
                TopicStructs[^1].Description = Encoding.GetEncoding("Shift-JIS").GetString(Data.Skip(TopicStructs[^1].TopicDescriptionPointer).TakeWhile(b => b != 0).ToArray());
            }
        }
    }

    public class TopicStruct
    {
        public int TopicDialogueIndex { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }

        public short Id { get; set; }
        public short EventIndex { get; set; }
        public byte EpisodeGroup { get; set; }
        public byte GroupSelection { get; set; }
        public TopicCategory Category { get; set; }
        public short BaseTimeGain { get; set; }
        public short UnknownShort03 { get; set; }
        public short UnknownShort04 { get; set; }
        public short KyonTimePercentage { get; set; }
        public short MikuruTimePercentage { get; set; }
        public short NagatoTimePercentage { get; set; }
        public short KoizumiTimePercentage { get; set; }
        public short Padding { get; set; }
        public int TopicTitlePointer { get; set; }
        public int TopicDescriptionPointer { get; set; }
        public TopicCardType CardType { get; set; }
        public TopicType Type { get; set; }

        public TopicStruct(int dialogueIndex, string dialogueLine, byte[] data, ILogger log)
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
            GroupSelection = data[0x09];
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

        public override string ToString()
        {
            return $"0x{Id:X4} '{Title}'";
        }

        public string GetSource(int currentTopic, ref int endPointerIndex)
        {
            StringBuilder sb = new();

            sb.AppendLine($"TOPIC{currentTopic:D3}:");
            sb.AppendLine($"   .short {(short)CardType}");
            sb.AppendLine($"   .short {(short)Type}");
            sb.AppendLine($"   .short {Id}");
            sb.AppendLine($"   .short {EventIndex}");
            sb.AppendLine($"   .byte {EpisodeGroup}");
            sb.AppendLine($"   .byte {GroupSelection}");
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

        public string ToCsvLine()
        {
            return $"{TopicDialogueIndex},{Title},{Id:X4},{EventIndex}";
        }
    }

    public enum TopicCardType : short
    {
        Main = 0x00,
        Haruhi = 0x01,
        Mikuru = 0x02,
        Nagato = 0x03,
        Koizumi = 0x04,
        Sub = 0x05,
    }

    public enum TopicType : short
    {
        Main = 0x00,
        Haruhi = 0x01,
        Character = 0x02,
        Sub = 0x03,
    }

    public enum TopicCategory : short
    {
        Main = 0x00,
        Sub = 0x01,
        Character = 0x03,
        Haruhi = 0x04,
        Hidden = 0x05,
    }
}
