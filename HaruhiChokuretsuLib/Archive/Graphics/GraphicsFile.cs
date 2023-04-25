using HaruhiChokuretsuLib.Util;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace HaruhiChokuretsuLib.Archive.Graphics
{
    public partial class GraphicsFile : FileInArchive
    {
        public List<byte> PaletteData { get; set; }
        public List<SKColor> Palette { get; set; } = new();
        public List<byte> PixelData { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public Function FileFunction { get; set; }
        public TileForm ImageTileForm { get; set; }
        public Form ImageForm { get; set; }
        public string Determinant { get; set; }

        public static int PNG_QUALITY = 100;

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
            SCREEN,
            LAYOUT,
            ANIMATION,
        }

        public enum Form
        {
            UNKNOWN,
            TEXTURE,
            TILE,
        }

        public override void Initialize(byte[] decompressedData, int offset, ILogger log)
        {
            _log = log;
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
                    Palette.Add(new SKColor((byte)((color & 0x1F) << 3), (byte)((color >> 5 & 0x1F) << 3), (byte)((color >> 10 & 0x1F) << 3)));
                }

                while (Palette.Count < 256)
                {
                    Palette.Add(new SKColor(0, 0, 0));
                }

                PixelData = Data.Skip(paletteLength + 0x14).ToList();
            }
            else if (Name.EndsWith("BNS", StringComparison.OrdinalIgnoreCase))
            {
                FileFunction = Function.SCREEN;
                Width = 640;
                Height = 480;
                int screenDataLength = BitConverter.ToInt32(Data.Take(4).ToArray());
                for (int i = 0; i < screenDataLength; i++)
                {
                    ScreenData.Add(new() { Index = Data[2 * i + 4], Palette = (byte)(Data[2 * i + 5] >> 4), Flip = (ScreenTileFlip)(Data[2 * i + 5] & 0xF) });
                }
            }
            else if (Name.EndsWith("BNL", StringComparison.OrdinalIgnoreCase))
            {
                FileFunction = Function.LAYOUT;
                for (int i = 0x08; i <= Data.Count - 0x1C; i += 0x1C)
                {
                    LayoutEntries.Add(new(Data.Skip(i).Take(0x1C)));
                }
            }
            else if (Name.EndsWith("BNA", StringComparison.OrdinalIgnoreCase))
            {
                FileFunction = Function.ANIMATION;
                if (Name.Contains("PAN")) // if the animation type byte is valid for rotations, we assume this is a rotatey boy
                {
                    for (int i = 0x00; i <= Data.Count - 0x08; i += 0x08)
                    {
                        AnimationEntries.Add(new PaletteRotateAnimationEntry(Data.Skip(i).Take(0x08)));
                    }
                }
                else if (Name.Contains("CAN"))
                {
                    for (int i = 0x00; i <= Data.Count - 0xCC; i += 0xCC)
                    {
                        AnimationEntries.Add(new PaletteColorAnimationEntry(Data.Skip(i).Take(0xCC)));
                    }
                }
                else if (IO.ReadShort(Data, 0xE) == -1)
                {
                    AnimationX = IO.ReadShort(Data, 0x02);
                    AnimationY = IO.ReadShort(Data, 0x04);
                    for (int i = 0x10; i <= Data.Count - 0x0A; i += 0x0A)
                    {
                        if (IO.ReadShort(Data, i + 8) != 0)
                        {
                            AnimationEntries.Add(new FrameAnimationEntry(Data.Skip(i).Take(10)));
                        }
                    }
                }
            }
            else
            {
                FileFunction = Function.UNKNOWN;
            }
        }

        public override void NewFile(string filename, ILogger log)
        {
            _log = log;
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
            Name = fileComponents.Last().ToUpper();
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
            Height = PixelData.Count / 8;
        }

        // Hardcoding these until we figure out how the game knows what to do lol
        public bool IsTexture()
        {
            return ImageForm != Form.TILE
                && Index != 0x0C9
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
                && (Index < 0x50E || Index > 0x516)
                && (Index < 0x8B4 || Index > 0x8B7)
                && (Index < 0xB3E || Index > 0xB51)
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
            return $"{Index:X3} {Index:D4} 0x{Offset:X8} ({FileFunction}) - {Name}";
        }

        public SKBitmap GetImage(int width = -1, int transparentIndex = -1, int paletteOffset = 0, GraphicsFile tilesGrp = null)
        {
            if (FileFunction == Function.SCREEN)
            {
                return GetScreenImage(tilesGrp);
            }
            if (IsTexture())
            {
                return GetTexture(width, transparentIndex);
            }
            else
            {
                return GetTiles(width, transparentIndex, paletteOffset);
            }
        }

        private SKBitmap GetTiles(int width = -1, int transparentIndex = -1, int paletteOffset = 0)
        {
            SKColor originalColor = SKColors.Black;
            if (transparentIndex >= 0)
            {
                originalColor = Palette[transparentIndex];
                Palette[transparentIndex] = SKColors.Transparent;
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
                                    bitmap.SetPixel(col * 8 + xpix * 2 + xypix, row * 8 + ypix,
                                        Palette[(PixelData[pixelIndex] >> xypix * 4 & 0xF) + paletteOffset]);
                                }
                                pixelIndex++;
                            }
                        }
                        else
                        {
                            for (int xpix = 0; xpix < 8 && pixelIndex < PixelData.Count; xpix++)
                            {
                                bitmap.SetPixel(col * 8 + xpix, row * 8 + ypix,
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

            SKBitmap bmp = new(width, height);
            for (int y = 0; y < height && i < (PixelData?.Count ?? 0); y++)
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
                    canvas.DrawRect(col * 16, row * 16, 16, 16, new() { Color = Palette[16 * row + col] });
                }
            }

            return palette;
        }

        public void SetPalette(List<SKColor> palette, int transparentIndex = -1, bool suppressOutput = false)
        {
            Palette = palette;
            if (transparentIndex >= 0)
            {
                Palette.Insert(transparentIndex, SKColors.Transparent);
            }

            PaletteData = new();
            if (!suppressOutput)
            {
                _log.Log($"Using provided palette for #{Index:X3}... ");
            }

            for (int i = 0; i < Palette.Count; i++)
            {
                byte[] color = BitConverter.GetBytes((short)(Palette[i].Red / 8 | Palette[i].Green / 8 << 5 | Palette[i].Blue / 8 << 10));
                PaletteData.AddRange(color);
            }
        }

        /// <summary>
        /// Replaces the current pixel data with a bitmap image on disk
        /// </summary>
        /// <param name="bitmapFile">Path to bitmap file to import</param>
        /// <returns>Width of new bitmap image</returns>
        public int SetImage(string bitmapFile, bool setPalette = false, int transparentIndex = -1, bool newSize = false, GraphicsFile associatedTiles = null)
        {
            Edited = true;
            return SetImage(SKBitmap.Decode(bitmapFile), setPalette, transparentIndex, newSize, associatedTiles);
        }

        /// <summary>
        /// Replaces the current pixel data with a bitmap image in memory
        /// </summary>
        /// <param name="bitmap">Bitmap image in memory</param>
        /// <returns>Width of new bitmap image</returns>
        public int SetImage(SKBitmap bitmap, bool setPalette = false, int transparentIndex = -1, bool newSize = false, GraphicsFile associatedTiles = null)
        {
            PnnLABQuantizer quantizer = new();
            if (setPalette && FileFunction != Function.SCREEN)
            {
                SetPaletteFromImage(bitmap, quantizer, transparentIndex);
            }

            if (FileFunction == Function.SCREEN)
            {
                return SetScreenImage(bitmap, quantizer, associatedTiles);
            }
            else if (IsTexture())
            {
                return SetTexture(bitmap, quantizer, newSize, transparentIndex == 0 ? true : false);
            }
            else
            {
                return SetTiles(bitmap, quantizer, newSize, transparentIndex == 0 ? true : false);
            }
        }

        private void SetPaletteFromImage(SKBitmap bitmap, PnnLABQuantizer quantizer, int transparentIndex = -1)
        {
            int numColors = Palette.Count;
            if (transparentIndex >= 0)
            {
                numColors--;
            }

            SKColor[] newPalette = Palette.ToArray();
            quantizer.Pnnquan(bitmap.Pixels.Select(c => (uint)c).ToArray(), ref newPalette, ref numColors, _log);
            Palette = newPalette.ToList();

            if (transparentIndex >= 0)
            {
                Palette.Insert(transparentIndex, SKColors.Transparent);
            }

            PaletteData = new();
            _log.Log($"Generating new palette for #{Index:X3}... ");

            for (int i = 0; i < Palette.Count; i++)
            {
                byte[] color = BitConverter.GetBytes((short)(Palette[i].Red / 8 | Palette[i].Green / 8 << 5 | Palette[i].Blue / 8 << 10));
                PaletteData.AddRange(color);
            }
        }

        private int SetTexture(SKBitmap bitmap, PnnQuantizer quantizer, bool newSize, bool firstTransparent)
        {
            if (!VALID_WIDTHS.Contains(bitmap.Width))
            {
                throw new ArgumentException($"Image width {bitmap.Width} not a valid width.");
            }
            int calculatedHeight = PixelData.Count / bitmap.Width;
            if (newSize)
            {
                PixelData = new(new byte[bitmap.Width * bitmap.Height]);
            }
            else if (bitmap.Height != calculatedHeight)
            {
                throw new ArgumentException($"Image height {bitmap.Height} does not match calculated height {calculatedHeight}.");
            }

            quantizer.QuantizeImage(bitmap, this, 256, true, true, _log);

            return bitmap.Width;
        }

        private int SetTiles(SKBitmap bitmap, PnnQuantizer quantizer, bool newSize, bool firstTransparent)
        {
            if (!VALID_WIDTHS.Contains(bitmap.Width))
            {
                throw new ArgumentException($"Image width {bitmap.Width} not a valid width.");
            }
            int calculatedHeight = PixelData.Count / (bitmap.Width / (ImageTileForm == TileForm.GBA_4BPP ? 2 : 1));
            if (newSize)
            {
                _log.LogWarning("Resizing... ");
                PixelData = new(new byte[bitmap.Width * bitmap.Height]);
            }
            else if (bitmap.Height != calculatedHeight)
            {
                throw new ArgumentException($"Image height {bitmap.Height} does not match calculated height {calculatedHeight}.");
            }

            quantizer.QuantizeImage(bitmap, this, ImageTileForm == TileForm.GBA_4BPP ? 16 : 256, true, false, _log);
            return bitmap.Width;
        }
    }
}
