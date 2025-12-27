// These files are taken from Nitro Studio 2
// https://github.com/Gota7/NitroStudio2
// They have been modified to work with .NET 6.0
// Nitro Studio 2 is created by Gota 7
// It is not explicitly licensed, but given that its
// components are licensed under GPLv3, we can assume
// it is also GPLv3 compatible
using GotaSoundIO.IO;
using System.Linq;

namespace HaruhiChokuretsuLib.Audio.SDAT.SoundArchiveComponents;

/// <summary>
/// Player info.
/// </summary>
public class PlayerInfo : IReadable, IWriteable
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
    /// Sequence max.
    /// </summary>
    public ushort SequenceMax;

    /// <summary>
    /// Channel flags.
    /// </summary>
    public bool[] ChannelFlags = new bool[16];

    /// <summary>
    /// Heap size.
    /// </summary>
    public uint HeapSize;

    /// <summary>
    /// Read the info.
    /// </summary>
    /// <param name="r">The reader.</param>
    public void Read(FileReader r)
    {
        SequenceMax = r.ReadUInt16();
        ChannelFlags = r.ReadBitFlags(2);
        if (ChannelFlags.Where(x => x == false).Count() == 16) { ChannelFlags = [true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true,
        ]; }
        HeapSize = r.ReadUInt32();
    }

    /// <summary>
    /// Write the info.
    /// </summary>
    /// <param name="w">The writer.</param>
    public void Write(FileWriter w)
    {
        w.Write(SequenceMax);
        if (ChannelFlags.Count(x => x) == 16)
        {
            w.Write((ushort)0);
        }
        else
        {
            w.WriteBitFlags(ChannelFlags, 2);
        }
        w.Write(HeapSize);
    }

    /// <summary>
    /// Get bit flags.
    /// </summary>
    public ushort BitFlags()
    {

        //Flags.
        ushort u = 0;
        for (int i = 0; i < ChannelFlags.Length; i++)
        {
            if (ChannelFlags[i]) { u |= (ushort)(0b1 << i); }
        }
        return u;
    }
}