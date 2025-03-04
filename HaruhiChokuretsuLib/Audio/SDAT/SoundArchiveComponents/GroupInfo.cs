// These files are taken from Nitro Studio 2
// https://github.com/Gota7/NitroStudio2
// They have been modified to work with .NET 6.0
// Nitro Studio 2 is created by Gota 7
// It is not explicitly licensed, but given that its
// components are licensed under GPLv3, we can assume
// it is also GPLv3 compatible
using GotaSoundIO.IO;
using System.Collections.Generic;
using System.Linq;

namespace HaruhiChokuretsuLib.Audio.SDAT.SoundArchiveComponents
{
    /// <summary>
    /// Group info.
    /// </summary>
    public class GroupInfo : IReadable, IWriteable
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
        /// Entries.
        /// </summary>
        public List<GroupEntry> Entries = [];

        /// <summary>
        /// Read the info.
        /// </summary>
        /// <param name="r">The reader.</param>
        public void Read(FileReader r)
        {
            Entries = [];
            uint numEntries = r.ReadUInt32();
            for (uint i = 0; i < numEntries; i++)
            {
                Entries.Add(r.Read<GroupEntry>());
            }
        }

        /// <summary>
        /// Write the info.
        /// </summary>
        /// <param name="w">The writer.</param>
        public void Write(FileWriter w)
        {
            Entries = Entries.Where(x => x.Entry != null).ToList();
            w.Write((uint)Entries.Count);
            foreach (var e in Entries)
            {
                w.Write(e);
            }
        }
    }
}
