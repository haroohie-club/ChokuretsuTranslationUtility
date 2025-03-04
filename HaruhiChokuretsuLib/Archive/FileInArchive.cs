using HaruhiChokuretsuLib.Util;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using LiteDB;

namespace HaruhiChokuretsuLib.Archive;

/// <summary>
/// Base class for representing a file in a bin archive
/// </summary>
public partial class FileInArchive
{
    /// <summary>
    /// Name of the file in the archive
    /// </summary>
    public string Name { get; set; }
    /// <summary>
    /// The file's magic integer in the archive
    /// </summary>
    [JsonIgnore]
    [BsonIgnore]
    public uint MagicInteger { get; set; }
    /// <summary>
    /// Index of the file in the archive
    /// </summary>
    public int Index { get; set; }
    /// <summary>
    /// Offset of the file in the archive
    /// </summary>
    [JsonIgnore]
    [BsonIgnore]
    public int Offset { get; set; }
    /// <summary>
    /// Decompressed length of the file
    /// </summary>
    [JsonIgnore]
    [BsonIgnore]
    public int Length { get; set; }
    /// <summary>
    /// Decompressed binary file data
    /// </summary>
    [JsonIgnore]
    [BsonIgnore]
    public List<byte> Data { get; set; }
    /// <summary>
    /// Compressed binary file data
    /// </summary>
    [JsonIgnore]
    [BsonIgnore]
    public byte[] CompressedData { get; set; }
    /// <summary>
    /// If true, the file has been edited and will need to be replaced when saving the archive
    /// </summary>
    [JsonIgnore]
    [BsonIgnore]
    public bool Edited { get; set; } = false;
    /// <summary>
    /// ILogger instance for logging
    /// </summary>
    protected ILogger Log { get; set; }

    /// <summary>
    /// Initializes a file in archive (when overridden in a base class, of a specific type)
    /// </summary>
    /// <param name="decompressedData">The decompressed data to initialize the file with</param>
    /// <param name="offset">The offset of the file in the archive</param>
    /// <param name="log">An ILogger instance for logging</param>
    public virtual void Initialize(byte[] decompressedData, int offset, ILogger log)
    {
        Data = [.. decompressedData];
        Log = log;
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
        newFile.Initialize([.. Data], Offset, Log);

        return newFile;
    }

    /// <inheritdoc/>
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