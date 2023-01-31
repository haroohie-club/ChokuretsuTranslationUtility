using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using HaruhiChokuretsuLib.Util;

namespace HaruhiChokuretsuLib.Archive
{
    public class ArchiveFile<T>
        where T : FileInArchive, new()
    {
        public const int FirstMagicIntegerOffset = 0x20;

        public string FileName { get; set; }
        public int NumFiles { get; set; }
        public int FileSpacing { get; set; }
        public int MagicIntegerLsbMultiplier { get; set; }
        public int MagicIntegerLsbMask { get; set; }
        public int MagicIntegerMsbShift { get; set; }
        public int FileNamesLength { get; set; }
        public uint Unknown1 { get; set; }
        public uint Unknown2 { get; set; }
        public List<uint> MagicIntegers { get; set; } = new();
        public List<uint> SecondHeaderNumbers { get; set; } = new();
        public List<byte> FileNamesSection { get; set; }
        public List<T> Files { get; set; } = new();
        public Dictionary<int, int> LengthToMagicIntegerMap { get; private set; } = new();
        private ILogger _log { get; set; }

        public static ArchiveFile<T> FromFile(string fileName, ILogger log)
        {
            byte[] archiveBytes = File.ReadAllBytes(fileName);
            return new ArchiveFile<T>(archiveBytes, log) { FileName = Path.GetFileName(fileName) };
        }

        public ArchiveFile(byte[] archiveBytes, ILogger log)
        {
            _log = log;

            // Convert the main header components
            NumFiles = BitConverter.ToInt32(archiveBytes.Take(4).ToArray());

            FileSpacing = BitConverter.ToInt32(archiveBytes.Skip(0x04).Take(4).ToArray());
            MagicIntegerLsbMultiplier = BitConverter.ToInt32(archiveBytes.Skip(0x08).Take(4).ToArray());

            MagicIntegerLsbMask = BitConverter.ToInt32(archiveBytes.Skip(0x10).Take(4).ToArray());
            MagicIntegerMsbShift = BitConverter.ToInt32(archiveBytes.Skip(0x0C).Take(4).ToArray());

            Unknown1 = BitConverter.ToUInt32(archiveBytes.Skip(0x14).Take(4).ToArray());
            Unknown2 = BitConverter.ToUInt32(archiveBytes.Skip(0x18).Take(4).ToArray());

            // Grab all the magic integers
            for (int i = 0; i <= MagicIntegerLsbMask; i++)
            {
                int length = GetFileLength((uint)i);
                if (!LengthToMagicIntegerMap.ContainsKey(length))
                {
                    LengthToMagicIntegerMap.Add(length, i);
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
            List<string> filenames = new();
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
                byte[] fileBytes = archiveBytes.Skip(offset).Take(compressedLength).ToArray();
                if (fileBytes.Length > 0)
                {
                    T file = new();
                    try
                    {
                        file = FileManager<T>.FromCompressedData(fileBytes, _log, offset, filenames[i]);
                    }
                    catch (IndexOutOfRangeException)
                    {
                        _log.LogWarning($"Failed to parse file at 0x{i:X8} due to index out of range exception (most likely during decompression)");
                    }
                    file.Offset = offset;
                    file.MagicInteger = MagicIntegers[i];
                    file.Index = i + 1;
                    file.Length = compressedLength;
                    file.CompressedData = fileBytes.ToArray();
                    Files.Add(file);
                }
            }
        }

        public string GetSourceInclude()
        {
            return string.Join("\n", Files.Select(f => $".set {f.Name}, {f.Index}")) + "\n";
        }

        public int GetFileOffset(uint magicInteger)
        {
            return (int)((magicInteger >> MagicIntegerMsbShift) * FileSpacing);
        }

        public int GetFileLength(uint magicInteger)
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
                    salt = (uint)standardLengthIncrement + (salt << 1) + (uint)(carryFlag ? 1 : 0);
                    carryFlag = nextCarryFlag;
                    // SUBCC
                    if (!carryFlag)
                    {
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

        public int RecalculateFileOffset(T file)
        {
            return GetFileOffset(file.MagicInteger);
        }

        public uint GetNewMagicInteger(T file, int compressedLength)
        {
            uint offsetComponent = (uint)(file.Offset / FileSpacing) << MagicIntegerMsbShift;
            int newLength = (compressedLength + 0x7FF) & ~0x7FF; // round to nearest 0x800
            int newLengthComponent = LengthToMagicIntegerMap[newLength];

            return offsetComponent | (uint)newLengthComponent;
        }

        public void AddFile(string filename)
        {
            T file = new();
            _log.Log($"Creating new file from {Path.GetFileName(filename)}... ");
            file.NewFile(filename, _log);
            AddFile(file);
        }

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

        public byte[] GetBytes()
        {
            List<byte> bytes = new();

            bytes.AddRange(BitConverter.GetBytes(NumFiles));
            bytes.AddRange(BitConverter.GetBytes(FileSpacing));
            bytes.AddRange(BitConverter.GetBytes(MagicIntegerLsbMultiplier));
            bytes.AddRange(BitConverter.GetBytes(MagicIntegerMsbShift));
            bytes.AddRange(BitConverter.GetBytes(MagicIntegerLsbMask));
            bytes.AddRange(BitConverter.GetBytes(Unknown1));
            bytes.AddRange(BitConverter.GetBytes(Unknown2));

            List<byte> namesSectionBytes = new();
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
                        || nameBytes[j] >= 88)
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
                        pointerShift = ((bytes.Count - Files[i + 1].Offset) / FileSpacing) + 1;
                    }
                    if (pointerShift > 0)
                    {
                        // Calculate the new magic integer factoring in pointer shift
                        Files[i + 1].Offset = ((Files[i + 1].Offset / FileSpacing) + pointerShift) * FileSpacing;
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

            return bytes.ToArray();
        }
    }
}
