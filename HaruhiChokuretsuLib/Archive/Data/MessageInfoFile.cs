using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Util;
using System.Collections.Generic;
using System.Text;

namespace HaruhiChokuretsuLib.Archive.Data
{
    public class MessageInfoFile : DataFile
    {
        public List<MessageInfo> MessageInfos { get; set; } = new();

        public override void Initialize(byte[] decompressedData, int offset, ILogger log)
        {
            _log = log;

            int numSections = IO.ReadInt(decompressedData, 0);
            if (numSections != 1)
            {
                _log.LogError($"MESSAGEINFO file should only have 1 section; {numSections} specified.");
                return;
            }

            int sectionStart = IO.ReadInt(decompressedData, 0x0C);
            int sectionCount = IO.ReadInt(decompressedData, 0x10);

            for (int i = 0; i < sectionCount - 1; i++)
            {
                MessageInfos.Add(new()
                {
                    Character = (Speaker)IO.ReadShort(decompressedData, sectionStart + i * 0x08),
                    VoiceFont = IO.ReadShort(decompressedData, sectionStart + i * 0x08 + 2),
                    TextTimer = IO.ReadShort(decompressedData, sectionStart + i * 0x08 + 4),
                    Unknown = IO.ReadShort(decompressedData, sectionStart + i * 0x08 + 6),
                });
            }
        }

        public override string GetSource(Dictionary<string, IncludeEntry[]> includes)
        {
            StringBuilder sb = new();

            for (int i = 1; i < 0x1A; i++)
            {
                sb.AppendLine($".set {(Speaker)i}, {i}");
            }
            sb.AppendLine();

            sb.AppendLine(".word 1");
            sb.AppendLine(".word END_POINTERS");
            sb.AppendLine(".word FILE_START");
            sb.AppendLine(".word MESSINFOS");
            sb.AppendLine($".word {MessageInfos.Count + 1}");
            sb.AppendLine();

            sb.AppendLine("FILE_START:");
            sb.AppendLine("MESSINFOS:");
            for (int i = 0; i < MessageInfos.Count; i++)
            {
                sb.AppendLine($".short {MessageInfos[i].Character}");
                sb.AppendLine($"   .short {MessageInfos[i].VoiceFont}");
                sb.AppendLine($"   .short {MessageInfos[i].TextTimer}");
                sb.AppendLine($"   .short {MessageInfos[i].Unknown}");
            }
            sb.AppendLine(".word 0");
            sb.AppendLine(".word 0");
            sb.AppendLine();

            sb.AppendLine("END_POINTERS:");
            sb.AppendLine(".word 0");

            return sb.ToString();
        }
    }

    public struct MessageInfo
    {
        public Speaker Character { get; set; }
        public short VoiceFont { get; set; }
        public short TextTimer { get; set; }
        public short Unknown { get; set; }
    }
}
