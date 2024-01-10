using HaruhiChokuretsuLib.Util;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HaruhiChokuretsuLib.Archive.Data
{
    /// <summary>
    /// A representation of EXTRA.S in dat.bin
    /// </summary>
    public class ExtraFile : DataFile
    {
        /// <summary>
        /// The list of BGM extra data
        /// </summary>
        public List<BgmExtraData> Bgms { get; set; } = [];
        /// <summary>
        /// The list of CG extra data
        /// </summary>
        public List<CgExtraData> Cgs { get; set; } = [];

        /// <inheritdoc/>
        public override void Initialize(byte[] decompressedData, int offset, ILogger log)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            int numSections = IO.ReadInt(decompressedData, 0);
            if (numSections != 3)
            {
                Log.LogError($"Extras file should only have 3 sections, {numSections} detected.");
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

        /// <inheritdoc/>
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
            foreach (BgmExtraData bgm in Bgms)
            {
                sb.AppendLine($".short {bgm.Index}");
                sb.AppendLine($"   .short {bgm.Flag}");
                sb.AppendLine($"   POINTER{endPointers++:D3}: .word BGM{bgm.Index:D3}");
            }
            sb.AppendLine(".skip 8");
            foreach (BgmExtraData bgm in Bgms)
            {
                sb.AppendLine($"BGM{bgm.Index:D3}: .string \"{bgm.Name.EscapeShiftJIS()}\"");
                sb.AsmPadString(bgm.Name, Encoding.GetEncoding("Shift-JIS"));
            }
            sb.AppendLine();

            sb.AppendLine("CGS:");
            foreach (CgExtraData cg in Cgs)
            {
                sb.AppendLine($".short {cg.BgId}");
                sb.AppendLine($"   .short {cg.Flag}");
                sb.AppendLine($"   .word {cg.Unknown04}");
                sb.AppendLine($"   POINTER{endPointers++:D3}: .word CG{cg.BgId:D3}");
            }
            sb.AppendLine(".skip 12");
            foreach (CgExtraData cg in Cgs)
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

    /// <summary>
    /// The extras data for a particular background music track
    /// </summary>
    public class BgmExtraData
    {
        /// <summary>
        /// The index of the BGM in SND_DS.S
        /// </summary>
        public short Index { get; set; }
        /// <summary>
        /// The flag indicating that this BGM has been encountered in-game
        /// </summary>
        public short Flag { get; set; }
        /// <summary>
        /// The name of this BGM as displayed in the extras mode's BGM viewer
        /// </summary>
        public string Name { get; set; }
    }

    /// <summary>
    /// The extras data for a particular CG
    /// </summary>
    public class CgExtraData
    {
        /// <summary>
        /// The ID of the CG as defined in BGTBL.S
        /// </summary>
        public short BgId { get; set; }
        /// <summary>
        /// The flag indicating that this CG has been encountered in game
        /// </summary>
        public short Flag { get; set; }
        /// <summary>
        /// Unknown
        /// </summary>
        public int Unknown04 { get; set; }
        /// <summary>
        /// The name of the CG as displayed in the extras mode's CG viewer
        /// </summary>
        public string Name { get; set; }
    }
}
