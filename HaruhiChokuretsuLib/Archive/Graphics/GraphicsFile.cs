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
    /// <summary>
    /// Represents all files in grp.bin
    /// </summary>
    public partial class GraphicsFile : FileInArchive
    {
        /// <summary>
        /// Palette data for texture files
        /// </summary>
        public List<byte> PaletteData { get; set; }
        /// <summary>
        /// SKColor representation of the palette for texture files
        /// </summary>
        public List<SKColor> Palette { get; set; } = [];
        /// <summary>
        /// Raw pixel data for textures
        /// </summary>
        public List<byte> PixelData { get; set; }
        /// <summary>
        /// Unknown
        /// </summary>
        public short Unknown08 { get; set; }
        /// <summary>
        /// The width of the tiles that make up the graphic
        /// </summary>
        public short TileWidth { get; set; }
        /// <summary>
        /// The height of the tiles that make up the graphic
        /// </summary>
        public short TileHeight { get; set; }
        /// <summary>
        /// Texture file width
        /// </summary>
        public int Width { get; set; }
        /// <summary>
        /// Texture file height
        /// </summary>
        public int Height { get; set; }
        /// <summary>
        /// Unknown
        /// </summary>
        public short Unknown12 { get; set; }
        /// <summary>
        /// The function of a particular graphics file
        /// </summary>
        public Function FileFunction { get; set; }
        /// <summary>
        /// The tile form (4bpp or 8bpp) of a texture file
        /// </summary>
        public TileForm ImageTileForm { get; set; }
        /// <summary>
        /// Determines which screen the texture is optimized for
        /// </summary>
        public Form ImageForm { get; set; }
        /// <summary>
        /// Initial bytes used in determining the file type
        /// </summary>
        public string Determinant { get; set; }

        /// <summary>
        /// The quality level of exported PNGs
        /// </summary>
        public const int PNG_QUALITY = 100;

        /// <summary>
        /// The valid widths of an image
        /// </summary>
        private readonly static int[] VALID_WIDTHS = [8, 16, 32, 64, 128, 256, 512, 1024];

        /// <summary>
        /// An enum representing the number of colors in a texture
        /// </summary>
        public enum TileForm
        {
            // corresponds to number of colors
            /// <summary>
            /// 4 bits per pixel i.e. 16 colors
            /// </summary>
            GBA_4BPP = 0x10,
            /// <summary>
            /// 8 bits per pixel i.e. 256 colors
            /// </summary>
            GBA_8BPP = 0x100,
        }

        /// <summary>
        /// An enum representing the function of a graphics file
        /// </summary>
        public enum Function
        {
            /// <summary>
            /// Unknown function
            /// </summary>
            UNKNOWN,
            /// <summary>
            /// Shade Texture graphic
            /// </summary>
            SHTX,
            /// <summary>
            /// Top screen VRAM-optimized graphic
            /// </summary>
            SCREEN,
            /// <summary>
            /// Layout file
            /// </summary>
            LAYOUT,
            /// <summary>
            /// Animation file
            /// </summary>
            ANIMATION,
        }

        /// <summary>
        /// An enum representing which screen the texture is optimized for (TILE = top, TEXTURE = bottom)
        /// </summary>
        public enum Form
        {
            /// <summary>
            /// Unknown to be tile or texture
            /// </summary>
            UNKNOWN,
            /// <summary>
            /// Texture (bottom screen optimized)
            /// </summary>
            TEXTURE,
            /// <summary>
            /// Tile (top screen OAM optimized)
            /// </summary>
            TILE,
        }

        /// <inheritdoc/>
        public override void Initialize(byte[] decompressedData, int offset, ILogger log)
        {
            Log = log;
            Offset = offset;
            Data = [.. decompressedData];
            byte[] magicBytes = Data.Take(4).ToArray();
            if (Encoding.ASCII.GetString(magicBytes) == "SHTX")
            {
                FileFunction = Function.SHTX;
                Determinant = Encoding.ASCII.GetString(Data.Skip(4).Take(2).ToArray());
                ImageTileForm = (TileForm)BitConverter.ToInt16(decompressedData.Skip(0x06).Take(2).ToArray());
                Unknown08 = IO.ReadShort(decompressedData, 0x08);
                TileWidth = IO.ReadShort(decompressedData, 0x0A);
                TileHeight = IO.ReadShort(decompressedData, 0x0C);
                Width = (int)Math.Pow(2, Data.ElementAt(0x0E));
                Height = (int)Math.Pow(2, Data.ElementAt(0x0F));
                Unknown12 = IO.ReadShort(decompressedData, 0x12);
                int paletteLength = 0x200;
                if (ImageTileForm == TileForm.GBA_4BPP && !Name.StartsWith("CHS_SYS_"))
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
                if (Name.Contains("PAN"))
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
                    ChibiAnimationType = IO.ReadShort(Data, 0x08);
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

        /// <inheritdoc/>
        public override void NewFile(string filename, ILogger log)
        {
            Log = log;
            SKBitmap bitmap = SKBitmap.Decode(filename);
            string[] fileComponents = Path.GetFileNameWithoutExtension(filename).Split('_');
            Determinant = "DS";
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
            Unknown08 = short.Parse(fileComponents[3]);
            TileWidth = short.Parse(fileComponents[4]);
            TileHeight = short.Parse(fileComponents[5]);
            Unknown12 = 0;
            Name = fileComponents.Last().ToUpper();
            Data = [];
            FileFunction = Function.SHTX;
            int transparentIndex = -1;
            Match transparentIndexMatch = Regex.Match(filename, @"tidx(?<transparentIndex>\d+)");
            if (transparentIndexMatch.Success)
            {
                transparentIndex = int.Parse(transparentIndexMatch.Groups["transparentIndex"].Value);
            }

            PaletteData = [];
            PixelData = [];
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
            byte encodedWidth, encodedHeight;
            if (ImageForm == Form.TILE)
            {
                encodedWidth = (byte)Math.Log2(256);
                encodedHeight = (byte)Math.Ceiling(Math.Log2(bitmap.Height / (256 / bitmap.Width)));
            }
            else
            {
                encodedWidth = (byte)Math.Ceiling(Math.Log2(bitmap.Width));
                encodedHeight = (byte)Math.Ceiling(Math.Log2(bitmap.Height));
            }
            Data.AddRange([0x01, 0x00, 0x00, 0x01, 0xC0, 0x00, encodedWidth, encodedHeight, 0x00, 0xC0, 0x00, 0x00]);
            Data.AddRange(PaletteData);
            Data.AddRange(PixelData);

            SetImage(bitmap, setPalette: true, transparentIndex: transparentIndex);
        }

        /// <summary>
        /// Initializes ZENFONT.DNX (special font file)
        /// </summary>
        public void InitializeFontFile()
        {
            ImageTileForm = TileForm.GBA_4BPP;
            // grayscale palette
            Palette =
            [
                new SKColor(0x00, 0x00, 0x00),
                new SKColor(0x1F, 0x1F, 0xF),
                new SKColor(0x2F, 0x2F, 0x2F),
                new SKColor(0x3F, 0x3F, 0x3F),
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
            ];
            PixelData = Data;
            Width = 16;
            Height = PixelData.Count / 8;
        }

        /// <summary>
        /// Hardocded method determining whether a file is top screen or bottom screen optimized
        /// </summary>
        /// <returns>True if optimized for bottom screen; false otherwise</returns>
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
                && (Index < 0xC7B || Index > 0xC7C)
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

        /// <inheritdoc/>
        public override byte[] GetBytes()
        {
            if (FileFunction == Function.SHTX)
            {
                List<byte> data =
                [
                    .. Encoding.ASCII.GetBytes($"SHTX{Determinant}"),
                    .. BitConverter.GetBytes((short)ImageTileForm),
                    .. BitConverter.GetBytes(Unknown08),
                    .. BitConverter.GetBytes(TileWidth),
                    .. BitConverter.GetBytes(TileHeight),
                    (byte)Math.Log2(Width),
                    (byte)Math.Log2(Height),
                    .. BitConverter.GetBytes((ushort)(Width * Height)),
                    .. BitConverter.GetBytes(Unknown12),
                    .. PaletteData,
                    .. PixelData,
                ];

                return [.. data];
            }
            else if (FileFunction == Function.LAYOUT)
            {
                List<byte> data =
                [
                    .. Data.Take(0x08), // get header
                ];
                foreach (LayoutEntry entry in LayoutEntries)
                {
                    data.AddRange(entry.GetBytes());
                }

                return [.. data];
            }
            else if (FileFunction == Function.ANIMATION && AnimationEntries.FirstOrDefault().GetType() == typeof(FrameAnimationEntry))
            {
                List<byte> data =
                [
                    .. BitConverter.GetBytes((short)0x10),
                    .. BitConverter.GetBytes(AnimationX),
                    .. BitConverter.GetBytes(AnimationY),
                    .. new byte[] { 0x00, 0x00 },
                    .. BitConverter.GetBytes(ChibiAnimationType),
                    .. new byte[] { 0x00, 0xFF, 0x00, 0x00, 0xFF, 0xFF },
                ];
                foreach (FrameAnimationEntry frame in AnimationEntries.Cast<FrameAnimationEntry>())
                {
                    data.AddRange(frame.GetBytes());
                }
                data.AddRange(new byte[10]);

                return [.. data];
            }
            else if (Index == 0xE50) // more special casing for the font file
            {
                return [.. PixelData];
            }
            else
            {
                return [.. Data];
            }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{Index:X3} {Index:D4} 0x{Offset:X8} ({FileFunction}) - {Name}";
        }

        /// <summary>
        /// Renders a bitmap image of the texture or screen image
        /// </summary>
        /// <param name="width">(Optional) Specifies a width other than the default width</param>
        /// <param name="transparentIndex">(Optional) Specifies a transparent index for the palette (should be 0 if specified)</param>
        /// <param name="paletteOffset">(Optional) specifies a palette offset</param>
        /// <param name="tilesGrp">(Optional) Specifies a tiles graphics file for screen images</param>
        /// <returns>A rendered SKBitmap of the texture/screen graphic</returns>
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

        /// <summary>
        /// Returns a bitmap representation of the graphics file palette
        /// </summary>
        /// <returns>A bitmap with every color in the palette placed in order</returns>
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

        /// <summary>
        /// Sets the palette for the graphics file
        /// </summary>
        /// <param name="palette">The palette to replace the current one with</param>
        /// <param name="transparentIndex">The transparent index of the palette</param>
        /// <param name="suppressOutput">If true, doesn't log when replacing palette</param>
        public void SetPalette(List<SKColor> palette, int transparentIndex = -1, bool suppressOutput = false)
        {
            Palette = palette;
            if (transparentIndex >= 0)
            {
                Palette.Insert(transparentIndex, SKColors.Transparent);
            }

            PaletteData = [];
            if (!suppressOutput)
            {
                Log.Log($"Using provided palette for #{Index:X3}... ");
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
        /// <param name="setPalette">(Optional) If true, sets the palette of the graphics file from the provided image</param>
        /// <param name="transparentIndex">(Optional) Sets the transparent index (if existing, usually 0)</param>
        /// <param name="newSize">(Optional) If true, resizes the image</param>
        /// <param name="associatedTiles">(Optional) If attempting to render a screen image, has these associated tiles</param>
        /// <returns>Width of new bitmap image</returns>
        public int SetImage(string bitmapFile, bool setPalette = false, int transparentIndex = -1, bool newSize = false, GraphicsFile associatedTiles = null)
        {
            return SetImage(SKBitmap.Decode(bitmapFile), setPalette, transparentIndex, newSize, associatedTiles);
        }

        /// <summary>
        /// Replaces the current pixel data with a bitmap image in memory
        /// </summary>
        /// <param name="bitmap">Bitmap image in memory</param>
        /// <param name="setPalette">(Optional) If true, sets the palette of the graphics file from the provided image</param>
        /// <param name="transparentIndex">(Optional) Sets the transparent index (if existing, usually 0)</param>
        /// <param name="newSize">(Optional) If true, resizes the image</param>
        /// <param name="associatedTiles">(Optional) If attempting to render a screen image, has these associated tiles</param>
        /// <returns>Width of new bitmap image</returns>
        public int SetImage(SKBitmap bitmap, bool setPalette = false, int transparentIndex = -1, bool newSize = false, GraphicsFile associatedTiles = null)
        {
            Edited = true;
            PnnQuantizer quantizer = new();

            if (FileFunction == Function.SCREEN)
            {
                return SetScreenImage(bitmap, quantizer, associatedTiles);
            }
            else if (IsTexture())
            {
                return SetTexture(bitmap, quantizer, newSize, transparentIndex == 0, setPalette);
            }
            else
            {
                return SetTiles(bitmap, quantizer, newSize, transparentIndex == 0, setPalette);
            }
        }

        private int SetTexture(SKBitmap bitmap, PnnQuantizer quantizer, bool newSize, bool firstTransparent, bool setPalette)
        {
            if (!VALID_WIDTHS.Contains(bitmap.Width))
            {
                throw new ArgumentException($"Image width {bitmap.Width} not a valid width.");
            }
            int calculatedHeight = PixelData.Count / bitmap.Width;
            if (newSize)
            {
                Log.LogWarning("Resizing texture... ");
                Width = bitmap.Width;
                Height = bitmap.Height;
                PixelData = new(new byte[bitmap.Width * bitmap.Height]);
            }
            else if (bitmap.Height < calculatedHeight)
            {
                throw new ArgumentException($"Image height {bitmap.Height} does not match calculated height {calculatedHeight}.");
            }

            quantizer.QuantizeImage(bitmap, this, 256, texture: true, dither: true, firstTransparent, setPalette, Log);

            return bitmap.Width;
        }

        private int SetTiles(SKBitmap bitmap, PnnQuantizer quantizer, bool newSize, bool firstTransparent, bool setPalette)
        {
            if (!VALID_WIDTHS.Contains(bitmap.Width))
            {
                throw new ArgumentException($"Image width {bitmap.Width} not a valid width.");
            }
            int calculatedHeight = PixelData.Count / (bitmap.Width / (ImageTileForm == TileForm.GBA_4BPP ? 2 : 1));
            if (newSize)
            {
                Log.LogWarning("Resizing tiled image... ");
                PixelData = new(new byte[bitmap.Width * bitmap.Height]);
            }
            else if (bitmap.Height < calculatedHeight)
            {
                throw new ArgumentException($"Image height {bitmap.Height} does not match calculated height {calculatedHeight}.");
            }

            quantizer.QuantizeImage(bitmap, this, ImageTileForm == TileForm.GBA_4BPP ? 16 : 256, texture: false, dither: true, firstTransparent, setPalette, Log);           

            return bitmap.Width;
        }
    }
}
