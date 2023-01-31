using HaruhiChokuretsuLib.Util;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HaruhiChokuretsuLib.Archive.Data
{
    public enum BgType
    { 
        KINETIC_SCREEN = 0,
        TEX_TOP_BOTTOM = 1,
        TEX_TOP_BOTTOM_0A = 0x0A,
        TEX_BOTTOM_TILE_TOP = 0x0B,
        TEX_BOTTOM_TOP_WIDE = 0x0C,
        SINGLE_TEX = 0x0E,
    }

    public class BgTableFile : DataFile
    {
        public List<BgTableEntry> BgTableEntries { get; set; } = new();

        public override void Initialize(byte[] decompressedData, int offset, ILogger log)
        {
            _log = log;
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
            HashSet<string> names = new();
            string source = ".include \"GRPBIN.INC\"\n\n";
            source += $".set {nameof(BgType.KINETIC_SCREEN)}, {(int)BgType.KINETIC_SCREEN}\n";
            source += $".set {nameof(BgType.TEX_TOP_BOTTOM)}, {(int)BgType.TEX_TOP_BOTTOM}\n";
            source += $".set {nameof(BgType.TEX_TOP_BOTTOM_0A)}, {(int)BgType.TEX_TOP_BOTTOM_0A}\n";
            source += $".set {nameof(BgType.TEX_BOTTOM_TILE_TOP)}, {(int)BgType.TEX_BOTTOM_TILE_TOP}\n";
            source += $".set {nameof(BgType.TEX_BOTTOM_TOP_WIDE)}, {(int)BgType.TEX_BOTTOM_TOP_WIDE}\n";
            source += $".set {nameof(BgType.SINGLE_TEX)}, {(int)BgType.SINGLE_TEX}\n";
            source += "\n";

            source += ".word 1\n";
            source += ".word END_POINTERS\n";
            source += ".word FILE_START\n";
            source += ".word BGTBL\n";
            source += $".word {BgTableEntries.Count}\n\n";

            source += "FILE_START:\n";
            source += "BGTBL:\n";

            const int COMMENT_WIDTH = 24;
            for (int i = 0; i < BgTableEntries.Count; i++)
            {
                if (BgTableEntries[i].BgIndex1 != 0)
                {
                    string fileName1 = includes["GRPBIN"].First(inc => inc.Value == BgTableEntries[i].BgIndex1).Name;
                    string fileName2 = BgTableEntries[i].Type != BgType.SINGLE_TEX ? includes["GRPBIN"].First(inc => inc.Value == BgTableEntries[i].BgIndex2).Name : "0";
                    string bgName = fileName1[0..fileName1.LastIndexOf('_')];
                    string bgNameBackup = bgName;
                    for (int j = 1; names.Contains(bgName); j++)
                    {
                        bgName = $"{bgNameBackup}{j:D2}";
                    }
                    names.Add(bgName);

                    source += $"   .set {bgName}, 0x{i:X4}\n" +
                        $"   .word {BgTableEntries[i].Type}{string.Join(' ', new string[COMMENT_WIDTH - BgTableEntries[i].Type.ToString().Length + 1])}@ ENTRY TYPE\n" +
                        $"   .short {fileName1}{string.Join(' ', new string[COMMENT_WIDTH - fileName1.Length])}@ BG TOP\n" +
                        $"   .short {fileName2}{string.Join(' ', new string[COMMENT_WIDTH - fileName2.Length])}@ BG BOTTOM\n" +
                        $"   \n";
                }
                else
                {
                    source += $"   .set UNUSED{i:D3}, 0x{i:X4}\n" +
                        $"   .word 0\n" +
                        $"   .short 0\n" +
                        $"   .short 0\n" +
                        $"   \n";
                }
            }

            source += "END_POINTERS:\n";

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
