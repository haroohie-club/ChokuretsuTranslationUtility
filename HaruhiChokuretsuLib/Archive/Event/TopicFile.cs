using HaruhiChokuretsuLib.Util;
using System;
using System.Collections.Generic;
using System.Linq;

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
                TopicStructs.Add(new(i, DialogueLines[i].Text, Data.Skip(0x18 + i / 2 * 0x24).Take(0x24).ToArray(), _log));
            }
        }
    }

    public class TopicStruct
    {
        public int TopicDialogueIndex { get; set; }
        public string Title { get; set; }

        public short Id { get; set; }
        public short EventIndex { get; set; }
        public byte EpisodeGroup { get; set; }
        public byte GroupSelection { get; set; }
        public TopicType Type { get; set; }
        public short BaseTimeGain { get; set; }
        public short UnknownShort03 { get; set; }
        public short UnknownShort04 { get; set; }
        public short KyonTimePercentage { get; set; }
        public short MikuruTimePercentage { get; set; }
        public short NagatoTimePercentage { get; set; }
        public short KoizumiTimePercentage { get; set; }
        public short UnknownShort09 { get; set; }
        public short UnknownShort10 { get; set; }
        public short UnknownShort11 { get; set; }
        public short UnknownShort12 { get; set; }
        public short UnknownShort13 { get; set; }
        public short UnknownShort14 { get; set; }
        public short UnknownShort15 { get; set; }

        public TopicStruct(int dialogueIndex, string dialogueLine, byte[] data, ILogger log)
        {
            if (data.Length != 0x24)
            {
                log.LogError($"Topic struct data length must be 0x24, was 0x{data.Length:X2}");
                return;
            }

            TopicDialogueIndex = dialogueIndex;
            Title = dialogueLine;
            Id = IO.ReadShort(data, 0);
            EventIndex = IO.ReadShort(data, 0x02);
            EpisodeGroup = data[0x04];
            GroupSelection = data[0x05];
            Type = (TopicType)IO.ReadShort(data, 0x06);
            BaseTimeGain = IO.ReadShort(data, 0x08);
            UnknownShort03 = IO.ReadShort(data, 0x0A);
            UnknownShort04 = IO.ReadShort(data, 0x0C);
            KyonTimePercentage = IO.ReadShort(data, 0x0E);
            MikuruTimePercentage = IO.ReadShort(data, 0x10);
            NagatoTimePercentage = IO.ReadShort(data, 0x12);
            KoizumiTimePercentage = IO.ReadShort(data, 0x14);
            UnknownShort09 = IO.ReadShort(data, 0x16);
            UnknownShort10 = IO.ReadShort(data, 0x18);
            UnknownShort11 = IO.ReadShort(data, 0x1A);
            UnknownShort12 = IO.ReadShort(data, 0x1C);
            UnknownShort13 = IO.ReadShort(data, 0x1E);
            UnknownShort14 = IO.ReadShort(data, 0x20);
            UnknownShort15 = IO.ReadShort(data, 0x22);
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

    public enum TopicType : short
    {
        Main = 0x00,
        Sub = 0x01,
        Character = 0x03,
        Haruhi = 0x04,
    }
}
