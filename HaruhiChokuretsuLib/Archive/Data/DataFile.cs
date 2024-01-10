using HaruhiChokuretsuLib.Util;
using System.Collections.Generic;
using System.Linq;

namespace HaruhiChokuretsuLib.Archive.Data
{
    /// <summary>
    /// Represents an abstract file in dat.bin
    /// </summary>
    public class DataFile : FileInArchive, ISourceFile
    {
        public override void Initialize(byte[] decompressedData, int offset, ILogger log)
        {
            _log = log;
            Offset = offset;
            Data = decompressedData.ToList();
        }

        public override byte[] GetBytes() => Data.ToArray();

        public override string ToString()
        {
            return $"{Index:X3} 0x{Offset:X8} - {Name}";
        }

        public virtual string GetSource(Dictionary<string, IncludeEntry[]> includes)
        {
            _log.LogError("Attempting to get source of a generic data file; not supported.");
            return null;
        }
    }

    /// <summary>
    /// Represents a section of a data file
    /// </summary>
    public class DataFileSection
    {
        /// <summary>
        /// The name of the section (used for source file creation; not actually present in the binary)
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// The offset of the section in the binary
        /// </summary>
        public int Offset { get; set; }
        /// <summary>
        /// The number of items the section contains
        /// </summary>
        public int ItemCount { get; set; }
    }
}
