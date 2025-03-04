// These files are taken from Nitro Studio 2
// https://github.com/Gota7/NitroStudio2
// They have been modified to work with .NET 6.0
// Nitro Studio 2 is created by Gota 7
// It is not explicitly licensed, but given that its
// components are licensed under GPLv3, we can assume
// it is also GPLv3 compatible
using GotaSoundIO.IO;
using System.Linq;

namespace HaruhiChokuretsuLib.Audio.SDAT.Instruments;

/// <summary>
/// Direct instrument.
/// </summary>
public class DirectInstrument : Instrument
{
    /// <summary>
    /// Get the instrument type.
    /// </summary>
    /// <returns>The instrument type.</returns>
    public override InstrumentType Type() => NoteInfo[0].InstrumentType;

    /// <summary>
    /// Max instruments.
    /// </summary>
    /// <returns>The max instruments.</returns>
    public override uint MaxInstruments() => 1;

    /// <summary>
    /// Read the instrument.
    /// </summary>
    /// <param name="r">The reader.</param>
    public override void Read(FileReader r)
    {
        NoteInfo.Add(r.Read<NoteInfo>());
        NoteInfo.Last().Key = GotaSequenceLib.Notes.gn9;
    }

    /// <summary>
    /// Write the instrument.
    /// </summary>
    /// <param name="w">The writer.</param>
    public override void Write(FileWriter w)
    {
        w.Write(NoteInfo[0]);
    }
}