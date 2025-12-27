// These files are taken from Nitro Studio 2
// https://github.com/Gota7/NitroStudio2
// They have been modified to work with .NET 6.0
// Nitro Studio 2 is created by Gota 7
// It is not explicitly licensed, but given that its
// components are licensed under GPLv3, we can assume
// it is also GPLv3 compatible
using GotaSoundIO.IO;

namespace HaruhiChokuretsuLib.Audio.SDAT.SoundArchiveComponents;

/// <summary>
/// Sequence info.
/// </summary>
public class SequenceInfo : IReadable, IWriteable
{
    /// <summary>
    /// Name.
    /// </summary>
    public string Name;

    /// <summary>
    /// Entry index.
    /// </summary>
    public int Index;

    /// <summary>
    /// Force the file to be individualized.
    /// </summary>
    public bool ForceIndividualFile;

    /// <summary>
    /// File.
    /// </summary>
    public Sequence File;

    /// <summary>
    /// Bank.
    /// </summary>
    public BankInfo Bank;

    /// <summary>
    /// Player.
    /// </summary>
    public PlayerInfo Player;

    /// <summary>
    /// Volume.
    /// </summary>
    public byte Volume = 100;

    /// <summary>
    /// Channel priority.
    /// </summary>
    public byte ChannelPriority = 0x40;

    /// <summary>
    /// Player priority.
    /// </summary>
    public byte PlayerPriority = 0x40;

    /// <summary>
    /// Reading file ID.
    /// </summary>
    public uint ReadingFileId;

    /// <summary>
    /// Reading bank ID.
    /// </summary>
    public ushort ReadingBankId;

    /// <summary>
    /// Reading player ID.
    /// </summary>
    public byte ReadingPlayerId;

    /// <summary>
    /// Read the info.
    /// </summary>
    /// <param name="r">The reader.</param>
    public void Read(FileReader r)
    {
        ReadingFileId = r.ReadUInt32();
        ReadingBankId = r.ReadUInt16();
        Volume = r.ReadByte();
        ChannelPriority = r.ReadByte();
        PlayerPriority = r.ReadByte();
        ReadingPlayerId = r.ReadByte();
        r.ReadUInt16();
    }

    /// <summary>
    /// Write the info.
    /// </summary>
    /// <param name="w">The writer.</param>
    public void Write(FileWriter w)
    {
        w.Write(ReadingFileId);
        w.Write((ushort)(Bank != null ? Bank.Index : ReadingBankId));
        w.Write(Volume);
        w.Write(ChannelPriority);
        w.Write(PlayerPriority);
        w.Write((byte)(Player != null ? Player.Index : ReadingPlayerId));
        w.Write((ushort)0);
    }
}