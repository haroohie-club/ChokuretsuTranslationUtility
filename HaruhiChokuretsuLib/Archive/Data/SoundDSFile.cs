using HaruhiChokuretsuLib.Util;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HaruhiChokuretsuLib.Archive.Data
{
    public class SoundDSFile : DataFile
    {
        public List<int> UnknownSection1 { get; set; } = new();
        public List<UnknownAudio02Entry> UnknownSection2 { get; set; } = new();
        public List<SfxEntry> SfxSection { get; set; } = new();
        public List<short> UnknownSection4 { get; set; } = new();
        public List<string> BgmSection { get; set; } = new();
        public List<string> VoiceSection { get; set; } = new();
        public List<int> UnknownSection7 { get; set; } = new();
        public List<int> UnknownSection8 { get; set; } = new();
        public List<UnknownAudio09Entry> UnknownSection9 { get; set; } = new();
        // Section 10 is a pointers section

        public override void Initialize(byte[] decompressedData, int offset, ILogger log)
        {
            _log = log;

            int numSections = IO.ReadInt(decompressedData, 0);
            if (numSections != 10)
            {
                _log.LogError($"DS sound file should have 10 sections, {numSections} detected");
                return;
            }

            Data = decompressedData.ToList();
            Offset = offset;

            int unknownSection1Pointer = IO.ReadInt(Data, 0x0C);
            int unknownSection1Count = IO.ReadInt(Data, 0x10);
            for (int i = 0; i < unknownSection1Count; i++)
            {
                UnknownSection1.Add(IO.ReadInt(Data, unknownSection1Pointer + 0x04 * i));
            }

            int unknownSection2Pointer = IO.ReadInt(Data, 0x14);
            int unknownSection2Count = IO.ReadInt(Data, 0x18);
            for (int i = 0; i < unknownSection2Count; i++)
            {
                UnknownSection2.Add(new(Data.Skip(unknownSection2Pointer + 0x0C * i).Take(0x0C)));
            }

            int sfxSectionPointer = IO.ReadInt(Data, 0x1C);
            int sfxSectionCount = IO.ReadInt(Data, 0x20);
            for (int i = 0; i < sfxSectionCount; i++)
            {
                SfxSection.Add(new(Data.Skip(sfxSectionPointer + 0x14 * i).Take(0x14)));
            }

            int unknownSection4Pointer = IO.ReadInt(Data, 0x24);
            int unknownSection4Count = IO.ReadInt(Data, 0x28);
            for (int i = 0; i < unknownSection4Count; i++)
            {
                UnknownSection4.Add(IO.ReadShort(Data, unknownSection4Pointer + 0x02 * i));
            }

            int bgmSectionPointer = IO.ReadInt(Data, 0x2C);
            int bgmSectionCount = IO.ReadInt(Data, 0x30);
            for (int i = 0; i < bgmSectionCount; i++)
            {
                int bgmPointer = IO.ReadInt(Data, bgmSectionPointer + 0x04 * i);
                if (bgmPointer == 0)
                {
                    BgmSection.Add(null);
                    continue;
                }
                BgmSection.Add(IO.ReadAsciiString(Data, bgmPointer));
            }

            int vceSectionPointer = IO.ReadInt(Data, 0x34);
            int vceSectionCount = IO.ReadInt(Data, 0x38);
            for (int i = 0; i < vceSectionCount; i++)
            {
                int vcePointer = IO.ReadInt(Data, vceSectionPointer + 0x04 * i);
                if (vcePointer == 0)
                {
                    VoiceSection.Add(null);
                    continue;
                }
                VoiceSection.Add(IO.ReadAsciiString(Data, vcePointer));
            }

            int unknownSection7Pointer = IO.ReadInt(Data, 0x3C);
            int unknownSection7Count = IO.ReadInt(Data, 0x40);
            for (int i = 0; i < unknownSection7Count; i++)
            {
                UnknownSection7.Add(IO.ReadInt(Data, unknownSection7Pointer + 0x04 * i));
            }

            int unknownSection8Pointer = IO.ReadInt(Data, 0x44);
            int unknownSection8Count = IO.ReadInt(Data, 0x48);
            for (int i = 0; i < unknownSection8Count; i++)
            {
                UnknownSection8.Add(IO.ReadInt(Data, unknownSection8Pointer + 0x04 * i));
            }

            int unknownSection9Pointer = IO.ReadInt(Data, 0x4C);
            int unknownSection9Count = IO.ReadInt(Data, 0x50);
            for (int i = 0; i < unknownSection9Count; i++)
            {
                UnknownSection9.Add(new(Data.Skip(unknownSection9Pointer + 0x08 * i).Take(0x08)));
            }
        }

        public override string GetSource(Dictionary<string, IncludeEntry[]> includes)
        {
            StringBuilder sb = new();

            int numPointers = 0;

            sb.AppendLine(".word 10");
            sb.AppendLine(".word END_POINTERS");
            sb.AppendLine(".word FILE_START");
            sb.AppendLine(".word UNKNOWNSECTION1");
            sb.AppendLine($".word {UnknownSection1.Count}");
            sb.AppendLine(".word UNKNOWNSECTION2");
            sb.AppendLine($".word {UnknownSection2.Count}");
            sb.AppendLine(".word SFX_SECTION");
            sb.AppendLine($".word {SfxSection.Count}");
            sb.AppendLine(".word UNKNOWNSECTION4");
            sb.AppendLine($".word {UnknownSection4.Count}");
            sb.AppendLine(".word BGM_SECTION");
            sb.AppendLine($".word {BgmSection.Count}");
            sb.AppendLine(".word VOICE_SECTION");
            sb.AppendLine($".word {VoiceSection.Count}");
            sb.AppendLine(".word UNKNOWNSECTION7");
            sb.AppendLine($".word {UnknownSection7.Count}");
            sb.AppendLine(".word UNKNOWNSECTION8");
            sb.AppendLine($".word {UnknownSection8.Count}");
            sb.AppendLine(".word UNKNOWNSECTION9");
            sb.AppendLine($".word {UnknownSection9.Count}");
            sb.AppendLine(".word POINTERS_SECTION");
            sb.AppendLine($".word 1");
            sb.AppendLine();

            sb.AppendLine("FILE_START:");

            sb.AppendLine("UNKNOWNSECTION1:");
            foreach (int section1Entry in UnknownSection1)
            {
                sb.AppendLine($".word {section1Entry}");
            }
            sb.AppendLine(".skip 4");

            sb.AppendLine("UNKNOWNSECTION2:");
            foreach (UnknownAudio02Entry section2Entry in UnknownSection2)
            {
                sb.AppendLine(section2Entry.GetSource());
            }

            sb.AppendLine("SFX_SECTION:");
            foreach (SfxEntry sfxEntry in SfxSection)
            {
                sb.AppendLine(sfxEntry.GetSource());
            }

            sb.AppendLine("UNKNOWNSECTION4:");
            foreach (short section4Entry in UnknownSection4)
            {
                sb.AppendLine($".short {section4Entry}");
            }
            if (UnknownSection4.Count % 2 == 1)
            {
                sb.AppendLine(".skip 2");
            }

            sb.AppendLine("BGM_SECTION:");
            for (int i = 0; i < BgmSection.Count; i++)
            {
                if (string.IsNullOrEmpty(BgmSection[i]))
                {
                    sb.AppendLine(".word 0");
                    continue;
                }
                sb.AppendLine($"POINTER{numPointers++}: .word BGM{i:D3}");
            }
            for (int i = 0; i < BgmSection.Count; i++)
            {
                if (string.IsNullOrEmpty(BgmSection[i]))
                {
                    continue;
                }
                sb.AppendLine($"BGM{i:D3}: .string \"{BgmSection[i]}\"");
                sb.AsmPadString(BgmSection[i], Encoding.ASCII);
            }

            sb.AppendLine("VOICE_SECTION:");
            for (int i = 0; i < VoiceSection.Count; i++)
            {
                if (string.IsNullOrEmpty(VoiceSection[i]))
                {
                    sb.AppendLine(".word 0");
                    continue;
                }
                sb.AppendLine($"POINTER{numPointers++}: .word VCE{i:D4}");
            }
            for (int i = 0; i < VoiceSection.Count; i++)
            {
                if (string.IsNullOrEmpty(VoiceSection[i]))
                {
                    continue;
                }
                sb.AppendLine($"VCE{i:D4}: .string \"{VoiceSection[i]}\"");
                sb.AsmPadString(VoiceSection[i], Encoding.ASCII);
            }

            sb.AppendLine("UNKNOWNSECTION7:");
            foreach (int section7Entry in UnknownSection7)
            {
                sb.AppendLine($".short {section7Entry}");
            }
            if (UnknownSection7.Count % 2 == 1)
            {
                sb.AppendLine(".skip 2");
            }

            sb.AppendLine("UNKNOWNSECTION8:");
            foreach (int section8Entry in UnknownSection8)
            {
                sb.AppendLine($".short {section8Entry}");
            }
            if (UnknownSection8.Count % 2 == 1)
            {
                sb.AppendLine(".skip 2");
            }

            sb.AppendLine("UNKNOWNSECTION9:");
            foreach (UnknownAudio09Entry section9Entry in UnknownSection9)
            {
                sb.AppendLine(section9Entry.GetSource());
            }

            sb.AppendLine("POINTERS_SECTION:");
            sb.AppendLine($"POINTER{numPointers++}: .word UNKNOWNSECTION2");
            sb.AppendLine($"POINTER{numPointers++}: .word SFX_SECTION");
            sb.AppendLine($"POINTER{numPointers++}: .word UNKNOWNSECTION4");
            sb.AppendLine($".short {SfxSection.Count - 1}");
            sb.AppendLine($".short {UnknownSection4.Count - 1}");

            sb.AppendLine("END_POINTERS:");
            sb.AppendLine($".word {numPointers}");
            for (int i = 0; i < numPointers; i++)
            {
                sb.AppendLine($".word POINTER{i}");
            }

            return sb.ToString();
        }
    }

    public class UnknownAudio02Entry
    {
        public short Unknown01 { get; set; }
        public short Unknown02 { get; set; }
        public short Unknown03 { get; set; }
        public short Unknown04 { get; set; }
        public short Unknown05 { get; set; }

        public UnknownAudio02Entry(IEnumerable<byte> data)
        {
            Unknown01 = IO.ReadShort(data, 0x00);
            Unknown02 = IO.ReadShort(data, 0x02);
            Unknown03 = IO.ReadShort(data, 0x04);
            Unknown04 = IO.ReadShort(data, 0x06);
            Unknown05 = IO.ReadShort(data, 0x08);
        }

        public string GetSource()
        {
            StringBuilder sb = new();

            sb.AppendLine($".short {Unknown01}");
            sb.AppendLine($".short {Unknown02}");
            sb.AppendLine($".short {Unknown03}");
            sb.AppendLine($".short {Unknown04}");
            sb.AppendLine($".short {Unknown05}");
            sb.AppendLine($".skip 2");

            return sb.ToString();
        }
    }

    public class SfxEntry
    {
        public short SequenceArchive { get; set; }
        public short Index { get; set; }
        public short Volume { get; set; }
        public short Unknown4 { get; set; }
        public int Unknown5 { get; set; }
        public int Unknown6 { get; set; }
        public int Unknown7 { get; set; }

        public SfxEntry(IEnumerable<byte> data)
        {
            SequenceArchive = IO.ReadShort(data, 0x00);
            Index = IO.ReadShort(data, 0x02);
            Volume = IO.ReadShort(data, 0x04);
            Unknown4 = IO.ReadShort(data, 0x06);
            Unknown5 = IO.ReadInt(data, 0x08);
            Unknown6 = IO.ReadInt(data, 0x0C);
            Unknown7 = IO.ReadInt(data, 0x10);
        }

        public string GetSource()
        {
            StringBuilder sb = new();

            sb.AppendLine($".short {SequenceArchive}");
            sb.AppendLine($".short {Index}");
            sb.AppendLine($".short {Volume}");
            sb.AppendLine($".short {Unknown4}");
            sb.AppendLine($".word {Unknown5}");
            sb.AppendLine($".word {Unknown6}");
            sb.AppendLine($".word {Unknown7}");

            return sb.ToString();
        }
    }

    public class UnknownAudio09Entry
    {
        public short Unknown01 { get; set; }
        public short Unknown02 { get; set; }
        public short Unknown03 { get; set; }
        public short Unknown04 { get; set; }

        public UnknownAudio09Entry(IEnumerable<byte> data)
        {
            Unknown01 = IO.ReadShort(data, 0x00);
            Unknown02 = IO.ReadShort(data, 0x02);
            Unknown03 = IO.ReadShort(data, 0x04);
            Unknown04 = IO.ReadShort(data, 0x06);
        }

        public string GetSource()
        {
            StringBuilder sb = new();

            sb.AppendLine($".short {Unknown01}");
            sb.AppendLine($".short {Unknown02}");
            sb.AppendLine($".short {Unknown03}");
            sb.AppendLine($".short {Unknown04}");

            return sb.ToString();
        }
    }
}
