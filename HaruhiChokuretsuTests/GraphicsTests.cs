using HaruhiChokuretsuLib.Archive;
using HaruhiChokuretsuLib.Archive.Graphics;
using HaruhiChokuretsuLib.Util;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System;
using System.IO;
using System.Linq;

namespace HaruhiChokuretsuTests
{
    public class GraphicsTests
    {
        private readonly ConsoleLogger _log = new();

        [Test]
        // This file can be ripped directly from the ROM
        [TestCase(".\\inputs\\grp.bin")]
        public void GrpFileParserTest(string grpFile)
        {
            ConsoleLogger log = new();
            ArchiveFile<GraphicsFile> grp = ArchiveFile<GraphicsFile>.FromFile(grpFile, _log);
            grp.Files.First(f => f.Index == 0xE50).InitializeFontFile();

            foreach (GraphicsFile graphicsFile in grp.Files)
            {
                ClassicAssert.AreEqual(graphicsFile.Offset, grp.RecalculateFileOffset(graphicsFile));
            }

            byte[] newGrpBytes = grp.GetBytes();
            Console.WriteLine($"Efficiency: {(double)newGrpBytes.Length / File.ReadAllBytes(grpFile).Length * 100}%");

            ArchiveFile<GraphicsFile> newGrpFile = new(newGrpBytes, log);
            newGrpFile.Files.First(f => f.Index == 0xE50).InitializeFontFile();
            ClassicAssert.AreEqual(grp.Files.Count, newGrpFile.Files.Count);
            for (int i = 0; i < newGrpFile.Files.Count; i++)
            {
                if (grp.Files[i].Data is not null && newGrpFile.Files[i].Data is not null)
                {
                    ClassicAssert.AreEqual(grp.Files[i].Data, newGrpFile.Files[i].Data, $"Failed at file {i} (offset: 0x{grp.Files[i].Offset:X8}; index: {grp.Files[i].Index:X4}");
                }
            }

            ClassicAssert.AreEqual(newGrpBytes, newGrpFile.GetBytes());
        }
    }
}
