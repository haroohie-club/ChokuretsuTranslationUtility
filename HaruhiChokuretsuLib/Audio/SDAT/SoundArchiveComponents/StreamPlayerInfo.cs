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
/// Stream player info.
/// </summary>
public class StreamPlayerInfo : IReadable, IWriteable
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
    /// Is stereo.
    /// </summary>
    public bool IsStereo;

    /// <summary>
    /// Left channel.
    /// </summary>
    public byte LeftChannel;

    /// <summary>
    /// Right channel.
    /// </summary>
    public byte RightChannel;

    /// <summary>
    /// Read the info.
    /// </summary>
    /// <param name="r">The reader.</param>
    public void Read(FileReader r)
    {
        IsStereo = r.ReadByte() > 1;
        LeftChannel = r.ReadByte();
        RightChannel = r.ReadByte();
        r.ReadBytes(21);
    }

    /// <summary>
    /// Write the info.
    /// </summary>
    /// <param name="w">The writer.</param>
    public void Write(FileWriter w)
    {
        w.Write((byte)(IsStereo ? 2 : 1));
        w.Write(LeftChannel);
        w.Write((byte)(IsStereo ? RightChannel : 0xFF));
        for (int i = 0; i < 0xE; i++)
        {
            w.Write((byte)0xFF);
        }
        w.Write(new byte[7]);

    }
}