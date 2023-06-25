// These files are taken from Nitro Studio 2
// https://github.com/Gota7/NitroStudio2
// They have been modified to work with .NET 6.0
// Nitro Studio 2 is created by Gota 7
// It is not explicitly licensed, but given that its
// components are licensed under GPLv3, we can assume
// it is also GPLv3 compatible
using GotaSoundIO.IO;

namespace HaruhiChokuretsuLib.Audio.SDAT.SoundArchiveComponents
{
    /// <summary>
    /// Sequence archive info.
    /// </summary>
    public class SequenceArchiveInfo : IReadable, IWriteable
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
        public SequenceArchive File;

        /// <summary>
        /// Reading file Id.
        /// </summary>
        public uint ReadingFileId;

        /// <summary>
        /// Read the info.
        /// </summary>
        /// <param name="r">The reader.</param>
        public void Read(FileReader r)
        {
            ReadingFileId = r.ReadUInt32();
        }

        /// <summary>
        /// Write the info.
        /// </summary>
        /// <param name="w">The writer.</param>
        public void Write(FileWriter w)
        {
            w.Write(ReadingFileId);
        }
    }
}
