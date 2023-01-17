using HaruhiChokuretsuLib;
using HaruhiChokuretsuLib.Archive;
using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HaruhiChokuretsuTests
{
    public class EventTests
    {
        private readonly ConsoleLogger _log = new();

        [Test]
        [TestCase(TestVariables.EVT_000_DECOMPRESSED)]
        [TestCase(TestVariables.EVT_66_DECOMPRESSED)]
        [TestCase(TestVariables.EVT_TEST_DECOMPRESSED)]
        public void EventFileParserTest(string eventFile)
        {
            byte[] eventFileOnDisk = File.ReadAllBytes(eventFile);
            EventFile @event = new() { Name = "EV0_TESTS" };
            @event.Initialize(eventFileOnDisk, 0, _log);

            Assert.AreEqual(eventFileOnDisk, @event.GetBytes());
        }

        [Test]
        [TestCase(TestVariables.EVT_000_DECOMPRESSED)]
        [TestCase(TestVariables.EVT_66_DECOMPRESSED)]
        [TestCase(TestVariables.EVT_TEST_DECOMPRESSED)]
        public void EventFileMovePointersIdempotentTest(string eventFile)
        {
            byte[] eventFileOnDisk = File.ReadAllBytes(eventFile);
            EventFile @event = new() { Name = "EV0_TESTS" };
            @event.Initialize(eventFileOnDisk, 0, _log);

            string originalLine = @event.DialogueLines[0].Text;

            @event.EditDialogueLine(0, $"{originalLine}あ");
            @event.EditDialogueLine(0, $"{originalLine}");

            Assert.AreEqual(eventFileOnDisk, @event.GetBytes());
        }

        [Test]
        // This file can be ripped directly from the ROM
        [TestCase(".\\inputs\\evt.bin")]
        public void EvtFileParserTest(string evtFile)
        {
            ArchiveFile<EventFile> evt = ArchiveFile<EventFile>.FromFile(evtFile, _log);
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
