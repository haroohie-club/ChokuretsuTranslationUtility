using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Util;
using System.Collections.Generic;
using System.Text;

namespace HaruhiChokuretsuLib.Archive.Data;

/// <summary>
/// Representation of MESSINFO.S in dat.bin
/// </summary>
public class MessageInfoFile : DataFile
{
    /// <summary>
    /// The list of message info entries in the file
    /// </summary>
    public List<MessageInfo> MessageInfos { get; set; } = [];

    /// <inheritdoc/>
    public override void Initialize(byte[] decompressedData, int offset, ILogger log)
    {
        Log = log;

        int numSections = IO.ReadInt(decompressedData, 0);
        if (numSections != 1)
        {
            Log.LogError($"MESSAGEINFO file should only have 1 section; {numSections} specified.");
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

    /// <inheritdoc/>
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

/// <summary>
/// A representation of a "message info" entry which defines a particular speaker's settings
/// </summary>
public class MessageInfo
{
    /// <summary>
    /// The index of the character (represented here by the Speaker enum)
    /// </summary>
    public Speaker Character { get; set; }
    /// <summary>
    /// The SND_DS.S index of the voice font to use
    /// </summary>
    public short VoiceFont { get; set; }
    /// <summary>
    /// The length of the text timer (the timer that ticks down to when the next character should be displayed on-screen); 
    /// the lower this value, the faster text is displayed
    /// </summary>
    public short TextTimer { get; set; }
    /// <summary>
    /// Unknown
    /// </summary>
    public short Unknown { get; set; }
}