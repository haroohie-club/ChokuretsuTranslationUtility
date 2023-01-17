using System.Collections.Generic;
using System.Linq;
using HaruhiChokuretsuLib.Util;

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
        protected ILogger _log { get; set; }

        public virtual void Initialize(byte[] decompressedData, int offset, ILogger log)
        {
            Data = decompressedData.ToList();
            _log = log;
        }
        public virtual byte[] GetBytes()
        {
            return Data.ToArray();
        }

        public virtual void NewFile(string filename, ILogger log)
        {
        }

        public FileInArchive()
        {
        }

        public T CastTo<T>() where T : FileInArchive, new()
        {
            T newFile = new()
            {
                Name = Name,
                MagicInteger = MagicInteger,
                Index = Index,
                Offset = Offset,
                Length = Length,
                Data = Data,
                CompressedData = CompressedData,
                Edited = Edited
            };
            newFile.Initialize(Data.ToArray(), Offset, _log);

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
        public static T FromCompressedData(byte[] compressedData, ILogger log, int offset = 0, string name = null)
        {
            T created = new();
            created.Name = name;
            created.Initialize(Helpers.DecompressData(compressedData), offset, log);
            return created;
        }
    }
}
