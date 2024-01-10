using HaruhiChokuretsuLib.Util;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System;
using System.Collections.Generic;
using System.IO;

namespace HaruhiChokuretsuTests
{
    public class CompressionTests
    {
        [Test]
        [TestCase("evt_000", TestVariables.EVT_000_COMPRESSED, TestVariables.EVT_000_DECOMPRESSED)]
        [TestCase("grp_test", TestVariables.GRP_TEST_COMPRESSED, TestVariables.GRP_TEST_DECOMPRESSED)]
        public void AsmSimulatorTest(string filePrefix, string compressedFile, string decompressedFile)
        {
            byte[] compressedData = File.ReadAllBytes(compressedFile);
            AsmDecompressionSimulator asm = new(compressedData);

            byte[] decompressedDataOnDisk = File.ReadAllBytes(decompressedFile);
            File.WriteAllBytes($".\\inputs\\{filePrefix}_asm_decomp.bin", asm.Output);
            ClassicAssert.AreEqual(StripZeroes(decompressedDataOnDisk), StripZeroes(asm.Output));
        }

        [Test]
        [TestCase("evt_000", TestVariables.EVT_000_COMPRESSED, TestVariables.EVT_000_DECOMPRESSED)]
        [TestCase("evt_66", TestVariables.EVT_66_COMPRESSED, TestVariables.EVT_66_DECOMPRESSED)]
        [TestCase("evt_memorycard", TestVariables.EVT_MEMORYCARD_COMPRESSED, TestVariables.EVT_MEMORYCARD_DECOMPRESSED)]
        [TestCase("evt_test_orig", TestVariables.EVT_TEST_ORIG_COMP, TestVariables.EVT_TEST_DECOMPRESSED)]
        [TestCase("evt_test_prog", TestVariables.EVT_TEST_PROG_COMP, TestVariables.EVT_TEST_DECOMPRESSED)]
        [TestCase("grp_c1a", TestVariables.GRP_C1A_COMPRESSED, TestVariables.GRP_C1A_DECOMPRESSED)]
        [TestCase("grp_test", TestVariables.GRP_TEST_COMPRESSED, TestVariables.GRP_TEST_DECOMPRESSED)]
        public void DecompressionMethodTest(string filePrefix, string compressedFile, string decompressedFile)
        {
            byte[] decompressedDataInMemory = Helpers.DecompressData(File.ReadAllBytes(compressedFile));
            File.WriteAllBytes($".\\inputs\\{filePrefix}_prog_decomp.bin", decompressedDataInMemory);

            byte[] decompressedDataOnDisk = File.ReadAllBytes(decompressedFile);
            ClassicAssert.AreEqual(StripZeroes(decompressedDataOnDisk), StripZeroes(decompressedDataInMemory));
        }

        [Test]
        [TestCase("evt_000", TestVariables.EVT_000_DECOMPRESSED, TestVariables.EVT_000_COMPRESSED)]
        [TestCase("evt_66", TestVariables.EVT_66_DECOMPRESSED, TestVariables.EVT_66_COMPRESSED)]
        [TestCase("evt_memorycard", TestVariables.EVT_MEMORYCARD_DECOMPRESSED, TestVariables.EVT_MEMORYCARD_COMPRESSED, false)]
        [TestCase("grp_c1a", TestVariables.GRP_C1A_DECOMPRESSED, TestVariables.GRP_C1A_COMPRESSED, false)]
        [TestCase("evt_test", TestVariables.EVT_TEST_DECOMPRESSED, TestVariables.GRP_TEST_COMPRESSED)]
        [TestCase("grp_test", TestVariables.GRP_TEST_DECOMPRESSED, TestVariables.GRP_TEST_COMPRESSED)]
        public void CompressionMethodTest(string filePrefix, string decompressedFile, string originalCompressedFile, bool runAsm = true)
        {
            byte[] decompressedDataOnDisk = File.ReadAllBytes(decompressedFile);
            byte[] compressedData = Helpers.CompressData(decompressedDataOnDisk);
            File.WriteAllBytes($".\\inputs\\{filePrefix}_prog_comp.bin", compressedData);

            if (!string.IsNullOrEmpty(originalCompressedFile))
            {
                Console.WriteLine($"Original compression ratio: {(double)File.ReadAllBytes(originalCompressedFile).Length / decompressedDataOnDisk.Length * 100}%");
            }
            Console.WriteLine($"Our compression ratio: {(double)compressedData.Length / decompressedDataOnDisk.Length * 100}%");

            byte[] decompressedDataInMemory = Helpers.DecompressData(compressedData);
            File.WriteAllBytes($".\\inputs\\{filePrefix}_prog_decomp.bin", decompressedDataInMemory);
            ClassicAssert.AreEqual(StripZeroes(decompressedDataOnDisk), StripZeroes(decompressedDataInMemory), message: "Failed in implementation.");

            if (runAsm)
            {
                byte[] decompressedDataViaAsm = new AsmDecompressionSimulator(compressedData).Output;
                File.WriteAllBytes($".\\inputs\\{filePrefix}_asm_decomp.bin", decompressedDataViaAsm);
                ClassicAssert.AreEqual(StripZeroes(decompressedDataOnDisk), StripZeroes(decompressedDataViaAsm), message: "Failed in assembly simulation.");
            }
        }

        public static byte[] StripZeroes(byte[] array)
        {
            List<byte> strippedArray = new(array);

            for (int i = strippedArray.Count - 1; strippedArray[i] == 0; i--)
            {
                strippedArray.RemoveAt(i);
            }

            return strippedArray.ToArray();
        }
    }
}