using HaruhiChokuretsuLib.Archive;
using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Util;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System;
using System.IO;

namespace HaruhiChokuretsuTests
{
    public class DataTests
    {
        private readonly ConsoleLogger _log = new();

        [Test]
        // This file can be ripped directly from the ROM
        [TestCase(".\\inputs\\dat.bin")]
        public void DatFileParserTest(string datFile)
        {
            ConsoleLogger log = new();
            ArchiveFile<DataFile> dat = ArchiveFile<DataFile>.FromFile(datFile, _log);

            foreach (DataFile dataFile in dat.Files)
            {
                ClassicAssert.AreEqual(dataFile.Offset, dat.RecalculateFileOffset(dataFile));
            }

            byte[] newDataBytes = dat.GetBytes();
            Console.WriteLine($"Efficiency: {(double)newDataBytes.Length / File.ReadAllBytes(datFile).Length * 100}%");

            ArchiveFile<DataFile> newDatFile = new(newDataBytes, log);

            ClassicAssert.AreEqual(dat.Files.Count, newDatFile.Files.Count);
            for (int i = 0; i < newDatFile.Files.Count; i++)
            {
                ClassicAssert.AreEqual(dat.Files[i].Data, newDatFile.Files[i].Data, $"Failed at file {i} (offset: 0x{dat.Files[i].Offset:X8}; index: {dat.Files[i].Index:X4}");
            }

            ClassicAssert.AreEqual(newDataBytes, newDatFile.GetBytes());
        }
    }
}
