using HaruhiChokuretsuLib.Util;
using System.Collections.Generic;
using System.Text;

namespace HaruhiChokuretsuLib.Archive.Data;

/// <summary>
/// Representation of QMAP.S
/// </summary>
public class QMapFile : DataFile
{
    /// <summary>
    /// List of QMaps in the file
    /// </summary>
    public List<QMap> QMaps { get; set; } = [];

    /// <inheritdoc/>
    public override void Initialize(byte[] decompressedData, int offset, ILogger log)
    {
        Log = log;
        int numSections = IO.ReadInt(decompressedData, 0);

        if (numSections != 1)
        {
            Log.LogError($"QMAPS file should have only one section; {numSections} specified!");
            return;
        }

        int sectionOffset = IO.ReadInt(decompressedData, 0x0C);
        int sectionCount = IO.ReadInt(decompressedData, 0x10);

        for (int i = 0; i < sectionCount - 1; i++)
        {
            int qmapOffset = IO.ReadInt(decompressedData, sectionOffset + i * 8);
            QMaps.Add(new()
            {
                Name = IO.ReadAsciiString(decompressedData, qmapOffset),
                Slg = IO.ReadInt(decompressedData, sectionOffset + i * 8 + 4) != 0,
            });
        }
    }

    /// <inheritdoc/>
    public override string GetSource(Dictionary<string, IncludeEntry[]> includes)
    {
        StringBuilder sb = new();

        sb.AppendLine(".word 1");
        sb.AppendLine(".word END_POINTERS");
        sb.AppendLine(".word FILE_START");
        sb.AppendLine(".word QMAP_POINTERS");
        sb.AppendLine($".word {QMaps.Count + 1}");
        sb.AppendLine();

        sb.AppendLine("FILE_START:");
        sb.AppendLine("QMAP_POINTERS:");
        int endPointerCount = 0;
        for (int i = 0; i < QMaps.Count; i++)
        {
            sb.AppendLine($"POINTER{endPointerCount++:D2}: .word QMAP{i:D2}");
            sb.AppendLine($".word {(QMaps[i].Slg ? 1 : 0)}");
        }
        sb.AppendLine(".word 0");
        sb.AppendLine(".word 0");
        sb.AppendLine();

        for (int i = 0; i < QMaps.Count; i++)
        {
            sb.AppendLine($"QMAP{i:D2}: .string \"{QMaps[i].Name}\"");
            sb.AsmPadString(QMaps[i].Name, Encoding.ASCII);
        }
        sb.AppendLine();

        sb.AppendLine("END_POINTERS:");
        sb.AppendLine($".word {endPointerCount}");
        for (int i = 0; i < endPointerCount; i++)
        {
            sb.AppendLine($".word POINTER{i:D2}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// A struct representing a "QMap" (a map file in dat.bin)
    /// </summary>
    public struct QMap
    {
        /// <summary>
        /// The name of the map
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// "Singularity" (if true, the map is a puzzle phase map)
        /// </summary>
        public bool Slg { get; set; }
    }
}