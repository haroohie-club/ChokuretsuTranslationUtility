﻿using HaruhiChokuretsuLib.Util;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HaruhiChokuretsuLib.Archive.Data;

/// <summary>
/// Enum describing background image types in BGTBL.S
/// </summary>
public enum BgType
{ 
    /// <summary>
    /// Kinetic screen images are scrolling images used on the top screen
    /// </summary>
    KINETIC_SCREEN = 0,
    /// <summary>
    /// TEX_BG images are two-component visual novel background textures used on the bottom screen
    /// </summary>
    TEX_BG = 1,
    /// <summary>
    /// TEX_CG images are CGs two-component CGs used on the bottom screen
    /// </summary>
    TEX_CG = 0x0A,
    /// <summary>
    /// Dual screen CGs are two-component images displayed on both screens capable of being scrolled up and down
    /// </summary>
    TEX_CG_DUAL_SCREEN = 0x0B,
    /// <summary>
    /// Wide CGs are two-component CGs capable of being scrolled left and right
    /// </summary>
    TEX_CG_WIDE = 0x0C,
    /// <summary>
    /// Single CGs are single-component CGs capable of being scrolled up and down
    /// </summary>
    TEX_CG_SINGLE = 0x0E,
}

/// <summary>
/// Representation of BGTBL.S in dat.bin
/// </summary>
public class BgTableFile : DataFile
{
    /// <summary>
    /// List of BG table entries
    /// </summary>
    public List<BgTableEntry> BgTableEntries { get; set; } = [];

    /// <inheritdoc/>
    public override void Initialize(byte[] decompressedData, int offset, ILogger log)
    {
        Log = log;
        Offset = offset;
        Data = [.. decompressedData];

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

    /// <inheritdoc/>
    public override string GetSource(Dictionary<string, IncludeEntry[]> includes)
    {
        HashSet<string> names = [];
        string source = ".include \"GRPBIN.INC\"\n\n";
        source += $".set {nameof(BgType.KINETIC_SCREEN)}, {(int)BgType.KINETIC_SCREEN}\n";
        source += $".set {nameof(BgType.TEX_BG)}, {(int)BgType.TEX_BG}\n";
        source += $".set {nameof(BgType.TEX_CG)}, {(int)BgType.TEX_CG}\n";
        source += $".set {nameof(BgType.TEX_CG_DUAL_SCREEN)}, {(int)BgType.TEX_CG_DUAL_SCREEN}\n";
        source += $".set {nameof(BgType.TEX_CG_WIDE)}, {(int)BgType.TEX_CG_WIDE}\n";
        source += $".set {nameof(BgType.TEX_CG_SINGLE)}, {(int)BgType.TEX_CG_SINGLE}\n";
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
                string fileName2 = BgTableEntries[i].Type != BgType.TEX_CG_SINGLE ? includes["GRPBIN"].First(inc => inc.Value == BgTableEntries[i].BgIndex2).Name : "0";
                string bgName = fileName1[..fileName1.LastIndexOf('_')];
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

/// <summary>
/// A struct representaiton of a BG Table entry
/// </summary>
public struct BgTableEntry
{
    /// <summary>
    /// The BG Type
    /// </summary>
    public BgType Type;
    /// <summary>
    /// The first background file index (indexes into grp.bin)
    /// </summary>
    public short BgIndex1;
    /// <summary>
    /// The second background file index (indexes into grp.bin)
    /// </summary>
    public short BgIndex2;
}