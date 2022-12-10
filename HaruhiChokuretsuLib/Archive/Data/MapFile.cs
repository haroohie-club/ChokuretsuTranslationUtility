using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HaruhiChokuretsuLib.Archive.Data
{
    public class MapFile : DataFile
    {
        public int NumSections { get; set; }
        public int EndPointersOffset { get; set; }
        public int HeaderEndPointer { get; set; }
        public List<(int Offset, int ItemCount)> SectionOffsetsAndCounts { get; set; } = new();

        public MapFileSettings Settings { get; set; }

        public override void Initialize(byte[] decompressedData, int offset)
        {
            Offset = offset;
            Data = decompressedData.ToList();

            NumSections = BitConverter.ToInt32(Data.Take(4).ToArray());
            EndPointersOffset = BitConverter.ToInt32(Data.Skip(0x04).Take(4).ToArray());
            HeaderEndPointer = BitConverter.ToInt32(Data.Skip(0x08).Take(4).ToArray());
            for (int i = 0x0C; i < HeaderEndPointer; i += 0x08)
            {
                SectionOffsetsAndCounts.Add((BitConverter.ToInt32(Data.Skip(i).Take(4).ToArray()), BitConverter.ToInt32(Data.Skip(i + 4).Take(4).ToArray())));
            }

            Settings = new(Data.Skip(SectionOffsetsAndCounts[0].Offset).Take(0xBC));
        }

        public (SKBitmap, SKBitmap) GetMapImages(ArchiveFile<GraphicsFile> grp)
        {
            List<GraphicsFile> textures = Settings.TextureFileIndices.Select(i => grp.Files.First(f => f.Index == i)).ToList();
            GraphicsFile layout = grp.Files.First(f => f.Index == Settings.LayoutFileIndex);

            //int mapEndIndex = Settings.OverlayLayoutStartIndex == 0 && Settings.OverlayLayoutEndIndex == 0 ? Settings.ForegroundLayoutEndIndex - Settings.ForegroundLayoutStartIndex + 1 : Settings.OverlayLayoutEndIndex - Settings.ForegroundLayoutStartIndex + 1;
            int mapEndIndex = Settings.BackgroundLayoutStartIndex == 0 ? layout.Length : Settings.BackgroundLayoutStartIndex;

            SKBitmap mapBitmap = layout.GetLayout(textures, Settings.ForegroundLayoutStartIndex, mapEndIndex, false, true).bitmap;
            SKBitmap bgBitmap = null;

            if (Settings.BackgroundLayoutStartIndex != 0 || Settings.BackgroundLayoutEndIndex != 0)
            {
                bgBitmap = layout.GetLayout(textures, Settings.BackgroundLayoutStartIndex, Settings.BackgroundLayoutEndIndex - Settings.BackgroundLayoutStartIndex + 1, false, true).bitmap;
            }

            return (mapBitmap, bgBitmap);
        }
    }

    public class MapFileSettings
    {
        public int Unknown00 { get; set; }
        public int MapWidth { get; set; }
        public int MapHeight { get; set; }
        public List<short> TextureFileIndices { get; set; } = new();
        public short LayoutFileIndex { get; set; }
        public int ForegroundLayoutStartIndex { get; set; }
        public int Unknown18 { get; set; }
        public int UnknownLayoutIndex1C { get; set; }
        public int UnknownLayoutIndex20 { get; set; }
        public int UnknownLayoutIndex24 { get; set; }
        public int Unknown28 { get; set; }
        public int BackgroundLayoutStartIndex { get; set; }
        public int BackgroundLayoutEndIndex { get; set; }

        public MapFileSettings(IEnumerable<byte> data)
        {
            Unknown00 = BitConverter.ToInt32(data.Take(4).ToArray());
            MapWidth = BitConverter.ToInt32(data.Skip(0x04).Take(4).ToArray());
            MapHeight = BitConverter.ToInt32(data.Skip(0x08).Take(4).ToArray());
            TextureFileIndices.Add(BitConverter.ToInt16(data.Skip(0x0C).Take(2).ToArray()));
            TextureFileIndices.Add(BitConverter.ToInt16(data.Skip(0x10).Take(2).ToArray()));
            TextureFileIndices.Add(BitConverter.ToInt16(data.Skip(0x0E).Take(2).ToArray()));
            LayoutFileIndex = BitConverter.ToInt16(data.Skip(0x12).Take(2).ToArray());
            ForegroundLayoutStartIndex = BitConverter.ToInt32(data.Skip(0x14).Take(4).ToArray());
            Unknown18 = BitConverter.ToInt32(data.Skip(0x18).Take(4).ToArray());
            UnknownLayoutIndex1C = BitConverter.ToInt32(data.Skip(0x1C).Take(4).ToArray());
            UnknownLayoutIndex20 = BitConverter.ToInt32(data.Skip(0x20).Take(4).ToArray());
            UnknownLayoutIndex24 = BitConverter.ToInt32(data.Skip(0x24).Take(4).ToArray());
            Unknown28 = BitConverter.ToInt32(data.Skip(0x28).Take(4).ToArray());
            BackgroundLayoutStartIndex = BitConverter.ToInt32(data.Skip(0x2C).Take(4).ToArray());
            BackgroundLayoutEndIndex = BitConverter.ToInt32(data.Skip(0x30).Take(4).ToArray());
        }
    }
}
