// These files are taken from Nitro Studio 2
// https://github.com/Gota7/NitroStudio2
// They have been modified to work with .NET 6.0
// Nitro Studio 2 is created by Gota 7
// It is not explicitly licensed, but given that its
// components are licensed under GPLv3, we can assume
// it is also GPLv3 compatible
using GotaSequenceLib;
using GotaSoundIO.IO;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HaruhiChokuretsuLib.Audio.SDAT.Instruments
{
    /// <summary>
    /// Instrument.
    /// </summary>
    public abstract class Instrument : IReadable, IWriteable
    {
        /// <summary>
        /// Note info.
        /// </summary>
        public List<NoteInfo> NoteInfo = [];

        /// <summary>
        /// Program number.
        /// </summary>
        public int Index;

        /// <summary>
        /// Order.
        /// </summary>
        public long GetOrder => Order + Math.Max((int)Type() - 5, 0) * 100000000;

        /// <summary>
        /// Order.
        /// </summary>
        public long Order;

        /// <summary>
        /// Read the instrument.
        /// </summary>
        /// <param name="r">The reader.</param>
        public abstract void Read(FileReader r);

        /// <summary>
        /// Write the instrument.
        /// </summary>
        /// <param name="w">The writer.</param>
        public abstract void Write(FileWriter w);

        /// <summary>
        /// Instrument type.
        /// </summary>
        /// <returns>The instrument type.</returns>
        public abstract InstrumentType Type();

        /// <summary>
        /// Max instruments.
        /// </summary>
        /// <returns>The max instruments.</returns>
        public abstract uint MaxInstruments();

        /// <summary>
        /// Get the note parameter for a note.
        /// </summary>
        /// <param name="note">The note to retrieve.</param>
        /// <returns>The note parameter.</returns>
        public NoteInfo GetNoteInfo(Notes note)
        {
            //Switch instrument type.
            switch (Type())
            {

                //Direct.
                case InstrumentType.PCM:
                case InstrumentType.PSG:
                case InstrumentType.Noise:
                    return NoteInfo[0];

                //Range.
                case InstrumentType.DrumSet:
                case InstrumentType.KeySplit:

                    //Old algorithm.
                    /*if ((Type() == InstrumentType.DrumSet && (byte)note < (this as DrumSetInstrument).Min) || (byte)note > NoteInfo.Keys.ElementAt(NoteInfo.Keys.Count - 1)) {
                        return null;
                    }
                    int regionNum = 0;
                    for (int i = NoteInfo.Count - 1; i >= 0; i--) {
                        if ((byte)note <= NoteInfo.Keys.ElementAt(i)) {
                            regionNum = i;
                        }
                    }
                    return NoteInfo.Values.ElementAt(regionNum);*/

                    //New algorithm.
                    if ((Type() == InstrumentType.DrumSet && (byte)note < (this as DrumSetInstrument).Min) || (byte)note > NoteInfo.Select(x => (byte)x.Key).ElementAt(NoteInfo.Count - 1))
                    {
                        return null;
                    }
                    for (int i = 0; i < NoteInfo.Count; i++)
                    {
                        if ((byte)note <= (byte)NoteInfo[i].Key)
                        {
                            return NoteInfo[i];
                        }
                    }
                    return null;

            }

            //Default is null.
            return null;
        }
    }

    /// <summary>
    /// Instrument type.
    /// </summary>
    public enum InstrumentType : byte
    {
        /// <summary>
        /// No instrument
        /// </summary>
        Blank,
        /// <summary>
        /// PCM audio
        /// </summary>
        PCM,
        /// <summary>
        /// PSG audio
        /// </summary>
        PSG,
        /// <summary>
        /// Noise
        /// </summary>
        Noise,
        /// <summary>
        /// Direct PCM audio
        /// </summary>
        DirectPCM,
        /// <summary>
        /// Null
        /// </summary>
        Null,
        /// <summary>
        /// Drum set
        /// </summary>
        DrumSet = 16,
        /// <summary>
        /// Key split
        /// </summary>
        KeySplit = 17
    }
}
