using HaruhiChokuretsuLib.Util;
using System.Collections.Generic;
using System.Text;

namespace HaruhiChokuretsuLib.Archive.Data;

/// <summary>
/// Representation of ITEM.S in dat.bin
/// </summary>
public class ItemFile : DataFile
{
    /// <summary>
    /// List of grp.bin indices of the item textures
    /// </summary>
    public List<short> Items { get; set; } = [];

    /// <inheritdoc/>
    public override void Initialize(byte[] decompressedData, int offset, ILogger log)
    {
        Log = log;
        Offset = offset;
        Data = [.. decompressedData];

        int startIndex = IO.ReadInt(decompressedData, 0x0C);
        int numItems = IO.ReadInt(decompressedData, 0x10);

        for (int i = 0; i < numItems; i++)
        {
            Items.Add(IO.ReadShort(decompressedData, startIndex + i * 2));
        }
    }

    /// <inheritdoc/>
    public override string GetSource(Dictionary<string, IncludeEntry[]> includes)
    {
        StringBuilder sb = new();
        sb.AppendLine(".include \"GRPBIN.INC\"");
        sb.AppendLine();
        sb.AppendLine(".word 1");
        sb.AppendLine(".word END_POINTERS");
        sb.AppendLine(".word FILE_START");
        sb.AppendLine(".word ITEMS");
        sb.AppendLine($".word {Items.Count}");
        sb.AppendLine("FILE_START:");
        sb.AppendLine("ITEMS:");
        foreach (short item in Items)
        {
            sb.AppendLine($"   .short {(item > 0 ? includes["GRPBIN"][item - 1].Name : 0)}");
        }
        if ((Items.Count * 2) % 4 != 0)
        {
            sb.AppendLine("   .short 0");
        }
        sb.AppendLine("END_POINTERS:");
        sb.AppendLine(".word 0");

        return sb.ToString();
    }
}