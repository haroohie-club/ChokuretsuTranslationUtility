using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HaruhiChokuretsuLib.Archive.Data
{
    public class MapFile : DataFile
    {
        public int NumSections { get; set; }
        public int EndPointersOffset { get; set; }
        public int HeaderEndPointer { get; set; }
        public List<(int Offset, int ItemCount)> SectionOffsetsAndCounts { get; set; } = new();

        public MapFileSettings Settings { get; set; }
        public byte[][] PathingMap { get; set; }

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
            int currentPathingByte = 0;
            byte[] pathingBytes = Data.Skip(SectionOffsetsAndCounts[4].Offset).Take(SectionOffsetsAndCounts[4].ItemCount).ToArray();
            if (Settings.YOriented)
            {
                PathingMap = new byte[Settings.MapWidth][];
                for (int x = 0; x < Settings.MapWidth; x++)
                {
                    PathingMap[x] = new byte[Settings.MapHeight];

                    for (int y = 0; y < Settings.MapHeight; y++)
                    {
                        PathingMap[x][y] = pathingBytes[currentPathingByte++];
                    }
                }
            }
            else
            {
                PathingMap = new byte[Settings.MapHeight][];
                for (int y = 0; y < Settings.MapHeight; y++)
                {
                    PathingMap[y] = new byte[Settings.MapWidth];

                    for (int x = 0; x < Settings.MapWidth; x++)
                    {
                        PathingMap[y][x] = pathingBytes[currentPathingByte++];
                    }
                }
            }
        }

        public (SKBitmap mapBitmap, SKBitmap bgBitmap) GetMapImages(ArchiveFile<GraphicsFile> grp, int maxLayoutIndex = -1, GraphicsFile grpReplacement = null, int replacementIndex = 1)
        {
            List<GraphicsFile> textures = Settings.TextureFileIndices.Select(i => grp.Files.First(f => f.Index == i)).ToList();
            if (grpReplacement is not null)
            {
                textures[replacementIndex] = grpReplacement;
            }
            GraphicsFile layout = grp.Files.First(f => f.Index == Settings.LayoutFileIndex);

            int mapEndIndex = maxLayoutIndex >= 0 ? maxLayoutIndex : (Settings.BackgroundLayoutStartIndex == 0 ? layout.Length : Settings.BackgroundLayoutStartIndex);

            SKBitmap mapBitmap = layout.GetLayout(textures, Settings.ForegroundLayoutStartIndex, mapEndIndex, false, true).bitmap;
            SKBitmap bgBitmap = null;

            if (Settings.BackgroundLayoutStartIndex != 0 || Settings.BackgroundLayoutEndIndex != 0)
            {
                bgBitmap = layout.GetLayout(textures, Settings.BackgroundLayoutStartIndex, Settings.BackgroundLayoutEndIndex - Settings.BackgroundLayoutStartIndex + 1, false, true).bitmap;
            }

            return (mapBitmap, bgBitmap);
        }

        public SKBitmap GetMapImages(ArchiveFile<GraphicsFile> grp, int start, int length)
        {
            List<GraphicsFile> textures = Settings.TextureFileIndices.Select(i => grp.Files.First(f => f.Index == i)).ToList();
            GraphicsFile layout = grp.Files.First(f => f.Index == Settings.LayoutFileIndex);

            return layout.GetLayout(textures, start, length, false, true).bitmap;
        }

        public SKBitmap GetPathingImage()
        {
            SKBitmap pathingImage;
            pathingImage = new((Settings.MapWidth + Settings.MapHeight) * 10, (Settings.MapWidth + Settings.MapHeight) * 10);
            SKCanvas canvas = new(pathingImage);

            SKPathEffect rotatePathEffect = SKPathEffect.Create1DPath(
                SKPath.ParseSvgPathData("M -5 0 L 0 -5, 5 0, 0 5 Z"), 20, 0, SKPath1DPathEffectStyle.Rotate);
            
            if (Settings.YOriented)
            {
                for (int x = 0; x < PathingMap.Length; x++)
                {
                    for (int y = 0; y < PathingMap[x].Length; y++)
                    {
                        canvas.DrawRect(((Settings.MapWidth + Settings.MapHeight) * 10 / 2) - (x * 5) + (y * 5), (y * 5) + (x * 5) + 5, 5, 5, GetPathingSpacePaint(x, y, rotatePathEffect));
                        canvas.Flush();
                    }
                }
            }
            else
            {
                for (int y = 0; y < Settings.MapHeight; y++)
                {
                    for (int x = 0; x < Settings.MapWidth; x++)
                    {
                        canvas.DrawRect(((Settings.MapWidth + Settings.MapHeight) * 10 / 2) + (x * 5) - (y * 5), (y * 5) + (x * 5) + 5, 5, 5, GetPathingSpacePaint(y, x, rotatePathEffect));
                        canvas.Flush();
                    }
                }
            }

            return pathingImage;
        }

        public SKBitmap GetBackgroundGradient()
        {
            SKBitmap bgGradient = new(64, 64);
            SKCanvas canvas = new(bgGradient);
            SKRect rect = new(0, 0, 64, 64);

            using SKPaint paint = new();
            paint.Shader = SKShader.CreateLinearGradient(
                new SKPoint((rect.Left + rect.Right) / 2, rect.Top),
                new SKPoint((rect.Left + rect.Right) / 2, rect.Bottom),
                new SKColor[] { Settings.TopGradient, Settings.BottomGradient },
                new float[] { 0, 1 },
                SKShaderTileMode.Repeat);

            canvas.DrawRect(rect, paint);
            canvas.Flush();

            return bgGradient;
        }

        private SKPaint GetPathingSpacePaint(int x, int y, SKPathEffect rotatePathEffect)
        {
            return PathingMap[x][y] switch
            {
                1 => new() { Color = SKColors.Gray, PathEffect = rotatePathEffect }, // walkable
                2 => new() { Color = SKColors.Teal, PathEffect = rotatePathEffect }, // spawnable
                _ => new() { Color = SKColors.Black, PathEffect = rotatePathEffect }, // unwalkable
            };
        }

        public override string GetSource(Dictionary<string, IncludeEntry[]> includes)
        {
            StringBuilder sb = new();



            return sb.ToString();
        }
    }

    public class MapFileSettings
    {
        public bool YOriented { get; set; }
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
        public int Unknown34 { get; set; }
        public SKColor TopGradient { get; set; }
        public SKColor BottomGradient { get; set; }
        public int UnknownLayoutIndex40 { get; set; }
        public int Unknown44 { get; set; }
        public int Unknown48 { get; set; }
        public (int x, int y) StartingPosition { get; set; }
        public int CAnimationFileIndex { get; set; }
        public int PaletteAnimationFileIndex { get; set; }
        public int Unknown5C { get; set; }
        public int Unknown60 { get; set; }
        public int Unknown64 { get; set; }
        public int Unknown68 { get; set; }
        public int Unknown6C { get; set; }

        public MapFileSettings(IEnumerable<byte> data)
        {
            YOriented = BitConverter.ToInt32(data.Take(4).ToArray()) > 0;
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
            Unknown34 = BitConverter.ToInt32(data.Skip(0x34).Take(4).ToArray());
            TopGradient = new(data.ElementAt(0x3A), data.ElementAt(0x39), data.ElementAt(0x38));
            BottomGradient = new(data.ElementAt(0x3E), data.ElementAt(0x3D), data.ElementAt(0x3C));
            UnknownLayoutIndex40 = BitConverter.ToInt32(data.Skip(0x40).Take(4).ToArray());
            Unknown44 = BitConverter.ToInt32(data.Skip(0x44).Take(4).ToArray());
            Unknown48 = BitConverter.ToInt32(data.Skip(0x48).Take(4).ToArray());
            StartingPosition = (BitConverter.ToInt32(data.Skip(0x4C).Take(4).ToArray()), BitConverter.ToInt32(data.Skip(0x50).Take(4).ToArray()));
            CAnimationFileIndex = BitConverter.ToInt32(data.Skip(0x54).Take(4).ToArray());
            PaletteAnimationFileIndex = BitConverter.ToInt32(data.Skip(0x58).Take(4).ToArray());
            Unknown5C = BitConverter.ToInt32(data.Skip(0x5C).Take(4).ToArray());
            Unknown60 = BitConverter.ToInt32(data.Skip(0x60).Take(4).ToArray());
            Unknown60 = BitConverter.ToInt32(data.Skip(0x60).Take(4).ToArray());
            Unknown64 = BitConverter.ToInt32(data.Skip(0x64).Take(4).ToArray());
            Unknown68 = BitConverter.ToInt32(data.Skip(0x68).Take(4).ToArray());
            Unknown6C = BitConverter.ToInt32(data.Skip(0x6C).Take(4).ToArray());
        }
    }
}
