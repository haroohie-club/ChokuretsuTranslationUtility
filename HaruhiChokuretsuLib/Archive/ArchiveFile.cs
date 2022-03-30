using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HaruhiChokuretsuLib.Archive
{
    public class ArchiveFile<T>
        where T : FileInArchive, new()
    {
        public const int FirstHeaderPointerOffset = 0x1C;

        public List<byte> Header { get; set; }

        public string FileName { get; set; }
        public int NumItems { get; set; }
        public int HeaderLength { get; set; }
        public int MagicIntegerMsbMultiplier { get; set; }
        public int MagicIntegerLsbMultiplier { get; set; }
        public int MagicIntegerLsbAnd { get; set; }
        public int MagicIntegerMsbShift { get; set; }
        public List<uint> HeaderPointers { get; set; } = new();
        public List<uint> SecondHeaderNumbers { get; set; } = new();
        public List<byte> FinalHeaderComponent { get; set; }
        public List<T> Files { get; set; } = new();
        public Dictionary<int, int> LengthToMagicIntegerMap { get; private set; } = new();

        public static ArchiveFile<T> FromFile(string fileName)
        {
            byte[] archiveBytes = File.ReadAllBytes(fileName);
            return new ArchiveFile<T>(archiveBytes) { FileName = Path.GetFileName(fileName) };
        }

        public ArchiveFile(byte[] archiveBytes)
        {
            int endOfHeader = 0x00;
            for (int i = 0; i < archiveBytes.Length - 0x10; i++)
            {
                if (archiveBytes.Skip(i).Take(0x10).All(b => b == 0x00))
                {
                    endOfHeader = i;
                    break;
                }
            }
            int numZeroes = archiveBytes.Skip(endOfHeader).TakeWhile(b => b == 0x00).Count();
            int firstFileOffset = endOfHeader + numZeroes;

            Header = archiveBytes.Take(firstFileOffset).ToList();

            NumItems = BitConverter.ToInt32(archiveBytes.Take(4).ToArray());

            MagicIntegerMsbMultiplier = BitConverter.ToInt32(archiveBytes.Skip(0x04).Take(4).ToArray());
            MagicIntegerLsbMultiplier = BitConverter.ToInt32(archiveBytes.Skip(0x08).Take(4).ToArray());

            MagicIntegerLsbAnd = BitConverter.ToInt32(archiveBytes.Skip(0x10).Take(4).ToArray());
            MagicIntegerMsbShift = BitConverter.ToInt32(archiveBytes.Skip(0x0C).Take(4).ToArray());

            for (int i = 0; i <= MagicIntegerLsbAnd; i++)
            {
                int length = GetFileLength((uint)i);
                if (!LengthToMagicIntegerMap.ContainsKey(length))
                {
                    LengthToMagicIntegerMap.Add(length, i);
                }
            }

            HeaderLength = BitConverter.ToInt32(archiveBytes.Skip(0x1C).Take(4).ToArray()) + (((NumItems * 2) + 8) * 4);
            for (int i = FirstHeaderPointerOffset; i < (NumItems * 4) + 0x20; i += 4)
            {
                HeaderPointers.Add(BitConverter.ToUInt32(archiveBytes.Skip(i).Take(4).ToArray()));
            }
            int firstNextPointer = FirstHeaderPointerOffset + HeaderPointers.Count * 4;
            for (int i = firstNextPointer; i < (NumItems * 4) + firstNextPointer; i += 4)
            {
                SecondHeaderNumbers.Add(BitConverter.ToUInt32(archiveBytes.Skip(i).Take(4).ToArray()));
            }
            FinalHeaderComponent = archiveBytes.Skip(0x20 + (NumItems * 8)).Take(Header.Count - (NumItems * 8) - 0x20).ToList();

            for (int i = firstFileOffset; i < archiveBytes.Length;)
            {
                int offset = i;
                List<byte> fileBytes = new();
                byte[] nextLine = archiveBytes.Skip(i).Take(0x10).ToArray();
                // compression means that there won't be more than three repeated bytes, so if we see more than three zeroes we've reached the end of a file
                for (i += 0x10; nextLine.BytesInARowLessThan(3, 0x00); i += 0x10)
                {
                    fileBytes.AddRange(nextLine);
                    nextLine = archiveBytes.Skip(i).Take(0x10).ToArray();
                }
                fileBytes.AddRange(nextLine);
                if (fileBytes.Count > 0)
                {
                    fileBytes.AddRange(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });

                    T file = new();
                    try
                    {
                        file = FileManager<T>.FromCompressedData(fileBytes.ToArray(), offset);
                    }
                    catch (IndexOutOfRangeException)
                    {
                        Console.WriteLine($"Failed to parse file at 0x{i:X8} due to index out of range exception (most likely during decompression)");
                    }
                    file.Offset = offset;
                    file.MagicInteger = GetMagicInteger(file.Offset);
                    file.Index = GetFileIndex(file.MagicInteger);
                    file.Length = GetFileLength(file.MagicInteger);
                    file.CompressedData = fileBytes.ToArray();
                    Files.Add(file);
                }
                byte[] zeroes = archiveBytes.Skip(i).TakeWhile(b => b == 0x00).ToArray();
                i += zeroes.Length;
            }
        }

        public uint GetMagicInteger(int offset)
        {
            uint msbToSearchFor = (uint)(offset / MagicIntegerMsbMultiplier) << MagicIntegerMsbShift;
            return HeaderPointers.FirstOrDefault(p => (p & 0xFFFF0000) == msbToSearchFor);
        }

        public int GetFileIndex(uint magicInteger)
        {
            return HeaderPointers.IndexOf(magicInteger);
        }

        public int GetFileLength(uint magicInteger)
        {
            // absolutely unhinged routine
            int magicLengthInt = 0x7FF + (int)((magicInteger & (uint)MagicIntegerLsbAnd) * (uint)MagicIntegerLsbMultiplier);
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
                    bool nextCarryFlag = Helpers.AddWillCauseCarry(standardLengthIncrement, (int)(salt << 1) + (carryFlag ? 1 : 0));
                    salt = (uint)standardLengthIncrement + (salt << 1) + (uint)(carryFlag ? 1 : 0);
                    carryFlag = nextCarryFlag;
                    if (!carryFlag)
                    {
                        salt -= (uint)standardLengthIncrement;
                    }
                    nextCarryFlag = Helpers.AddWillCauseCarry(magicLengthInt, magicLengthInt + (carryFlag ? 1 : 0));
                    magicLengthInt = (magicLengthInt * 2) + (carryFlag ? 1 : 0);
                    carryFlag = nextCarryFlag;
                }
            }

            return magicLengthInt * 0x800;
        }

        public int RecalculateFileOffset(T file, byte[] searchSet = null)
        {
            if (searchSet is null)
            {
                searchSet = Header.ToArray();
            }
            return (int)(BitConverter.ToUInt32(searchSet.Skip(FirstHeaderPointerOffset + (file.Index * 4)).Take(4).ToArray()) >> MagicIntegerMsbShift) * MagicIntegerMsbMultiplier;
        }

        public uint GetNewMagicalInteger(T file, int compressedLength)
        {
            if (file.Data is null)
            {
                return file.MagicInteger;
            }

            uint offsetComponent = (uint)(file.Offset / MagicIntegerMsbMultiplier) << MagicIntegerMsbShift;
            int newLength = (compressedLength + 0x7FF) & ~0x7FF;
            int newLengthComponent = LengthToMagicIntegerMap[newLength];

            return offsetComponent | (uint)newLengthComponent;
        }

        public void AddFile(string filename)
        {
            T file = new();
            Console.WriteLine($"Creating new file from {Path.GetFileName(filename)}... ");
            file.NewFile(filename);
            AddFile(file);
        }

        public void AddFile(T file)
        {
            file.Edited = true;
            file.CompressedData = Helpers.CompressData(file.GetBytes());
            file.Offset = GetBytes().Length;
            NumItems++;
            Header.RemoveRange(0, 4);
            Header.InsertRange(0, BitConverter.GetBytes(NumItems));
            file.Index = Files.Max(f => f.Index) + 1;
            Console.Write($"New file #{file.Index:X3} will be placed at offset 0x{file.Offset:X8}... ");
            file.Length = file.CompressedData.Length + (0x800 - (file.CompressedData.Length % 0x800) == 0 ? 0 : 0x800 - (file.CompressedData.Length % 0x800));
            file.MagicInteger = GetNewMagicalInteger(file, file.CompressedData.Length);
            uint secondHeaderNumber = 0xC0C0C0C0;
            Header.InsertRange(0x1C + HeaderPointers.Count * 4, BitConverter.GetBytes(file.MagicInteger));
            HeaderPointers.Add(file.MagicInteger);
            Header.InsertRange(0x1C + HeaderPointers.Count * 4 + SecondHeaderNumbers.Count * 4, BitConverter.GetBytes(secondHeaderNumber));
            SecondHeaderNumbers.Add(secondHeaderNumber);
            FinalHeaderComponent.RemoveRange(FinalHeaderComponent.Count - 8, 8);
            Files.Add(file);
        }

        public byte[] GetBytes()
        {
            List<byte> bytes = new();

            bytes.AddRange(Header);
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
                    byte[] newMagicalIntegerBytes = BitConverter.GetBytes(GetNewMagicalInteger(Files[i], compressedBytes.Length));
                    int pointerOffset = FirstHeaderPointerOffset + (Files[i].Index * 4);
                    for (int j = 0; j < newMagicalIntegerBytes.Length; j++)
                    {
                        bytes[pointerOffset + j] = newMagicalIntegerBytes[j];
                        Header[pointerOffset + j] = newMagicalIntegerBytes[j];
                    }
                }
                bytes.AddRange(compressedBytes);
                if (i < Files.Count - 1)
                {
                    int pointerShift = 0;
                    while (bytes.Count % 0x10 != 0)
                    {
                        bytes.Add(0);
                    }
                    if (bytes.Count > Files[i + 1].Offset)
                    {
                        pointerShift = ((bytes.Count - Files[i + 1].Offset) / MagicIntegerMsbMultiplier) + 1;
                    }
                    if (pointerShift > 0)
                    {
                        byte[] newPointer = BitConverter.GetBytes((uint)((Files[i + 1].Offset / MagicIntegerMsbMultiplier) + pointerShift) << MagicIntegerMsbShift);
                        int pointerOffset = FirstHeaderPointerOffset + (Files[i + 1].Index * 4);
                        bytes[pointerOffset + 2] = newPointer[2];
                        bytes[pointerOffset + 3] = newPointer[3];
                        Header[pointerOffset + 2] = newPointer[2];
                        Header[pointerOffset + 3] = newPointer[3];
                        Files[i + 1].Offset = RecalculateFileOffset(Files[i + 1], bytes.ToArray());
                    }
                    while (bytes.Count < Files[i + 1].Offset)
                    {
                        bytes.Add(0);
                    }
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
