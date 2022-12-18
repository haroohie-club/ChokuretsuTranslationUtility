using System.Collections.Generic;
using System.Linq;

namespace HaruhiChokuretsuLib.Archive
{
    public class DataFile : FileInArchive, ISourceFile
    {
        public override void Initialize(byte[] decompressedData, int offset)
        {
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
            throw new System.NotImplementedException();
        }
    }
}
