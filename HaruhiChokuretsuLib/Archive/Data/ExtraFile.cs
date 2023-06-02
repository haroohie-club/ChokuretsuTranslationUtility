using HaruhiChokuretsuLib.Util;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HaruhiChokuretsuLib.Archive.Data
{
    public class ExtraFile : DataFile
    {
        public List<BgmStruct> Bgms { get; set; } = new();
        public List<CgStruct> Cgs { get; set; } = new();

        public override void Initialize(byte[] decompressedData, int offset, ILogger log)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            int numSections = IO.ReadInt(decompressedData, 0);
            if (numSections != 3)
            {
                _log.LogError($"Extras file should only have 3 sections, {numSections} detected.");
                return;
            }

            int settingsOffset = IO.ReadInt(decompressedData, 0x1C);
            short numBgms = IO.ReadShort(decompressedData, settingsOffset);
            short numCgs = IO.ReadShort(decompressedData, settingsOffset + 2);
            int bgmsOffset = IO.ReadInt(decompressedData, settingsOffset + 4);
            int cgsOffset = IO.ReadInt(decompressedData, settingsOffset + 8);

            for (int i = 0; i < numBgms; i++)
            {
                Bgms.Add(new()
                {
                    Index = IO.ReadShort(decompressedData, bgmsOffset + i * 8),
                    Flag = IO.ReadShort(decompressedData, bgmsOffset + i * 8 + 2),
                    Name = Encoding.GetEncoding("Shift-JIS").GetString(decompressedData.Skip(IO.ReadInt(decompressedData, bgmsOffset + i * 8 + 4)).TakeWhile(b => b != 0).ToArray()),
                });
            }

            for (int i = 0; i < numCgs; i++)
            {
                Cgs.Add(new()
                {
                    BgId = IO.ReadShort(decompressedData, cgsOffset + i * 12),
                    Flag = IO.ReadShort(decompressedData, cgsOffset + i * 12 + 2),
                    Unknown04 = IO.ReadInt(decompressedData, cgsOffset + i * 12 + 4),
                    Name = Encoding.GetEncoding("Shift-JIS").GetString(decompressedData.Skip(IO.ReadInt(decompressedData, cgsOffset + i * 12 + 8)).TakeWhile(b => b != 0).ToArray()),
                });
            }
        }

        public override string GetSource(Dictionary<string, IncludeEntry[]> includes)
        {
            StringBuilder sb = new();

            sb.AppendLine(".word 3");
            sb.AppendLine(".word END_POINTERS");
            sb.AppendLine(".word FILE_START");
            sb.AppendLine(".word BGMS");
            sb.AppendLine($".word {Bgms.Count + 1}");
            sb.AppendLine(".word CGS");
            sb.AppendLine($".word {Cgs.Count + 1}");
            sb.AppendLine(".word SETTINGS");
            sb.AppendLine(".word 1");
            sb.AppendLine();
            sb.AppendLine("FILE_START:");
            int endPointers = 0;

            sb.AppendLine("BGMS:");
            foreach (BgmStruct bgm in Bgms)
            {
                sb.AppendLine($".short {bgm.Index}");
                sb.AppendLine($"   .short {bgm.Flag}");
                sb.AppendLine($"   POINTER{endPointers++:D3}: .word BGM{bgm.Index:D3}");
            }
            sb.AppendLine(".skip 8");
            foreach (BgmStruct bgm in Bgms)
            {
                sb.AppendLine($"BGM{bgm.Index:D3}: .string \"{bgm.Name.EscapeShiftJIS()}\"");
                sb.AsmPadString(bgm.Name, Encoding.GetEncoding("Shift-JIS"));
            }
            sb.AppendLine();

            sb.AppendLine("CGS:");
            foreach (CgStruct cg in Cgs)
            {
                sb.AppendLine($".short {cg.BgId}");
                sb.AppendLine($"   .short {cg.Flag}");
                sb.AppendLine($"   .word {cg.Unknown04}");
                sb.AppendLine($"   POINTER{endPointers++:D3}: .word CG{cg.BgId:D3}");
            }
            sb.AppendLine(".skip 12");
            foreach (CgStruct cg in Cgs)
            {
                sb.AppendLine($"CG{cg.BgId:D3}: .string \"{cg.Name.EscapeShiftJIS()}\"");
                sb.AsmPadString(cg.Name, Encoding.GetEncoding("Shift-JIS"));
            }
            sb.AppendLine();

            sb.AppendLine("SETTINGS:");
            sb.AppendLine($".short {Bgms.Count}");
            sb.AppendLine($".short {Cgs.Count}");
            sb.AppendLine($"POINTER{endPointers++:D3}: .word BGMS");
            sb.AppendLine($"POINTER{endPointers++:D3}: .word CGS");
            sb.AppendLine();

            sb.AppendLine("END_POINTERS:");
            sb.AppendLine($".word {endPointers}");
            for (int i = 0; i < endPointers; i++)
            {
                sb.AppendLine($".word POINTER{i:D3}");
            }

            return sb.ToString();
        }
    }

    public class BgmStruct
    {
        public short Index { get; set; }
        public short Flag { get; set; }
        public string Name { get; set; }
    }

    public class CgStruct
    {
        public short BgId { get; set; }
        public short Flag { get; set; }
        public int Unknown04 { get; set; }
        public string Name { get; set; }
    }
}
