using System.Collections.Generic;
using System.Linq;

namespace HaruhiChokuretsuLib.Archive
{
    public partial class FileInArchive
    {
        public string Name { get; set; }
        public uint MagicInteger { get; set; }
        public int Index { get; set; }
        public int Offset { get; set; }
        public int Length { get; set; }
        public List<byte> Data { get; set; }
        public byte[] CompressedData { get; set; }
        public bool Edited { get; set; } = false;

        public virtual void Initialize(byte[] decompressedData, int offset)
        {
            Data = decompressedData.ToList();
        }
        public virtual byte[] GetBytes()
        {
            return Data.ToArray();
        }

        public virtual void NewFile(string filename)
        {
        }

        public FileInArchive()
        {
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public static class FileManager<T>
        where T : FileInArchive, new()
    {
        public static T FromCompressedData(byte[] compressedData, int offset = 0)
        {
            T created = new();
            created.Initialize(Helpers.DecompressData(compressedData), offset);
            return created;
        }
    }
}
