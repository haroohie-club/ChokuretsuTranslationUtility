using HaruhiChokuretsuLib.Archive;
using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HaruhiChokuretsuTests
{
    public class SourceTests
    {
        private ConsoleLogger _log = new();

        private static string[] _mapFileNames = new string[]
        {
            "BUND0S",
            "BUNN0S",
            "BUNN1S",
            "COMD0S",
            "K15D0S",
            "ONGD0S",
            "ONGN0S",
            "TAID0S",
            "POOD0S",
            "KYND0S",
            "SLTD0S",
            "SLTD1S",
            "SL0D0S",
            "SL1D0S",
            "SL2D0S",
            "SL3D0S",
            "SL4D0S",
            "SL5D0S",
            "SL6D0S",
            "SL7D0S",
            "POON0S",
            "ROKD0S",
            "ROUD0S",
            "AKIN0S",
            "SL1D1S",
            "SL2D1S",
            "SL3D1S",
            "SL4D1S",
            "SL5D1S",
            "AKID0S",
            "XTRD0S",
            "SL8D0S",
        };

        private static int[] GetEvtFileIndices()
        {
            List<int> indices = new();
            for (int i = 1; i <= 588; i++)
            {
                if (!new int[] { 106, 537, 580, 581, 588 }.Contains(i))
                {
                    indices.Add(i);
                }
            }
            return indices.ToArray();
        }

        private static async Task<byte[]> CompileFromSource(string source)
        {
            string filePath = @$".\file-{Guid.NewGuid()}.s"; // Guid for uniqueness so we can run these tests in parallel
            string devkitArm = @"C:\devkitPro\devkitARM";
            File.WriteAllText(filePath, source);

            string objFile = $"{Path.Combine(Path.GetDirectoryName(filePath), Path.GetFileNameWithoutExtension(filePath))}.o";
            string binFile = $"{Path.Combine(Path.GetDirectoryName(filePath), Path.GetFileNameWithoutExtension(filePath))}.bin";
            
            ProcessStartInfo gcc = new(Path.Combine(devkitArm, "bin/arm-none-eabi-gcc.exe"), $"-c -nostdlib -static \"{filePath}\" -o \"{objFile}");
            await Process.Start(gcc).WaitForExitAsync();
            await Task.Delay(50); // ensures process is actually complete
            ProcessStartInfo objcopy = new(Path.Combine(devkitArm, "bin/arm-none-eabi-objcopy.exe"), $"-O binary \"{objFile}\" \"{binFile}");
            await Process.Start(objcopy).WaitForExitAsync();
            await Task.Delay(50); // ensures process is actually copmlete
            byte[] bytes = File.ReadAllBytes(binFile);
            File.Delete(filePath);
            File.Delete(objFile);
            File.Delete(binFile);

            return bytes;
        }

        [SetUp]
        public static void Setup()
        {
            if (!File.Exists("COMMANDS.INC"))
            {
                StringBuilder sb = new();
                foreach (ScriptCommand command in EventFile.CommandsAvailable)
                {
                    sb.AppendLine(command.GetMacro());
                }
                File.WriteAllText("COMMANDS.INC", sb.ToString());
            }
        }

        [Test]
        [TestCaseSource("_mapFileNames")]
        [Parallelizable(ParallelScope.All)]
        public async Task MapSourceTest(string mapFileName)
        {
            // This file can be ripped directly from the ROM
            ArchiveFile<DataFile> dat = ArchiveFile<DataFile>.FromFile(@".\inputs\dat.bin", _log);
            MapFile mapFile = dat.Files.First(f => f.Name == mapFileName).CastTo<MapFile>();

            byte[] newBytes = await CompileFromSource(mapFile.GetSource(new()));
            List<byte> newBytesList = new(newBytes);
            if (newBytes.Length % 16 > 0)
            {
                newBytesList.AddRange(new byte[16 - (newBytes.Length % 16)]);
            }

            Assert.AreEqual(mapFile.Data, newBytesList);
        }

        [Test]
        [TestCaseSource("GetEvtFileIndices")]
        [Parallelizable(ParallelScope.All)]
        public async Task EvtSourceTest(int evtFileIndex)
        {
            // This file can be ripped directly from the ROM
            ArchiveFile<EventFile> evt = ArchiveFile<EventFile>.FromFile(@".\inputs\evt.bin", _log);
            EventFile eventFile = evt.Files.First(f => f.Index == evtFileIndex);

            byte[] newBytes = await CompileFromSource(eventFile.GetSource(new()));
            List<byte> newBytesList = new(newBytes);
            if (newBytes.Length % 16 > 0)
            {
                newBytesList.AddRange(new byte[16 - (newBytes.Length % 16)]);
            }

            Assert.AreEqual(eventFile.Data, newBytesList);
        }
    }
}
