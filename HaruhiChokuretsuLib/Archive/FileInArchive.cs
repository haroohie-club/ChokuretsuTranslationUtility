using HaruhiChokuretsuLib.Util;
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
        protected ILogger _log { get; set; }

        /// <summary>
        /// Initializes the file
        /// </summary>
        /// <param name="decompressedData">The file's decompressed data</param>
        /// <param name="offset">The offset of the file in its archive</param>
        /// <param name="log">An ILogger instance used for logging during initialization</param>
        public virtual void Initialize(byte[] decompressedData, int offset, ILogger log)
        {
            Data = [.. decompressedData];
            _log = log;
        }
        /// <summary>
        /// Gets the binary representation of the file
        /// </summary>
        /// <returns>A byte array containing the file data</returns>
        public virtual byte[] GetBytes()
        {
            return [.. Data];
        }

        /// <summary>
        /// Creates a new file for insertion into an archive
        /// </summary>
        /// <param name="filename">The name of the file as it will appear in the archive</param>
        /// <param name="log">An ILogger instance used for logging during file creation</param>
        public virtual void NewFile(string filename, ILogger log)
        {
        }

        /// <summary>
        /// Empty constructor of the abstract type
        /// </summary>
        public FileInArchive()
        {
        }

        /// <summary>
        /// Casts the file in archive to a specific type of file
        /// </summary>
        /// <typeparam name="T">The type to cast the file to (must inherit FileInArchive)</typeparam>
        /// <returns>That file</returns>
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
            newFile.Initialize([.. Data], Offset, _log);

            return newFile;
        }
        public override string ToString()
        {
            return Name;
        }
    }

    /// <summary>
    /// A static class for creating files
    /// </summary>
    /// <typeparam name="T">The type of file to create (must inherit FileInArchive)</typeparam>
    public static class FileManager<T>
        where T : FileInArchive, new()
    {
        /// <summary>
        /// Creates a file from compressed data
        /// </summary>
        /// <param name="compressedData">The file's compressed data</param>
        /// <param name="log">An ILogger instance </param>
        /// <param name="offset">(Optional) The offset of the file in the archive it comes from</param>
        /// <param name="name">(Optional) The name of the file in its archive</param>
        /// <returns>A file object of type T</returns>
        public static T FromCompressedData(IEnumerable<byte> compressedData, ILogger log, int offset = 0, string name = null)
        {
            T created = new()
            {
                Name = name
            };
            created.Initialize(Helpers.DecompressData([.. compressedData]), offset, log);
            return created;
        }
    }
}
