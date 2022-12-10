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

        public T CastTo<T>() where T : FileInArchive, new()
        {
            T newFile = new();
            newFile.Name = Name;
            newFile.MagicInteger = MagicInteger;
            newFile.Index = Index;
            newFile.Offset = Offset;
            newFile.Length = Length;
            newFile.Data = Data;
            newFile.CompressedData = CompressedData;
            newFile.Edited = Edited;
            newFile.Initialize(Data.ToArray(), Offset);

            return newFile;
        }
        public override string ToString()
        {
            return Name;
        }
    }

    public static class FileManager<T>
        where T : FileInArchive, new()
    {
        public static T FromCompressedData(byte[] compressedData, int offset = 0, string name = null)
        {
            T created = new();
            created.Name = name;
            created.Initialize(Helpers.DecompressData(compressedData), offset);
            return created;
        }
    }
}
