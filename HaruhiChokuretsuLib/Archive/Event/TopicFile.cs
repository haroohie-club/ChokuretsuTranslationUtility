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
                TopicStructs.Add(new(i, DialogueLines[i].Text, Data.Skip(0x18 + i / 2 * 0x24).Take(0x24).ToArray()));
            }
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
}
