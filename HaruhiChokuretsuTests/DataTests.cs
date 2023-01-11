using HaruhiChokuretsuLib;
using HaruhiChokuretsuLib.Archive;
using HaruhiChokuretsuLib.Archive.Data;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;

namespace HaruhiChokuretsuTests
{
    public class DataTests
    {
        [Test]
        // This file can be ripped directly from the ROM
        [TestCase(".\\inputs\\dat.bin")]
        public void DatFileParserTest(string datFile)
        {
            ArchiveFile<DataFile> dat = ArchiveFile<DataFile>.FromFile(datFile);

            foreach (DataFile dataFile in dat.Files)
            {
                Assert.AreEqual(dataFile.Offset, dat.RecalculateFileOffset(dataFile));
            }

            byte[] newDataBytes = dat.GetBytes();
            Console.WriteLine($"Efficiency: {(double)newDataBytes.Length / File.ReadAllBytes(datFile).Length * 100}%");

            ArchiveFile<DataFile> newDatFile = new(newDataBytes);

            Assert.AreEqual(dat.Files.Count, newDatFile.Files.Count);
            for (int i = 0; i < newDatFile.Files.Count; i++)
            {
                Assert.AreEqual(dat.Files[i].Data, newDatFile.Files[i].Data, $"Failed at file {i} (offset: 0x{dat.Files[i].Offset:X8}; index: {dat.Files[i].Index:X4}");
            }

            Assert.AreEqual(newDataBytes, newDatFile.GetBytes());
        }
    }
}
