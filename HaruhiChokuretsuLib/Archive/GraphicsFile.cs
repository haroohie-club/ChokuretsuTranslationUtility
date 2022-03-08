using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace HaruhiChokuretsuLib.Archive
{
    public class GraphicsFile : FileInArchive
    {
        public List<byte> PaletteData { get; set; }
        public List<SKColor> Palette { get; set; } = new();
        public List<byte> PixelData { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public List<LayoutEntry> LayoutEntries { get; set; } = new();
        public Function FileFunction { get; set; }
        public TileForm ImageTileForm { get; set; }
        public Form ImageForm { get; set; }
        public string Determinant { get; set; }

        public static int PNG_QUALITY = 300;

        private readonly static int[] VALID_WIDTHS = new int[] { 8, 16, 32, 64, 128, 256, 512, 1024 };

        public enum TileForm
        {
            // corresponds to number of colors
            GBA_4BPP = 0x10,
            GBA_8BPP = 0x100,
        }

        public enum Function
        {
            UNKNOWN,
            SHTX,
            LAYOUT,
        }

        public enum Form
        {
            UNKNOWN,
            TEXTURE,
            TILE,
        }

        public override void Initialize(byte[] decompressedData, int offset)
        {
            Offset = offset;
            Data = decompressedData.ToList();
            byte[] magicBytes = Data.Take(4).ToArray();
            if (Encoding.ASCII.GetString(magicBytes) == "SHTX")
            {
                FileFunction = Function.SHTX;
                Determinant = Encoding.ASCII.GetString(Data.Skip(4).Take(2).ToArray());
                ImageTileForm = (TileForm)BitConverter.ToInt16(decompressedData.Skip(0x06).Take(2).ToArray());
                Width = (int)Math.Pow(2, Data.Skip(0x0E).First());
                Height = (int)Math.Pow(2, Data.Skip(0x0F).First());

                int paletteLength = 0x200;
                if (ImageTileForm == TileForm.GBA_4BPP)
                {
                    paletteLength = 0x60;
                }

                PaletteData = Data.Skip(0x14).Take(paletteLength).ToList();
                for (int i = 0; i < PaletteData.Count; i += 2)
                {
                    short color = BitConverter.ToInt16(PaletteData.Skip(i).Take(2).ToArray());
                    Palette.Add(new SKColor((byte)((color & 0x1F) << 3), (byte)(((color >> 5) & 0x1F) << 3), (byte)(((color >> 10) & 0x1F) << 3)));
                }

                while (Palette.Count < 256)
                {
                    Palette.Add(new SKColor(0, 0, 0));
                }

                PixelData = Data.Skip(paletteLength + 0x14).ToList();
            }
            else if (magicBytes.SequenceEqual(new byte[] { 0x00, 0x01, 0xC0, 0x00 }) || magicBytes.SequenceEqual(new byte[] { 0x10, 0x04, 0x88, 0x02 }))
            {
                FileFunction = Function.LAYOUT;
                for (int i = 0x08; i < Data.Count - 0x1C; i += 0x1C)
                {
                    LayoutEntries.Add(new(Data.Skip(i).Take(0x1C)));
                }
            }
            else
            {
                FileFunction = Function.UNKNOWN;
            }
        }

        public override void NewFile(string filename)
        {
            SKBitmap bitmap = SKBitmap.Decode(filename);
            string[] fileComponents = Path.GetFileNameWithoutExtension(filename).Split('_');
            ImageTileForm = fileComponents[1].ToLower() switch
            {
                "4bpp" => TileForm.GBA_4BPP,
                "8bpp" => TileForm.GBA_8BPP,
                _ => throw new ArgumentException($"Image {filename} does not have its tile form (second argument should be '4BPP' or '8BPP')")
            };
            ImageForm = fileComponents[2].ToLower() switch
            {
                "texture" => Form.TEXTURE,
                "tile" => Form.TILE,
                _ => throw new ArgumentException($"Image {filename} does not have its image form (third argument should be 'texture' or 'tile')")
            };
            Data = new();
            FileFunction = Function.SHTX;
            int transparentIndex = -1;
            Match transparentIndexMatch = Regex.Match(filename, @"tidx(?<transparentIndex>\d+)");
            if (transparentIndexMatch.Success)
            {
                transparentIndex = int.Parse(transparentIndexMatch.Groups["transparentIndex"].Value);
            }

            PaletteData = new();
            PixelData = new();
            if (ImageTileForm == TileForm.GBA_4BPP)
            {
                Palette.AddRange(new SKColor[16]);
                PaletteData.AddRange(new byte[0x60]);
                PixelData.AddRange(new byte[bitmap.Width * bitmap.Height / 2]);
            }
            else
            {
                Palette.AddRange(new SKColor[256]);
                PaletteData.AddRange(new byte[512]);
                PixelData.AddRange(new byte[bitmap.Width * bitmap.Height]);
            }
            Data.AddRange(Encoding.ASCII.GetBytes("SHTXDS"));
            Data.AddRange(BitConverter.GetBytes((short)ImageTileForm));
            byte encodedWidth = (byte)Math.Log2(bitmap.Width);
            byte encodedHeight = (byte)Math.Log2(bitmap.Height);
            Data.AddRange(new byte[] { 0x01, 0x00, 0x00, 0x01, 0xC0, 0x00, encodedWidth, encodedHeight, 0x00, 0xC0, 0x00, 0x00 });
            Data.AddRange(PaletteData);
            Data.AddRange(PixelData);

            SetImage(bitmap, setPalette: true, transparentIndex: transparentIndex);
        }

        public void InitializeFontFile()
        {
            ImageTileForm = TileForm.GBA_4BPP;
            // grayscale palette
            Palette = new()
            {
                new SKColor(0x00, 0x00, 0x00),
                new SKColor(0x0F, 0x0F, 0x0F),
                new SKColor(0x2F, 0x2F, 0x2F),
                new SKColor(0x3F, 0x3F, 0x3F),
                new SKColor(0x4F, 0x4F, 0x4F),
                new SKColor(0x4F, 0x4F, 0x4F),
                new SKColor(0x5F, 0x5F, 0x5F),
                new SKColor(0x6F, 0x6F, 0x6F),
                new SKColor(0x7F, 0x7F, 0x7F),
                new SKColor(0x8F, 0x8F, 0x8F),
                new SKColor(0x9F, 0x9F, 0x9F),
                new SKColor(0xAF, 0xAF, 0xAF),
                new SKColor(0xBF, 0xBF, 0xBF),
                new SKColor(0xCF, 0xCF, 0xCF),
                new SKColor(0xDF, 0xDF, 0xDF),
                new SKColor(0xEF, 0xEF, 0xEF),
                new SKColor(0xFF, 0xFF, 0xFF),
            };
            PixelData = Data;
            Width = 16;
            Height = PixelData.Count / 32;
        }

        // Hardcoding these until we figure out how the game knows what to do lol
        public bool IsTexture()
        {
            return ImageForm != Form.TILE
                && (Index < 0x19E || Index > 0x1A7)
                && Index != 0x2C0
                && (Index < 0x2C4 || Index > 0x2C6)
                && Index != 0x2C8 && Index != 0x2CA
                && Index != 0x2CC && Index != 0x316
                && Index != 0x318 && Index != 0x331
                && Index != 0x370 && Index != 0x3A7
                && Index != 0x3A9 && Index != 0x3AB
                && Index != 0x3AF && Index != 0x3FE
                && (Index < 0x41B || Index > 0x42C)
                && (Index < 0x8B4 || Index > 0x8B7)
                && (Index < 0xB61 || Index > 0xB6F)
                && (Index < 0xBC9 || Index > 0xC1B)
                && (Index < 0xC70 || Index > 0xC78)
                && (Index < 0xCA3 || Index > 0xCA8)
                && Index != 0xCAF
                && (Index < 0xD02 || Index > 0xD9F)
                && Index != 0xDF3
                && (Index < 0xDFB || Index > 0xE08)
                && (Index < 0xE0E || Index > 0xE10)
                && (Index < 0xE17 || Index > 0xE25)
                && (Index < 0xE2A || Index > 0xE41)
                && Index != 0xE50;
        }

        public override byte[] GetBytes()
        {
            if (FileFunction == Function.SHTX)
            {
                List<byte> data = new();
                data.AddRange(Data.Take(0x14)); // get header
                data.AddRange(PaletteData);
                data.AddRange(PixelData);

                return data.ToArray();
            }
            else if (FileFunction == Function.LAYOUT)
            {
                List<byte> data = new();
                data.AddRange(Data.Take(0x08)); // get header
                foreach (LayoutEntry entry in LayoutEntries)
                {
                    data.AddRange(entry.GetBytes());
                }

                return data.ToArray();
            }
            else if (Index == 0xE50) // more special casing for the font file
            {
                return PixelData.ToArray();
            }
            else
            {
                return Data.ToArray();
            }
        }

        public override string ToString()
        {
            return $"{Index:X3} {Index:D4} 0x{Offset:X8} ({FileFunction})";
        }

        public SKBitmap GetImage(int width = -1, int transparentIndex = -1)
        {
            if (IsTexture())
            {
                return GetTexture(width, transparentIndex);
            }
            else
            {
                return GetTiles(width, transparentIndex);
            }
        }

        private SKBitmap GetTiles(int width = -1, int transparentIndex = -1)
        {

            SKColor originalColor = SKColors.Black;
            if (transparentIndex >= 0)
            {
                originalColor = Palette[transparentIndex];
                Palette[transparentIndex] = SKColors.White;
            }
            int height;
            if (width == -1)
            {
                width = Width;
                height = Height;
            }
            else
            {
                if (!VALID_WIDTHS.Contains(width))
                {
                    width = 256;
                }
                height = PixelData.Count / (width / (ImageTileForm == TileForm.GBA_4BPP ? 2 : 1));
            }
            SKBitmap bitmap = new(width, height);
            int pixelIndex = 0;
            for (int row = 0; row < height / 8 && pixelIndex < PixelData.Count; row++)
            {
                for (int col = 0; col < width / 8 && pixelIndex < PixelData.Count; col++)
                {
                    for (int ypix = 0; ypix < 8 && pixelIndex < PixelData.Count; ypix++)
                    {
                        if (ImageTileForm == TileForm.GBA_4BPP)
                        {
                            for (int xpix = 0; xpix < 4 && pixelIndex < PixelData.Count; xpix++)
                            {
                                for (int xypix = 0; xypix < 2 && pixelIndex < PixelData.Count; xypix++)
                                {
                                    bitmap.SetPixel((col * 8) + (xpix * 2) + xypix, (row * 8) + ypix,
                                        Palette[(PixelData[pixelIndex] >> (xypix * 4)) & 0xF]);
                                }
                                pixelIndex++;
                            }
                        }
                        else
                        {
                            for (int xpix = 0; xpix < 8 && pixelIndex < PixelData.Count; xpix++)
                            {
                                bitmap.SetPixel((col * 8) + xpix, (row * 8) + ypix,
                                    Palette[PixelData[pixelIndex++]]);
                            }
                        }
                    }
                }
            }
            if (transparentIndex >= 0)
            {
                Palette[transparentIndex] = originalColor;
            }
            return bitmap;
        }

        private SKBitmap GetTexture(int width = -1, int transparentIndex = -1)
        {
            int height;
            if (width == -1)
            {
                width = Width;
                height = Height;
            }
            else
            {
                if (!VALID_WIDTHS.Contains(width))
                {
                    width = 256;
                }
                height = PixelData.Count / (width / (ImageTileForm == TileForm.GBA_4BPP ? 2 : 1));
            }
            int i = 0;

            SKBitmap bmp = new SKBitmap(width, height);
            for (int y = 0; y < height && i < PixelData.Count; y++)
            {
                for (int x = 0; x < width && i < PixelData.Count; x++)
                {
                    SKColor color;
                    if (PixelData[i] == transparentIndex)
                    {
                        color = SKColors.Transparent;
                        i++;
                    }
                    else
                    {
                        color = Palette[PixelData[i++]];
                    }
                    bmp.SetPixel(x, y, color);
                }
            }

            return bmp;
        }

        public SKBitmap GetPalette()
        {
            SKBitmap palette = new(256, Palette.Count);
            using SKCanvas canvas = new(palette);

            for (int row = 0; row < palette.Height / 16; row++)
            {
                for (int col = 0; col < palette.Width / 16; col++)
                {
                    canvas.DrawRect(col * 16, row * 16, 16, 16, new() { Color = Palette[16 * row + col]});
                }
            }

            return palette;
        }

        public void SetPalette(List<SKColor> palette, int transparentIndex = -1)
        {
            Palette = palette;
            if (transparentIndex >= 0)
            {
                Palette.Insert(transparentIndex, SKColors.Transparent);
            }

            PaletteData = new();
            Console.Write($"Using provided palette for #{Index:X3}... ");

            for (int i = 0; i < Palette.Count; i++)
            {
                byte[] color = BitConverter.GetBytes((short)((Palette[i].Red / 8) | ((Palette[i].Green / 8) << 5) | ((Palette[i].Blue / 8) << 10)));
                PaletteData.AddRange(color);
            }
        }

        /// <summary>
        /// Replaces the current pixel data with a bitmap image on disk
        /// </summary>
        /// <param name="bitmapFile">Path to bitmap file to import</param>
        /// <returns>Width of new bitmap image</returns>
        public int SetImage(string bitmapFile, bool setPalette = false, int transparentIndex = -1)
        {
            Edited = true;
            return SetImage(SKBitmap.Decode(bitmapFile), setPalette, transparentIndex);
        }

        /// <summary>
        /// Replaces the current pixel data with a bitmap image in memory
        /// </summary>
        /// <param name="bitmap">Bitmap image in memory</param>
        /// <returns>Width of new bitmap image</returns>
        public int SetImage(SKBitmap bitmap, bool setPalette = false, int transparentIndex = -1)
        {
            if (setPalette)
            {
                SetPaletteFromImage(bitmap, transparentIndex);
            }

            if (IsTexture())
            {
                return SetTexture(bitmap);
            }
            else
            {
                return SetTiles(bitmap);
            }
        }

        private void SetPaletteFromImage(SKBitmap bitmap, int transparentIndex = -1)
        {
            int numColors = Palette.Count;
            if (transparentIndex >= 0)
            {
                numColors--;
            }
            Palette = Helpers.GetPaletteFromImage(bitmap, numColors);
            for (int i = Palette.Count; i < numColors; i++)
            {
                Palette.Add(SKColors.Black);
            }
            if (transparentIndex >= 0)
            {
                Palette.Insert(transparentIndex, SKColors.Transparent);
            }

            PaletteData = new();
            Console.Write($"Generating new palette for #{Index:X3}... ");

            for (int i = 0; i < Palette.Count; i++)
            {
                byte[] color = BitConverter.GetBytes((short)((Palette[i].Red / 8) | ((Palette[i].Green / 8) << 5) | ((Palette[i].Blue / 8) << 10)));
                PaletteData.AddRange(color);
            }
        }

        private int SetTexture(SKBitmap bitmap)
        {
            if (!VALID_WIDTHS.Contains(bitmap.Width))
            {
                throw new ArgumentException($"Image width {bitmap.Width} not a valid width.");
            }
            int calculatedHeight = PixelData.Count / bitmap.Width;
            if (bitmap.Height != calculatedHeight)
            {
                throw new ArgumentException($"Image height {bitmap.Height} does not match calculated height {calculatedHeight}.");
            }

            int i = 0;
            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    PixelData[i++] = (byte)Helpers.ClosestColorIndex(Palette, bitmap.GetPixel(x, y));
                }
            }

            return bitmap.Width;
        }

        private int SetTiles(SKBitmap bitmap)
        {
            if (!VALID_WIDTHS.Contains(bitmap.Width))
            {
                throw new ArgumentException($"Image width {bitmap.Width} not a valid width.");
            }
            int calculatedHeight = PixelData.Count / (bitmap.Width / (ImageTileForm == TileForm.GBA_4BPP ? 2 : 1));
            if (bitmap.Height != calculatedHeight)
            {
                throw new ArgumentException($"Image height {bitmap.Height} does not match calculated height {calculatedHeight}.");
            }

            List<byte> pixelData = new();

            for (int row = 0; row < bitmap.Height / 8 && pixelData.Count < PixelData.Count; row++)
            {
                for (int col = 0; col < bitmap.Width / 8 && pixelData.Count < PixelData.Count; col++)
                {
                    for (int ypix = 0; ypix < 8 && pixelData.Count < PixelData.Count; ypix++)
                    {
                        if (ImageTileForm == TileForm.GBA_4BPP)
                        {
                            for (int xpix = 0; xpix < 4 && pixelData.Count < PixelData.Count; xpix++)
                            {
                                int color1 = Helpers.ClosestColorIndex(Palette, bitmap.GetPixel((col * 8) + (xpix * 2), (row * 8) + ypix));
                                int color2 = Helpers.ClosestColorIndex(Palette, bitmap.GetPixel((col * 8) + (xpix * 2) + 1, (row * 8) + ypix));

                                pixelData.Add((byte)(color1 + (color2 << 4)));
                            }
                        }
                        else
                        {
                            for (int xpix = 0; xpix < 8 && pixelData.Count < PixelData.Count; xpix++)
                            {
                                pixelData.Add((byte)Helpers.ClosestColorIndex(Palette, bitmap.GetPixel((col * 8) + xpix, (row * 8) + ypix)));
                            }
                        }
                    }
                }
            }
            PixelData = pixelData;
            return bitmap.Width;
        }

        public (SKBitmap bitmap, List<LayoutEntry> layoutEntries) GetLayout(List<GraphicsFile> grpFiles, int entryIndex, int numEntries, bool darkMode)
        {
            return GetLayout(grpFiles, LayoutEntries.Skip(entryIndex).Take(numEntries).ToList(), darkMode);
        }

        public (SKBitmap bitmap, List<LayoutEntry> layouts) GetLayout(List<GraphicsFile> grpFiles, List<LayoutEntry> layoutEntries, bool darkMode)
        {
            SKBitmap layoutBitmap = new(640, 480);
            using SKCanvas canvas = new(layoutBitmap);
            if (darkMode)
            {
                canvas.DrawRect(0, 0, layoutBitmap.Width, layoutBitmap.Height, new() { Color = SKColors.Black });
            }
            foreach (LayoutEntry currentEntry in layoutEntries)
            {
                if (currentEntry.RelativeShtxIndex < 0)
                {
                    continue;
                }
                int grpIndex = Index + 1;
                for (int i = 0; i <= currentEntry.RelativeShtxIndex && grpIndex < grpFiles.Count; grpIndex++)
                {
                    if (grpFiles.First(g => g.Index == grpIndex).FileFunction == Function.SHTX)
                    {
                        i++;
                    }
                }
                GraphicsFile grpFile = grpFiles.First(g => g.Index == grpIndex - 1);

                SKRect boundingBox = new()
                {
                    Left = currentEntry.TextureX,
                    Top = currentEntry.TextureY,
                    Right = currentEntry.TextureX + currentEntry.TextureW,
                    Bottom = currentEntry.TextureY + currentEntry.TextureH,
                };
                SKRect destination = new()
                {
                    Left = currentEntry.ScreenX,
                    Top = currentEntry.ScreenY,
                    Right = currentEntry.ScreenX + currentEntry.ScreenW,
                    Bottom = currentEntry.ScreenY + currentEntry.ScreenH,
                };

                SKBitmap texture = grpFile.GetImage(transparentIndex: 0);
                SKBitmap tile = new((int)Math.Abs(boundingBox.Right - boundingBox.Left), (int)Math.Abs(boundingBox.Bottom - boundingBox.Top));
                SKCanvas transformCanvas = new(tile);

                if (currentEntry.ScreenW < 0)
                {
                    transformCanvas.Scale(-1, 1, tile.Width / 2.0f, 0);
                }
                if (currentEntry.ScreenH < 0)
                {
                    transformCanvas.Scale(1, -1, 0, tile.Height / 2.0f);
                }
                transformCanvas.DrawBitmap(texture, boundingBox, new SKRect(0, 0, tile.Width, tile.Height));

                for (int x = 0; x < tile.Width; x++)
                {
                    for (int y = 0; y < tile.Height; y++)
                    {
                        if (currentEntry.Tint.Red != 0xFF)
                        {
                            Console.WriteLine("Here");
                        }
                        SKColor pixelColor = tile.GetPixel(x, y);
                        tile.SetPixel(x, y, new((byte)(pixelColor.Red * currentEntry.Tint.Red / 255),
                            (byte)(pixelColor.Green * currentEntry.Tint.Green / 255),
                            (byte)(pixelColor.Blue * currentEntry.Tint.Blue / 255),
                            (byte)(pixelColor.Alpha * currentEntry.Tint.Alpha / 255)));
                    }
                }

                canvas.DrawBitmap(tile, destination);
            }

            return (layoutBitmap, layoutEntries);
        }
    }

    public class LayoutEntry
    {
        public short UnknownShort1 { get; set; }
        public short RelativeShtxIndex { get; set; }
        public short UnknownShort2 { get; set; }
        public short ScreenX { get; set; }
        public short ScreenY { get; set; }
        public short TextureW { get; set; }
        public short TextureH { get; set; }
        public short TextureX { get; set; }
        public short TextureY { get; set; }
        public short ScreenW { get; set; }
        public short ScreenH { get; set; }
        public short UnknownShort3 { get; set; }
        public SKColor Tint { get; set; }

        public LayoutEntry(IEnumerable<byte> data)
        {
            if (data.Count() != 0x1C)
            {
                throw new ArgumentException($"Layout entry must be of width 0x1C (was {data.Count()}");
            }

            UnknownShort1 = BitConverter.ToInt16(data.Take(2).ToArray());
            RelativeShtxIndex = BitConverter.ToInt16(data.Skip(0x02).Take(2).ToArray());
            UnknownShort2 = BitConverter.ToInt16(data.Skip(0x04).Take(2).ToArray());
            ScreenX = BitConverter.ToInt16(data.Skip(0x06).Take(2).ToArray());
            ScreenY = BitConverter.ToInt16(data.Skip(0x08).Take(2).ToArray());
            TextureW = BitConverter.ToInt16(data.Skip(0x0A).Take(2).ToArray());
            TextureH = BitConverter.ToInt16(data.Skip(0x0C).Take(2).ToArray());
            TextureX = BitConverter.ToInt16(data.Skip(0x0E).Take(2).ToArray());
            TextureY = BitConverter.ToInt16(data.Skip(0x10).Take(2).ToArray());
            ScreenW = BitConverter.ToInt16(data.Skip(0x12).Take(2).ToArray());
            ScreenH = BitConverter.ToInt16(data.Skip(0x14).Take(2).ToArray());
            UnknownShort3 = BitConverter.ToInt16(data.Skip(0x16).Take(2).ToArray());
            Tint = new((uint)BitConverter.ToInt32(data.Skip(0x18).Take(4).ToArray()));
        }

        public byte[] GetBytes()
        {
            List<byte> data = new();
            data.AddRange(BitConverter.GetBytes(UnknownShort1));
            data.AddRange(BitConverter.GetBytes(RelativeShtxIndex));
            data.AddRange(BitConverter.GetBytes(UnknownShort2));
            data.AddRange(BitConverter.GetBytes(ScreenX));
            data.AddRange(BitConverter.GetBytes(ScreenY));
            data.AddRange(BitConverter.GetBytes(TextureW));
            data.AddRange(BitConverter.GetBytes(TextureH));
            data.AddRange(BitConverter.GetBytes(TextureX));
            data.AddRange(BitConverter.GetBytes(TextureY));
            data.AddRange(BitConverter.GetBytes(ScreenW));
            data.AddRange(BitConverter.GetBytes(ScreenH));
            data.AddRange(BitConverter.GetBytes(UnknownShort3));
            data.AddRange(BitConverter.GetBytes((uint)Tint));
            return data.ToArray();
        }

        public override string ToString()
        {
            return $"Index: {RelativeShtxIndex}; TX: {TextureX} {TextureY} {TextureX + TextureW} {TextureY + TextureH}, SC: {ScreenX} {ScreenY} {ScreenX + ScreenW} {ScreenY + ScreenH}";
        }
    }
}
