using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HaruhiChokuretsuLib.Archive.Data
{
    public enum BgType
    { 
        UNKNOWN00 = 0,
        TEX_TOP_BOTTOM = 1,
        TEX_TOP_BOTTOM_0A = 0x0A,
        TEX_BOTTOM_TILE_TOP = 0x0B,
        TEX_BOTTOM_TOP_WIDE = 0x0C,
        SINGLE_TEX = 0x0E,
    }

    public class BgTable : DataFile
    {
        public List<BgTableEntry> BgTableEntries { get; set; } = new();

        public override void Initialize(byte[] decompressedData, int offset)
        {
            Offset = offset;
            Data = decompressedData.ToList();

            int startIndex = BitConverter.ToInt32(Data.Skip(0x0C).Take(4).ToArray());
            int numBgs = BitConverter.ToInt32(Data.Skip(0x10).Take(4).ToArray());

            for (int i = 0; i < numBgs; i++)
            {
                BgTableEntries.Add(new()
                {
                    Type = (BgType)BitConverter.ToInt32(Data.Skip(startIndex + i * 8).Take(4).ToArray()),
                    BgIndex1 = BitConverter.ToInt16(Data.Skip(startIndex + i * 8 + 4).Take(2).ToArray()),
                    BgIndex2 = BitConverter.ToInt16(Data.Skip(startIndex + i * 8 + 6).Take(2).ToArray()),
                });
            }
        }

        public override string GetSource(Dictionary<string, IncludeEntry[]> includes)
        {
            string source = ".include \"GRPBIN.INC\"\n\n";
            source += $".set {nameof(BgType.UNKNOWN00)}, {(int)BgType.UNKNOWN00}\n";
            source += $".set {nameof(BgType.TEX_TOP_BOTTOM)}, {(int)BgType.TEX_TOP_BOTTOM}\n";
            source += $".set {nameof(BgType.TEX_TOP_BOTTOM_0A)}, {(int)BgType.TEX_TOP_BOTTOM_0A}\n";
            source += $".set {nameof(BgType.TEX_BOTTOM_TILE_TOP)}, {(int)BgType.TEX_BOTTOM_TILE_TOP}\n";
            source += $".set {nameof(BgType.TEX_BOTTOM_TOP_WIDE)}, {(int)BgType.TEX_BOTTOM_TOP_WIDE}\n";
            source += $".set {nameof(BgType.SINGLE_TEX)}, {(int)BgType.SINGLE_TEX}\n";
            source += "\n";

            source += "BGTBL:\n";

            const int COMMENT_WIDTH = 24;
            for (int i = 0; i < BgTableEntries.Count; i++)
            {
                if (BgTableEntries[i].BgIndex1 != 0)
                {
                    string fileName1 = includes["GRPBIN"].First(inc => inc.Value == BgTableEntries[i].BgIndex1).Name;
                    string fileName2 = BgTableEntries[i].Type != BgType.SINGLE_TEX ? includes["GRPBIN"].First(inc => inc.Value == BgTableEntries[i].BgIndex2).Name : "0";
                    string macroName = fileName1[0..fileName1.LastIndexOf('_')];

                    source += $"    {macroName}:{string.Join(' ', new string[COMMENT_WIDTH - macroName.Length + 10])}@ 0x{i:X4}\n" +
                        $"        .word {BgTableEntries[i].Type}{string.Join(' ', new string[COMMENT_WIDTH - BgTableEntries[i].Type.ToString().Length + 1])}@ ENTRY TYPE\n" +
                        $"        .short {fileName1}{string.Join(' ', new string[COMMENT_WIDTH - fileName1.Length])}@ BG TOP\n" +
                        $"        .short {fileName2}{string.Join(' ', new string[COMMENT_WIDTH - fileName2.Length])}@ BG BOTTOM\n" +
                        $"    \n";
                }
                else
                {
                    source += $"    UNUSED{i:D2}:\n" +
                        $"        .word 0\n" +
                        $"        .short 0\n" +
                        $"        .short 0\n" +
                        $"    \n";
                }
            }

            return source;
        }
    }

    public struct BgTableEntry
    {
        public BgType Type;
        public short BgIndex1;
        public short BgIndex2;
    }
}
