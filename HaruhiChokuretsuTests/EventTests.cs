using HaruhiChokuretsuLib;
using HaruhiChokuretsuLib.Archive;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HaruhiChokuretsuTests
{
    public class EventTests
    {
        [Test]
        [TestCase(TestVariables.EVT_000_DECOMPRESSED)]
        [TestCase(TestVariables.EVT_66_DECOMPRESSED)]
        [TestCase(TestVariables.EVT_MEMORYCARD_DECOMPRESSED)]
        [TestCase(TestVariables.EVT_TEST_DECOMPRESSED)]
        public void EventFileParserTest(string eventFile)
        {
            byte[] eventFileOnDisk = File.ReadAllBytes(eventFile);
            EventFile @event = new();
            @event.Initialize(eventFileOnDisk);

            Assert.AreEqual(eventFileOnDisk, @event.GetBytes());
        }

        [Test]
        [TestCase(TestVariables.EVT_000_DECOMPRESSED)]
        [TestCase(TestVariables.EVT_66_DECOMPRESSED)]
        [TestCase(TestVariables.EVT_MEMORYCARD_DECOMPRESSED)]
        [TestCase(TestVariables.EVT_TEST_DECOMPRESSED)]
        public void EventFileMovePointersIdempotentTest(string eventFile)
        {
            byte[] eventFileOnDisk = File.ReadAllBytes(eventFile);
            EventFile @event = new();
            @event.Initialize(eventFileOnDisk);

            string originalLine = @event.DialogueLines[0].Text;

            @event.EditDialogueLine(0, $"{originalLine}あ");
            @event.EditDialogueLine(0, $"{originalLine}");

            Assert.AreEqual(eventFileOnDisk, @event.GetBytes());
        }

        [TestCase(TestVariables.EVT_589, TestVariables.EVT_589_RESX)]
        public void VoiceMapFileImportResxTest(string eventFile, string resxFile)
        {
            byte[] vmFileFileOnDisk = File.ReadAllBytes(eventFile);
            VoiceMapFile vmFile = new();
            vmFile.Initialize(vmFileFileOnDisk);

            int[] originalEndPointerPointers = new int[vmFile.EndPointerPointers.Count];
            vmFile.EndPointerPointers.CopyTo(originalEndPointerPointers);

            vmFile.ImportResxFile(resxFile);

            int currentShift = 1;
            bool reset = false;
            for (int i = 0; i < originalEndPointerPointers.Length; i++)
            {
                if (!reset && vmFile.EndPointers[i] > vmFile.DialogueLinesPointer)
                {
                    reset = true;
                    currentShift = 1;
                }
                if ((reset && i % 2 != 0 || !reset) && originalEndPointerPointers[i] > vmFile.DialogueLinesPointer)
                {
                    Assert.AreEqual(originalEndPointerPointers[i] + 4 * currentShift, BitConverter.ToInt32(vmFile.Data.Skip(vmFile.EndPointers[i]).Take(4).ToArray()), $"Failed at {i}");
                    currentShift++;
                }
                else
                {
                    Assert.AreEqual(originalEndPointerPointers[i], vmFile.EndPointerPointers[i], $"Failed at {i}");
                }
            }
        }

        [Test]
        // This file can be ripped directly from the ROM
        [TestCase(".\\inputs\\evt.bin")]
        public void EvtFileParserTest(string evtFile)
        {
            ArchiveFile<EventFile> evt = ArchiveFile<EventFile>.FromFile(evtFile);
            Assert.AreEqual(evt.NumFiles, evt.Files.Count);

            foreach (EventFile eventFile in evt.Files)
            {
                Assert.AreEqual(eventFile.Offset, evt.RecalculateFileOffset(eventFile));
            }

            byte[] newEvtBytes = evt.GetBytes();
            Console.WriteLine($"Efficiency: {(double)newEvtBytes.Length / File.ReadAllBytes(evtFile).Length * 100}%");

            ArchiveFile<EventFile> newEvtFile = new(newEvtBytes);
            Assert.AreEqual(evt.Files.Count, newEvtFile.Files.Count);
            for (int i = 0; i < newEvtFile.Files.Count; i++)
            {
                Assert.AreEqual(evt.Files[i].Data, newEvtFile.Files[i].Data, $"Failed at file {i} (offset: 0x{evt.Files[i].Offset:X8}; index: {evt.Files[i].Index:X4}");
            }

            Assert.AreEqual(newEvtBytes, newEvtFile.GetBytes());
        }
    }
}
