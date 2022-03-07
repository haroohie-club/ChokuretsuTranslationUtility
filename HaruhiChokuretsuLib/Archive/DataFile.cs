using System.Linq;

namespace HaruhiChokuretsuLib.Archive
{
    public class DataFile : FileInArchive
    {
        public override void Initialize(byte[] decompressedData, int offset)
        {
            Offset = offset;
            Data = decompressedData.ToList();
        }

        public override byte[] GetBytes() => Data.ToArray();

        public override string ToString()
        {
            return $"{Index:X3} 0x{Offset:X8}";
        }
    }
}
