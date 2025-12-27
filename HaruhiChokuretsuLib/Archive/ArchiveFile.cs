using HaruhiChokuretsuLib.Util;
using HaruhiChokuretsuLib.Util.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace HaruhiChokuretsuLib.Archive;

/// <summary>
/// Representation of a bin archive file
/// </summary>
/// <typeparam name="T">The type of file contained by this archive</typeparam>
public class ArchiveFile<T>
    where T : FileInArchive, new()
{
    /// <summary>
    /// Offset of first magic integer
    /// </summary>
    public const int FirstMagicIntegerOffset = 0x20;

    /// <summary>
    /// Name of the archive
    /// </summary>
    public string FileName { get; set; }
    /// <summary>
    /// Number of files in the archive
    /// </summary>
    public int NumFiles { get; internal set; }
    /// <summary>
    /// Alignment in bytes of the files
    /// </summary>
    public int FileAlignment { get; private set; }
    internal int MagicIntegerLsbMultiplier { get; set; }
    internal int MagicIntegerLsbMask { get; set; }
    internal int MagicIntegerMsbShift { get; set; }
    internal int FileNamesLength { get; set; }
    internal uint Unknown1 { get; set; }
    internal uint Unknown2 { get; set; }
    /// <summary>
    /// List of magic integers in the archive
    /// </summary>
    public List<uint> MagicIntegers { get; set; } = [];
    /// <summary>
    /// Unknown second header numbers section data
    /// </summary>
    public List<uint> SecondHeaderNumbers { get; set; } = [];
    /// <summary>
    /// Filenames section data
    /// </summary>
    public List<byte> FileNamesSection { get; set; }
    /// <summary>
    /// List of files in the archive
    /// </summary>
    public List<T> Files { get; set; } = [];
    private readonly Dictionary<int, int> _lengthToMagicIntegerMap = [];
    private readonly ILogger _log;

    /// <summary>
    /// Creates an ArchiveFile object from a file on disk
    /// </summary>
    /// <param name="fileName">The file on disk</param>
    /// <param name="log">ILogger instance for logging</param>
    /// <param name="dontThrow">(Optional) If set to true, will not throw errors during archive decompression (but will log them)</param>
    /// <param name="generic">(Optional) Indicates that this is a data archive being parsed as an event archive</param>
    /// <returns></returns>
    public static ArchiveFile<T> FromFile(string fileName, ILogger log, bool dontThrow = true, bool generic = false)
    {
        byte[] archiveBytes = File.ReadAllBytes(fileName);
        return new(archiveBytes, log, dontThrow, generic) { FileName = Path.GetFileName(fileName) };
    }

    /// <summary>
    /// Parameterless constructor intended only for testing mocks. Do not use in production.
    /// </summary>
    public ArchiveFile()
    {
    }

    /// <summary>
    /// Creates a new archive file give binary bin archive data
    /// </summary>
    /// <param name="archiveBytes">Binary data from a bin archive</param>
    /// <param name="log">ILogger instance for logging</param>
    /// <param name="dontThrow">(Optional) If set to true, will not throw errors during archive decompression (but will log them)</param>
    /// <param name="generic">(Optional) Indicates that this is a data archive being parsed as an event archive</param>
    /// <exception cref="ArchiveLoadException"></exception>
    public ArchiveFile(byte[] archiveBytes, ILogger log, bool dontThrow = true, bool generic = false)
    {
        _log = log;

        // Convert the main header components
        NumFiles = BitConverter.ToInt32(archiveBytes.Take(4).ToArray());

        FileAlignment = BitConverter.ToInt32(archiveBytes.Skip(0x04).Take(4).ToArray());
        MagicIntegerLsbMultiplier = BitConverter.ToInt32(archiveBytes.Skip(0x08).Take(4).ToArray());

        MagicIntegerLsbMask = BitConverter.ToInt32(archiveBytes.Skip(0x10).Take(4).ToArray());
        MagicIntegerMsbShift = BitConverter.ToInt32(archiveBytes.Skip(0x0C).Take(4).ToArray());

        Unknown1 = BitConverter.ToUInt32(archiveBytes.Skip(0x14).Take(4).ToArray());
        Unknown2 = BitConverter.ToUInt32(archiveBytes.Skip(0x18).Take(4).ToArray());

        // Grab all the magic integers
        for (int i = 0; i <= MagicIntegerLsbMask; i++)
        {
            int length = GetFileLength((uint)i);
            if (!_lengthToMagicIntegerMap.ContainsKey(length))
            {
                _lengthToMagicIntegerMap.Add(length, i);
            }
        }

        // Grab the other parts of the header (not used in-game, but we should keep them for fidelity)
        FileNamesLength = BitConverter.ToUInt16(archiveBytes.Skip(0x1C).Take(4).ToArray());
        for (int i = FirstMagicIntegerOffset; i < (NumFiles * 4) + 0x20; i += 4)
        {
            MagicIntegers.Add(BitConverter.ToUInt32(archiveBytes.Skip(i).Take(4).ToArray()));
        }
        int firstNextPointer = FirstMagicIntegerOffset + MagicIntegers.Count * 4;
        for (int i = firstNextPointer; i < (NumFiles * 4) + firstNextPointer; i += 4)
        {
            SecondHeaderNumbers.Add(BitConverter.ToUInt32(archiveBytes.Skip(i).Take(4).ToArray()));
        }
        FileNamesSection = archiveBytes.Skip(0x20 + (NumFiles * 8)).Take(FileNamesLength).ToList();

        // Calculate file names based on the substitution cipher
        List<string> filenames = [];
        for (int i = 0; i < FileNamesSection.Count;)
        {
            byte[] nameBytes = FileNamesSection.Skip(i).TakeWhile(b => b != 0x00).ToArray();
            for (int j = 0; j < nameBytes.Length; j++)
            {
                if ((nameBytes[j] >= 67 && nameBytes[j] <= 70)
                    || (nameBytes[j] >= 75 && nameBytes[j] <= 76)
                    || (nameBytes[j] >= 84 && nameBytes[j] <= 86)
                    || (nameBytes[j] >= 91 && nameBytes[j] <= 94)
                    || (nameBytes[j] >= 99 && nameBytes[j] <= 103)
                    || nameBytes[j] >= 107)
                {
                    nameBytes[j] -= 19;
                }
                else
                {
                    nameBytes[j] -= 11;
                }
            }
            filenames.Add(Encoding.ASCII.GetString(nameBytes));
            i += nameBytes.Length + 1;
        }

        // Add all the files to the archive from the magic integer offsets
        for (int i = 0; i < MagicIntegers.Count; i++)
        {
            int offset = GetFileOffset(MagicIntegers[i]);
            int compressedLength = GetFileLength(MagicIntegers[i]);
            byte[] fileBytes = archiveBytes[offset..(offset + compressedLength)];
            if (fileBytes.Length == 0)
            {
                continue;
            }

            T file = new();
            try
            {
                file = FileManager<T>.FromCompressedData(fileBytes, _log, offset, i >= filenames.Count ? $"FILE{i}" : filenames[i], generic: generic);
            }
            catch (Exception ex)
            {
                if (dontThrow)
                {
                    _log.LogError($"Failed to parse file {filenames[i]} (0x{i + 1:X3}): {ex.Message}!");
                    _log.LogWarning(ex.StackTrace);
                }
                else
                {
                    throw new ArchiveLoadException(i, i >= filenames.Count ? $"FILE{i+1}" : filenames[i], ex);
                }
            }
            file.Offset = offset;
            file.MagicInteger = MagicIntegers[i];
            file.Index = i + 1;
            file.Length = compressedLength;
            file.CompressedData = fileBytes.ToArray();
            Files.Add(file);
        }
    }

    /// <summary>
    /// Gets a file from the archive given a specified file index
    /// </summary>
    /// <param name="index">The index of the file to get</param>
    /// <returns>A FileInArchive object</returns>
    public virtual T GetFileByIndex(int index)
    {
        if (index > 0 && index <= Files.Count)
        {
            return Files[index - 1];
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// Gets a file from the archive given a specified file name
    /// </summary>
    /// <param name="name">The name of the file to get</param>
    /// <returns>A FileInArchive object</returns>
    public virtual T GetFileByName(string name)
    {
        return Files.AsParallel().FirstOrDefault(f => f.Name == name);
    }

    /// <summary>
    /// Lists all files in an ARM assembly include file
    /// </summary>
    /// <returns>A string of valid ARM assembly setting macros for all files and their indices</returns>
    public string GetSourceInclude()
    {
        return string.Join("\n", Files.Select(f => $".set {f.Name}, {f.Index}")) + "\n";
    }

    private int GetFileOffset(uint magicInteger)
    {
        return (int)((magicInteger >> MagicIntegerMsbShift) * FileAlignment);
    }

    private int GetFileLength(uint magicInteger)
    {
        // absolutely unhinged routine
        int magicLengthInt = 0x7FF + (int)((magicInteger & (uint)MagicIntegerLsbMask) * (uint)MagicIntegerLsbMultiplier);
        int standardLengthIncrement = 0x800;
        if (magicLengthInt < standardLengthIncrement)
        {
            magicLengthInt = 0;
        }
        else
        {
            int magicLengthIntLeftShift = 0x1C;
            uint salt = (uint)magicLengthInt >> 0x04;
            if (standardLengthIncrement <= salt >> 0x0C)
            {
                magicLengthIntLeftShift -= 0x10;
                salt >>= 0x10;
            }
            if (standardLengthIncrement <= salt >> 0x04)
            {
                magicLengthIntLeftShift -= 0x08;
                salt >>= 0x08;
            }
            if (standardLengthIncrement <= salt)
            {
                magicLengthIntLeftShift -= 0x04;
                salt >>= 0x04;
            }

            magicLengthInt = (int)((uint)magicLengthInt << magicLengthIntLeftShift);
            standardLengthIncrement = 0 - standardLengthIncrement;

            bool carryFlag = Helpers.AddWillCauseCarry(magicLengthInt, magicLengthInt);
            magicLengthInt *= 2;

            int pcIncrement = magicLengthIntLeftShift * 12;

            for (; pcIncrement <= 0x174; pcIncrement += 0x0C)
            {
                // ADCS
                bool nextCarryFlag = Helpers.AddWillCauseCarry(standardLengthIncrement, (int)(salt << 1) + (carryFlag ? 1 : 0));
                // ReSharper disable once IntVariableOverflowInUncheckedContext
                salt = (uint)standardLengthIncrement + (salt << 1) + (uint)(carryFlag ? 1 : 0);
                carryFlag = nextCarryFlag;
                // SUBCC
                if (!carryFlag)
                {
                    // ReSharper disable once IntVariableOverflowInUncheckedContext
                    salt -= (uint)standardLengthIncrement;
                }
                // ADCS
                nextCarryFlag = Helpers.AddWillCauseCarry(magicLengthInt, magicLengthInt + (carryFlag ? 1 : 0));
                magicLengthInt = (magicLengthInt * 2) + (carryFlag ? 1 : 0);
                carryFlag = nextCarryFlag;
            }
        }

        return magicLengthInt * 0x800;
    }

    /// <summary>
    /// Recalculates a file offset
    /// </summary>
    /// <param name="file">The file whose offset to recalculate</param>
    /// <returns>The offset of the file</returns>
    public int RecalculateFileOffset(T file)
    {
        return GetFileOffset(file.MagicInteger);
    }

    private uint GetNewMagicInteger(T file, int compressedLength)
    {
        uint offsetComponent = (uint)(file.Offset / FileAlignment) << MagicIntegerMsbShift;
        int newLength = (compressedLength + 0x7FF) & ~0x7FF; // round to nearest 0x800
        int newLengthComponent = _lengthToMagicIntegerMap[newLength];

        return offsetComponent | (uint)newLengthComponent;
    }

    /// <summary>
    /// Adds a file to the archive
    /// </summary>
    /// <param name="filename">The path to the file to be added</param>
    public void AddFile(string filename)
    {
        T file = new();
        _log.Log($"Creating new file from {Path.GetFileName(filename)}... ");
        file.NewFile(filename, _log);
        AddFile(file);
    }

    /// <summary>
    /// Adds a file to the archive
    /// </summary>
    /// <param name="file">The file to be added to the archive</param>
    public void AddFile(T file)
    {
        file.Edited = true;
        file.CompressedData = Helpers.CompressData(file.GetBytes());
        file.Offset = GetBytes().Length;
        NumFiles++;
        file.Index = Files.Max(f => f.Index) + 1;
        _log.Log($"New file #{file.Index:X3} will be placed at offset 0x{file.Offset:X8}... ");
        file.Length = file.CompressedData.Length + (0x800 - (file.CompressedData.Length % 0x800) == 0 ? 0 : 0x800 - (file.CompressedData.Length % 0x800));
        file.MagicInteger = GetNewMagicInteger(file, file.CompressedData.Length);
        uint secondHeaderNumber = 0xC0C0C0C0;
        MagicIntegers.Add(file.MagicInteger);
        SecondHeaderNumbers.Add(secondHeaderNumber);
        byte[] nameBytes = Encoding.ASCII.GetBytes(file.Name);
        Files.Add(file);
    }

    /// <summary>
    /// Gets the binary archive data
    /// </summary>
    /// <returns>The binary bin archive data</returns>
    public byte[] GetBytes()
    {
        List<byte> bytes =
        [
            .. BitConverter.GetBytes(NumFiles),
            .. BitConverter.GetBytes(FileAlignment),
            .. BitConverter.GetBytes(MagicIntegerLsbMultiplier),
            .. BitConverter.GetBytes(MagicIntegerMsbShift),
            .. BitConverter.GetBytes(MagicIntegerLsbMask),
            .. BitConverter.GetBytes(Unknown1),
            .. BitConverter.GetBytes(Unknown2),
        ];

        List<byte> namesSectionBytes = [];
        foreach (string filename in Files.Select(f => f.Name))
        {
            byte[] nameBytes = Encoding.ASCII.GetBytes(filename);
            for (int j = 0; j < nameBytes.Length; j++)
            {
                if ((nameBytes[j] >= 48 && nameBytes[j] <= 51)
                    || (nameBytes[j] >= 56 && nameBytes[j] <= 57)
                    || (nameBytes[j] >= 65 && nameBytes[j] <= 67)
                    || (nameBytes[j] >= 72 && nameBytes[j] <= 75)
                    || (nameBytes[j] >= 80 && nameBytes[j] <= 83)
                    || (nameBytes[j] >= 88 && nameBytes[j] <= 94))
                {
                    nameBytes[j] += 19;
                }
                else
                {
                    nameBytes[j] += 11;
                }
            }
            namesSectionBytes.AddRange(nameBytes);
            namesSectionBytes.Add(0);
        }
        bytes.AddRange(BitConverter.GetBytes(namesSectionBytes.Count));

        foreach (uint magicInteger in MagicIntegers)
        {
            bytes.AddRange(BitConverter.GetBytes(magicInteger));
        }
        foreach (uint secondInteger in SecondHeaderNumbers)
        {
            bytes.AddRange(BitConverter.GetBytes(secondInteger));
        }
        bytes.AddRange(namesSectionBytes);

        bytes.AddRange(new byte[Files[0].Offset - bytes.Count]);

        for (int i = 0; i < Files.Count; i++)
        {
            byte[] compressedBytes;
            if (!Files[i].Edited || Files[i].Data is null || Files[i].Data.Count == 0)
            {
                compressedBytes = Files[i].CompressedData;
            }
            else
            {
                compressedBytes = Helpers.CompressData(Files[i].GetBytes());
                byte[] newMagicalIntegerBytes = BitConverter.GetBytes(GetNewMagicInteger(Files[i], compressedBytes.Length));
                int magicIntegerOffset = FirstMagicIntegerOffset + ((Files[i].Index - 1) * 4);
                for (int j = 0; j < newMagicalIntegerBytes.Length; j++)
                {
                    bytes[magicIntegerOffset + j] = newMagicalIntegerBytes[j];
                }
            }
            bytes.AddRange(compressedBytes);
            if (i < Files.Count - 1) // If we aren't on the last file
            {
                int pointerShift = 0;
                while (bytes.Count % 0x10 != 0)
                {
                    bytes.Add(0);
                }
                // If the current size of the archive we’ve constructed so far is greater than
                // the next file’s offset, that means we need to adjust the next file’s offset
                if (bytes.Count > Files[i + 1].Offset)
                {
                    pointerShift = ((bytes.Count - Files[i + 1].Offset) / FileAlignment) + 1;
                }
                if (pointerShift > 0)
                {
                    // Calculate the new magic integer factoring in pointer shift
                    Files[i + 1].Offset = ((Files[i + 1].Offset / FileAlignment) + pointerShift) * FileAlignment;
                    int magicIntegerOffset = FirstMagicIntegerOffset + (i + 1) * 4;
                    uint newMagicInteger = GetNewMagicInteger(Files[i + 1], Files[i + 1].Length);
                    Files[i + 1].MagicInteger = newMagicInteger;
                    MagicIntegers[i + 1] = newMagicInteger;
                    bytes.RemoveRange(magicIntegerOffset, 4);
                    bytes.InsertRange(magicIntegerOffset, BitConverter.GetBytes(Files[i + 1].MagicInteger));
                }
                bytes.AddRange(new byte[Files[i + 1].Offset - bytes.Count]);
            }
        }
        while (bytes.Count % 0x800 != 0)
        {
            bytes.Add(0);
        }

        return [.. bytes];
    }
}