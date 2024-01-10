using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HaruhiChokuretsuLib.Util
{
    /// <summary>
    /// Helper methods
    /// </summary>
    public static class Helpers
    {
        /// <summary>
        /// Rounds to the next power of two
        /// </summary>
        /// <param name="n">Number to round</param>
        /// <returns>The next power of two past that number</returns>
        public static int NextPowerOf2(int n)
        {
            n--;
            n |= n >> 1;
            n |= n >> 2;
            n |= n >> 4;
            n |= n >> 8;
            n |= n >> 16;
            n++;

            return n;
        }

        /// <summary>
        /// Gets a string indent
        /// </summary>
        /// <param name="indent">Number of spaces to indent</param>
        /// <returns>A string composed of that many spaces</returns>
        public static string Indent(int indent)
        {
            return string.Join(' ', new string[indent + 1]);
        }

        /// <summary>
        /// Escapes Shift-JIS for inclusion in ARM assembly files
        /// </summary>
        /// <param name="shiftJisString">A Shift-JIS string</param>
        /// <returns>An escaped string ready to include in ARM assembly</returns>
        public static string EscapeShiftJIS(this string shiftJisString)
        {
            return string.Join("", Encoding.GetEncoding("Shift-JIS").GetBytes(shiftJisString).Select(b => $"\\x{b:X2}"));
        }

        /// <summary>
        /// Gets the length of a Shift-JIS string in bytess
        /// </summary>
        /// <param name="shiftJisString">A Shift-JIS string</param>
        /// <returns>Length of the string in bytes</returns>
        public static int GetShiftJISLength(this string shiftJisString)
        {
            return Encoding.GetEncoding("Shift-JIS").GetByteCount(shiftJisString) + 1; // +1 for trailing \x00
        }

        /// <summary>
        /// Pads the end of a string in an assembly source file with the necessary number of bytes to maintain four-byte alignment
        /// </summary>
        /// <param name="sb">The StringBuilder instance being used to build the source file</param>
        /// <param name="str">The string to pad</param>
        /// <param name="encoding">The encoding of the string</param>
        public static void AsmPadString(this StringBuilder sb, string str, Encoding encoding)
        {
            int neededPadding = 4 - encoding.GetByteCount(str) % 4 - 1;
            if (neededPadding > 0)
            {
                sb.AppendLine($".skip {neededPadding}");
            }
        }

        /// <summary>
        /// Finds the greatest common factor between two integers
        /// </summary>
        /// <param name="a">First integer</param>
        /// <param name="b">Second integer</param>
        /// <returns>The GCF of the two provided integers</returns>
        public static int GreatestCommonFactor(int a, int b)
        {
            while (b != 0)
            {
                (a, b) = (b, a % b);
            }

            return a;
        }

        /// <summary>
        /// Finds the least common multiple of two integers
        /// </summary>
        /// <param name="a">First integer</param>
        /// <param name="b">Second integer</param>
        /// <returns>The LCM of the two provided integers</returns>
        public static int LeastCommonMultiple(int a, int b)
        {
            return a / GreatestCommonFactor(a, b) * b;
        }

        /// <summary>
        /// Finds the least common multiple of a set of integers
        /// </summary>
        /// <param name="list">The list of integers</param>
        /// <returns>The LCM of the entire provided list of integers</returns>
        public static int LeastCommonMultiple(IEnumerable<int> list)
        {
            int lcm = list.ElementAt(0);
            for (int i = 1; i < list.Count(); i++)
            {
                lcm = LeastCommonMultiple(lcm, list.ElementAt(i));
            }

            return lcm;
        }

        /// <summary>
        /// Swaps two entries in a list
        /// </summary>
        /// <typeparam name="T">Type of the list</typeparam>
        /// <param name="list">The list in which to swap two items</param>
        /// <param name="firstIndex">First index to swap</param>
        /// <param name="secondIndex">Second index to swap</param>
        /// <param name="numToSwapAtOnce">Number of items to swap at once</param>
        /// <returns></returns>
        public static IList<T> Swap<T>(this IList<T> list, int firstIndex, int secondIndex, int numToSwapAtOnce = 1)
        {
            for (int i = 0; i < numToSwapAtOnce; i++)
            {
                (list[secondIndex + i], list[firstIndex + i]) = (list[firstIndex + i], list[secondIndex + i]);
            }

            return list;
        }

        /// <summary>
        /// Rotates a section of a list left
        /// </summary>
        /// <typeparam name="T">The type of the list</typeparam>
        /// <param name="enumerable">The list to rotate within</param>
        /// <param name="index">The starting index of rotation</param>
        /// <param name="length">The length of the section to rotate</param>
        /// <returns>The rotated enumerable</returns>
        public static IEnumerable<T> RotateSectionLeft<T>(this IEnumerable<T> enumerable, int index, int length)
        {
            T[] array = enumerable.ToArray();
            T firstItem = array[index];
            for (int i = 1; i < length; i++)
            {
                array[index + i - 1] = array[index + i];
            }
            array[index + length - 1] = firstItem;

            return array.AsEnumerable();
        }

        /// <summary>
        /// Rotates a section of a list right
        /// </summary>
        /// <typeparam name="T">The type of the list</typeparam>
        /// <param name="enumerable">The list to rotate within</param>
        /// <param name="index">The starting index of rotation</param>
        /// <param name="length">The length of the section to rotate</param>
        /// <returns>The rotated enumerable</returns>
        public static IEnumerable<T> RotateSectionRight<T>(this IEnumerable<T> enumerable, int index, int length)
        {
            T[] array = enumerable.ToArray();
            T lastItem = array[index + length - 1];
            for (int i = length - 1; i > 0; i--)
            {
                array[index + i] = array[index + i - 1];
            }
            array[index] = lastItem;

            return array.AsEnumerable();
        }

        // redmean color distance formula with alpha term
        /// <summary>
        /// Color distance calculated with redmean formula
        /// </summary>
        /// <param name="color1">First color</param>
        /// <param name="color2">Second color</param>
        /// <returns>Distance between the two colors</returns>
        public static double ColorDistance(SKColor color1, SKColor color2)
        {
            double redmean = (color1.Red + color2.Red) / 2.0;

            return Math.Sqrt((2 + redmean / 256) * Math.Pow(color1.Red - color2.Red, 2)
                + 4 * Math.Pow(color1.Green - color2.Green, 2)
                + (2 + (255 - redmean) / 256) * Math.Pow(color1.Blue - color2.Blue, 2)
                + Math.Pow(color1.Alpha - color2.Alpha, 2));
        }

        /// <summary>
        /// Finds the closet color in a palette to a given color
        /// </summary>
        /// <param name="colors">The palette of colors to search within</param>
        /// <param name="color">The color to try to match</param>
        /// <param name="firstTransparent">Whether to ignore the first entry as it's transparent</param>
        /// <returns></returns>
        public static int ClosestColorIndex(IList<SKColor> colors, SKColor color, bool firstTransparent)
        {
            int skip = firstTransparent ? 1 : 0;
            var colorDistances = colors.Skip(skip).Select(c => ColorDistance(c, color)).ToList();

            return colorDistances.IndexOf(colorDistances.Min());
        }

        /// <summary>
        /// Converts an RGB555 value to an SKColor
        /// </summary>
        /// <param name="rgb555">Color formatted as an RGB555 16-bit integer</param>
        /// <returns>An equivalent SKColor</returns>
        public static SKColor Rgb555ToSKColor(short rgb555)
        {
            return new SKColor((byte)((rgb555 & 0x1F) << 3), (byte)((rgb555 >> 5 & 0x1F) << 3), (byte)((rgb555 >> 10 & 0x1F) << 3));
        }

        /// <summary>
        /// Converts an SKColor to an RGB555
        /// </summary>
        /// <param name="color">An SKColor</param>
        /// <returns>An equivalent RGB555 value</returns>
        public static short SKColorToRgb555(SKColor color)
        {
            return (short)(color.Red >> 3 | color.Green << 2 | color.Blue << 7);
        }

        /// <summary>
        /// Determines whether an addition will cause a carry to take place
        /// </summary>
        /// <param name="x">First addend</param>
        /// <param name="y">Second addend</param>
        /// <returns>True if carry occurs, false otherwise</returns>
        public static bool AddWillCauseCarry(int x, int y)
        {
            return ((x & 0xFFFFFFFFFL) + (y & 0xFFFFFFFFL) & 0x1000000000) > 0;
        }

        /// <summary>
        /// Finds the index of a sequence inside another sequence
        /// </summary>
        /// <typeparam name="T">Type of enumerable</typeparam>
        /// <param name="items">The enumerable to search within</param>
        /// <param name="search">The pattern to search for</param>
        /// <returns>The index of the pattern if found, -1 otherwise</returns>
        public static int IndexOfSequence<T>(this IEnumerable<T> items, IEnumerable<T> search)
        {
            int searchLength = search.Count();
            int lastIndex = items.Count() - searchLength;
            for (int i = 0; i < lastIndex; i++)
            {
                if (items.Skip(i).Take(searchLength).SequenceEqual(search))
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Gets a palette from a set of images
        /// </summary>
        /// <param name="bitmaps">A set of images to extract a palette from</param>
        /// <param name="numberOfColors">The number of colors in the palette</param>
        /// <param name="log">An ILogger instance</param>
        /// <returns></returns>
        public static List<SKColor> GetPaletteFromImages(IList<SKBitmap> bitmaps, int numberOfColors, ILogger log)
        {
            PnnQuantizer quantizer = new();
            return quantizer.GetPaletteFromImages(bitmaps, numberOfColors, log);
        }

        /// <summary>
        /// Gets a palette from a single image
        /// </summary>
        /// <param name="bitmap">The image to extract a palette from</param>
        /// <param name="numberOfColors">The number of colors in the palette</param>
        /// <param name="log">An ILogger instance</param>
        /// <returns></returns>
        public static List<SKColor> GetPaletteFromImage(SKBitmap bitmap, int numberOfColors, ILogger log)
        {
            PnnQuantizer quantizer = new();
            return quantizer.GetPaletteFromImages(new SKBitmap[] { bitmap }, numberOfColors, log);
        }

        /// <summary>
        /// Compression implementation of the Shade compression algorithm
        /// </summary>
        /// <param name="decompressedData">Binary data to compress</param>
        /// <returns>A byte array of compressed data</returns>
        public static byte[] CompressData(byte[] decompressedData)
        {
            // nonsense hack to deal with a rare edge case where the last byte of a file could get dropped
            List<byte> temp = [.. decompressedData];
            temp.Add(0x00);
            decompressedData = [.. temp];

            List<byte> compressedData = new();

            int directBytesToWrite = 0;
            Dictionary<LookbackEntry, List<int>> lookbackDictionary = new();
            for (int i = 0; i < decompressedData.Length;)
            {
                int numNext = Math.Min(decompressedData.Length - i - 1, 4);
                if (numNext == 0)
                {
                    break;
                }

                List<byte> nextBytes = decompressedData.Skip(i).Take(numNext).ToList();
                LookbackEntry nextEntry = new(nextBytes, i);
                if (lookbackDictionary.ContainsKey(nextEntry) && i - lookbackDictionary[nextEntry].Max() <= 0x1FFF)
                {
                    if (directBytesToWrite > 0)
                    {
                        WriteDirectBytes(decompressedData, compressedData, i, directBytesToWrite);
                        directBytesToWrite = 0;
                    }

                    int lookbackIndex = 0;
                    int longestSequenceLength = 0;
                    foreach (int index in lookbackDictionary[nextEntry])
                    {
                        if (i - index <= 0x1FFF)
                        {
                            List<byte> lookbackSequence = new();
                            for (int j = 0; i + j < decompressedData.Length && decompressedData[index + j] == decompressedData[i + j]; j++)
                            {
                                lookbackSequence.Add(decompressedData[lookbackIndex + j]);
                            }
                            if (lookbackSequence.Count > longestSequenceLength)
                            {
                                longestSequenceLength = lookbackSequence.Count;
                                lookbackIndex = index;
                            }
                        }
                    }
                    lookbackDictionary[nextEntry].Add(i);

                    int encodedLookbackIndex = i - lookbackIndex;
                    int encodedLength = longestSequenceLength - 4;
                    int remainingEncodedLength = 0;
                    if (encodedLength > 3)
                    {
                        remainingEncodedLength = encodedLength - 3;
                        encodedLength = 3;
                    }
                    byte firstByte = (byte)(encodedLookbackIndex / 0x100 | encodedLength << 5 | 0x80);
                    byte secondByte = (byte)(encodedLookbackIndex & 0xFF);
                    compressedData.AddRange(new byte[] { firstByte, secondByte });
                    if (remainingEncodedLength > 0)
                    {
                        while (remainingEncodedLength > 0)
                        {
                            int currentEncodedLength = Math.Min(remainingEncodedLength, 0x1F);
                            remainingEncodedLength -= currentEncodedLength;

                            compressedData.Add((byte)(0x60 | currentEncodedLength));
                        }
                    }

                    i += longestSequenceLength;
                }
                else if (nextBytes.Count == 4 && nextBytes.All(b => b == nextBytes[0]))
                {
                    if (directBytesToWrite > 0)
                    {
                        WriteDirectBytes(decompressedData, compressedData, i, directBytesToWrite);
                        directBytesToWrite = 0;
                    }

                    List<byte> repeatedBytes = decompressedData.Skip(i).TakeWhile(b => b == nextBytes[0]).ToList();
                    int numRepeatedBytes = Math.Min(0x1F3, repeatedBytes.Count);
                    if (numRepeatedBytes <= 0x13)
                    {
                        compressedData.Add((byte)(0x40 | numRepeatedBytes - 4)); // 0x40 -- repeated byte, 4-bit length
                    }
                    else
                    {
                        int numToEncode = numRepeatedBytes - 4;
                        int msb = numToEncode & 0xF00;
                        byte firstByte = (byte)(0x50 | msb / 0x100);
                        byte secondByte = (byte)(numToEncode - msb); // 0x50 -- repeated byte, 12-bit length
                        compressedData.AddRange(new byte[] { firstByte, secondByte });
                    }
                    compressedData.Add(repeatedBytes[0]);
                    i += numRepeatedBytes;
                }
                else
                {
                    if (directBytesToWrite + numNext > 0x1FFF)
                    {
                        WriteDirectBytes(decompressedData, compressedData, i, directBytesToWrite);
                        directBytesToWrite = 0;
                    }
                    if (!lookbackDictionary.ContainsKey(nextEntry))
                    {
                        lookbackDictionary.Add(nextEntry, new List<int> { i });
                    }
                    else
                    {
                        lookbackDictionary[nextEntry].Add(i);
                    }
                    directBytesToWrite++;
                    i++;
                }
            }

            if (directBytesToWrite > 0)
            {
                WriteDirectBytes(decompressedData, compressedData, decompressedData.Length - 1, directBytesToWrite);
            }

            return [.. compressedData];
        }

        private class LookbackEntry
        {
            public byte[] Bytes { get; set; }

            public LookbackEntry(List<byte> bytes, int index)
            {
                Bytes = [.. bytes];
            }

            public override bool Equals(object obj)
            {
                var other = (LookbackEntry)obj;
                if (other.Bytes.Length != Bytes.Length)
                {
                    return false;
                }
                bool equals = true;
                for (int i = 0; i < Bytes.Length; i++)
                {
                    equals = equals && Bytes[i] == other.Bytes[i];
                }
                return equals;
            }

            public override int GetHashCode()
            {
                string hash = "";
                foreach (byte @byte in Bytes)
                {
                    hash += $"{@byte}";
                }
                return hash.GetHashCode();
            }
        }

        private static void WriteDirectBytes(byte[] writeFrom, List<byte> writeTo, int position, int numBytesToWrite)
        {
            if (numBytesToWrite < 0x20)
            {
                writeTo.Add((byte)numBytesToWrite);
            }
            else
            {
                int msb = 0x1F00 & numBytesToWrite;
                byte firstByte = (byte)(0x20 | msb / 0x100);
                byte secondByte = (byte)(numBytesToWrite - msb);
                writeTo.AddRange(new byte[] { firstByte, secondByte });
            }
            writeTo.AddRange(writeFrom.Skip(position - numBytesToWrite).Take(numBytesToWrite));
        }

        /// <summary>
        /// Decompression implementation of the Shade compression algorithm
        /// </summary>
        /// <param name="compressedData">Compressed data to decompress</param>
        /// <returns>A byte array of decompressed data</returns>
        public static byte[] DecompressData(byte[] compressedData)
        {
            List<byte> decompressedData = new();

            // documentation note: bits 1234 5678 in a byte
            for (int z = 0; z < compressedData.Length;)
            {
                int blockByte = compressedData[z++];
                if (blockByte == 0)
                {
                    break;
                }

                if ((blockByte & 0x80) == 0)
                {
                    if ((blockByte & 0x40) == 0)
                    {
                        // bits 1 & 2 == 0 --> direct data read
                        int numBytes;
                        if ((blockByte & 0x20) == 0)
                        {
                            numBytes = blockByte; // the `& 0x1F` is unnecessary since we've already determined bits 1-3 to be 0
                        }
                        else
                        {
                            // bit 3 == 1 --> need two bytes to indicate how much data to read
                            numBytes = compressedData[z++] + (blockByte & 0x1F) * 0x100;
                        }
                        for (int i = 0; i < numBytes; i++)
                        {
                            decompressedData.Add(compressedData[z++]);
                        }
                    }
                    else
                    {
                        // bit 1 == 0 && bit 2 == 1 --> repeated byte
                        int numBytes;
                        if ((blockByte & 0x10) == 0)
                        {
                            numBytes = (blockByte & 0x0F) + 4;
                        }
                        else
                        {
                            numBytes = compressedData[z++] + (blockByte & 0x0F) * 0x100 + 4;
                        }
                        byte repeatedByte = compressedData[z++];
                        for (int i = 0; i < numBytes; i++)
                        {
                            decompressedData.Add(repeatedByte);
                        }
                    }
                }
                else
                {
                    // bit 1 == 1 --> backreference
                    int numBytes = ((blockByte & 0x60) >> 0x05) + 4;
                    int backReferenceIndex = decompressedData.Count - (compressedData[z++] + (blockByte & 0x1F) * 0x100);
                    for (int i = backReferenceIndex; i < backReferenceIndex + numBytes; i++)
                    {
                        decompressedData.Add(decompressedData[i]);
                    }
                    while ((compressedData[z] & 0xE0) == 0x60)
                    {
                        int nextNumBytes = compressedData[z++] & 0x1F;
                        if (nextNumBytes > 0)
                        {
                            for (int i = backReferenceIndex + numBytes; i < backReferenceIndex + numBytes + nextNumBytes; i++)
                            {
                                decompressedData.Add(decompressedData[i]);
                            }
                        }
                        backReferenceIndex += nextNumBytes;
                    }
                }
            }

            // nonsense hack which corresponds to above nonsense hack
            if (decompressedData.Count % 16 == 1 && decompressedData.Last() == 0x00)
            {
                decompressedData.RemoveAt(decompressedData.Count - 1);
            }

            while (decompressedData.Count % 0x10 != 0)
            {
                decompressedData.Add(0x00);
            }
            return [.. decompressedData];
        }
    }

    internal class AsmDecompressionSimulator
    {
        private int z, c, l, n;
        private List<byte> _output = new();
        private byte[] _data;

        public byte[] Output { get { return [.. _output]; } }

        public AsmDecompressionSimulator(byte[] data)
        {
            _data = data;
            z = 0;
            Lxx_2026198();
        }

        private void Lxx_2026198()
        {
            if (z >= _data.Length)
            {
                return;
            }
            c = _data[z++];     // ldrb     r3,[r0],1h
            if (c == 0)         // cmp      r3,0h
            {
                return;         // beq      Lxx_20262A0h
            }
            if ((c & 0x80) == 0)
            {
                Lxx_2026224();
            }
            if (z >= _data.Length)
            {
                return;
            }
            l = _data[z++];                 // ldrb     r12,[r0],1h
            n = c & 0x60;                   // and      r14,r3,60h
            c = (int)((uint)c << 0x1B);     // mov      r3,r3,lsl 1Bh
            n >>= 0x05;                     // mov      r14,r14,asr 5h
            c = l | (int)((uint)c >> 0x13); // orr      r3,r12,r3,lsr 13h
            l = n + 4;                      // add      r12,r14,4h
            n = _output.Count - c;          // sub      r14,r1,r3
            Lxx_20261C8();
        }

        private void Lxx_20261C8()
        {
            while (l > 0)               // bgt      Lxx_20261C8h
            {
                if (n >= _output.Count)
                {
                    return;
                }
                c = _output[n++];       // ldrb     r3,[r14],1h
                l--;                    // sub      r12,r12,1h
                _output.Add((byte)c);   // strb     r3,[r1],1h     
            }
            c = _data[z];               // ldrb     r3,[r0]
            c &= 0xE0;                  // and      r3,r3,0E0h
            if (c != 0x60)              // cmp      r3,60h
            {
                Lxx_2026198();          // bne      Lxx_2026198h
            }
            Lxx_20261EC();
        }

        private void Lxx_20261EC()
        {
            if (z >= _data.Length)
            {
                return;
            }
            c = _data[z++];             // ldrb     r3,[r0],1h
            l = c & 0x1F;               // and      r12,r3,1Fh
            if (l <= 0)                 // cmp      r12,0h
            {
                Lxx_2026210();          // ble      Lxx_2026210h
            }
            Lxx_20261FC();
        }

        private void Lxx_20261FC()
        {
            while (l > 0)               // bgt      Lxx_20261FCh (self)
            {
                c = _output[n++];       // ldrb     r3,[r14],1h
                l--;                    // sub      r12,r12,1h
                _output.Add((byte)c);   // strb     r3,[r1],1h
            }
            Lxx_2026210();
        }

        private void Lxx_2026210()
        {
            if (z >= _data.Length)
            {
                return;
            }
            c = _data[z];               // ldrb     r3,[r0]
            c &= 0xE0;                  // and      r3,r3,0E0h
            if (c == 0x60)              // cmp      r3,60h
            {
                Lxx_20261EC();          // beq      Lxx_20261ECh
            }
            Lxx_2026198();              // b        Lxx_2026198h
        }

        private void Lxx_2026224()
        {
            if ((c & 0x40) == 0)            // tst      r3,40h
            {
                Lxx_2026268();              // beq      Lxx_2026268h
            }
            if ((c & 0x10) == 0)            // tst      r3,10h
            {
                c &= 0x0F;                  // andeq    r3,r3,0Fh
                Lxx_2026244();              // beq      Lxx_2026244h
            }
            if (z >= _data.Length)
            {
                return;
            }
            l = _data[z++];                 // ldrb     r12,[r0],1h
            c = (int)((uint)c << 0x1C);     // mov      r3,r3,lsl 1Ch
            c = l | (int)((uint)c >> 0x14); // orr      r3,r12,r3,lsr 14h
            Lxx_2026244();
        }

        private void Lxx_2026244()
        {
            l = c + 4;                  // add      r12,r3,4h
            if (z >= _data.Length)
            {
                return;
            }
            c = _data[z++];             // ldrb     r3,[r0],1h
            if (l <= 0)                 // cmp      r12,0h
            {
                Lxx_2026198();          // ble      Lxx_2026198h
            }
            Lxx_2026254();
        }

        private void Lxx_2026254()
        {
            while (l > 0)               // bgt      Lxx_2026254h
            {
                l--;                    // sub      r12,r12,1h
                _output.Add((byte)c);   // strb     r3,[r1],1h
            }
            Lxx_2026198();              // b        Lxx_2026198h
        }

        private void Lxx_2026268()
        {
            if ((c & 0x20) == 0)        // tst      r3,20h
            {
                l = c & 0x1F;           // andeq    r12,r3,1Fh
                Lxx_2026280();          // beq      Lxx_2026280h
            }
            if (z >= _data.Length)
            {
                return;
            }
            l = _data[z++];             // ldrb     r12,[r0],1h
            c = (int)((uint)c << 0x1B); // mov      r3,r3,lsl 1Bh
            l |= (int)((uint)c >> 0x13);// orr      r12,r12,r3,lsr 13h
            Lxx_2026280();
        }

        private void Lxx_2026280()
        {
            if (l <= 0)                 // cmp      r12,0h
            {
                Lxx_2026198();          // ble      Lxx_2026198h
            }
            while (l > 0)               // bgt      Lxx_2026288h
            {
                c = _data[z++];         // ldrb     r3,[r0],1h
                l--;                    // sub      r12,r12,1h
                _output.Add((byte)c);   // strb     r3,[r1],1h
            }
            Lxx_2026198();
        }
    }
}
