using HaruhiChokuretsuLib.Util;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HaruhiChokuretsuLib.Archive.Graphics
{
    public partial class GraphicsFile
    {
        /// <summary>
        /// Data for screen file
        /// </summary>
        public List<ScreenDataEntry> ScreenData { get; set; } = [];
        /// <summary>
        /// Gets a rendered screen image
        /// </summary>
        /// <param name="tilesGrp">The graphics file representing tiles to use to render the image</param>
        /// <returns>A rendered SKBitmap of the screen image</returns>
        public SKBitmap GetScreenImage(GraphicsFile tilesGrp)
        {
            SKBitmap bitmap = new(256, 192);
            List<SKBitmap> tileImages = [];
            for (int palette = 0; palette <= ScreenData.Max(s => s.Palette); palette++)
            {
                tileImages.Add(tilesGrp.GetImage(paletteOffset: palette * 16));
            }
            using SKCanvas canvas = new(bitmap);

            List<List<SKBitmap>> tiles = new();
            for (int y = 0; y < tilesGrp.Height; y += 8)
            {
                for (int x = 0; x < tilesGrp.Width; x += 8)
                {
                    List<SKBitmap> tileVariants = new();
                    SKRect boundingBox = new()
                    {
                        Left = x,
                        Top = y,
                        Right = x + 8,
                        Bottom = y + 8,
                    };
                    foreach (SKBitmap tileImage in tileImages)
                    {
                        SKBitmap tile = new(8, 8);
                        using SKCanvas tileCanvas = new(tile);
                        tileCanvas.DrawBitmap(tileImage, boundingBox, new SKRect(0, 0, 8, 8));
                        tileCanvas.Flush();
                        tileVariants.Add(tile);
                    }
                    tiles.Add(tileVariants);
                }
            }

            int i = 0;
            for (int y = 0; y < bitmap.Height && i < ScreenData.Count; y += 8)
            {
                for (int x = 0; x < bitmap.Width && i < ScreenData.Count; x += 8)
                {
                    SKBitmap tile = new(8, 8);
                    using SKCanvas transformCanvas = new(tile);
                    if (ScreenData[i].Flip.HasFlag(ScreenTileFlip.HORIZONTAL))
                    {
                        transformCanvas.Scale(-1, 1, 4, 0);
                    }
                    if (ScreenData[i].Flip.HasFlag(ScreenTileFlip.VERTICAL))
                    {
                        transformCanvas.Scale(1, -1, 0, 4);
                    }
                    transformCanvas.DrawBitmap(tiles[ScreenData[i].Index - 1][ScreenData[i++].Palette], new SKPoint(0, 0));
                    transformCanvas.Flush();
                    canvas.DrawBitmap(tile, new SKRect(x, y, x + 8, y + 8));
                }
            }
            canvas.Flush();

            return bitmap;
        }

        /// <summary>
        /// Sets a screen image from a bitmap
        /// </summary>
        /// <param name="bitmap">Bitmap to set the screen image to</param>
        /// <param name="quantizer">A PnnQuantizer to quantize the image</param>
        /// <param name="associatedTiles">A graphics file to which the associated tiles will be set</param>
        /// <returns>The width of the screen image</returns>
        public int SetScreenImage(SKBitmap bitmap, PnnQuantizer quantizer, GraphicsFile associatedTiles)
        {
            if (bitmap.Width != 256 || bitmap.Height != 192)
            {
                Log.LogError("Screen image size must be 256x192");
                return 256;
            }

            List<SKBitmap> tiles = [];
            for (int y = 0; y < bitmap.Height; y += 8)
            {
                for (int x = 0; x < bitmap.Width; x += 8)
                {
                    SKBitmap tile = new(8, 8);
                    using SKCanvas tileCanvas = new(tile);
                    SKRect boundingBox = new()
                    {
                        Left = x,
                        Top = y,
                        Right = x + 8,
                        Bottom = y + 8,
                    };
                    tileCanvas.DrawBitmap(bitmap, boundingBox, new SKRect(0, 0, 8, 8));
                    tileCanvas.Flush();
                    tiles.Add(tile);
                }
            }

            List<SKColor> palette = Helpers.GetPaletteFromImage(bitmap, 16, Log, firstTransparent: true);
            palette = [.. palette.RotateSectionRight(0, palette.Count)];
            PnnQuantizer pnn = new();
            foreach (SKBitmap tile in tiles)
            {
                for (int y = 0; y < tile.Height; y++)
                {
                    for (int x = 0; x < tile.Width; x++)
                    {
                        tile.SetPixel(x, y, palette[pnn.DitherColorIndex([.. palette], (uint)tile.GetPixel(x, y), 0)]);
                    }
                }
            }

            List<SKBitmap> distinctTiles = [];
            foreach (SKBitmap tile in tiles)
            {
                if (!distinctTiles.Any(t => t.Pixels.SequenceEqual(tile.Pixels)))
                {
                    distinctTiles.Add(tile);
                }
            }

            if (distinctTiles.Count > 255)
            {
                Log.LogError($"Error attempting to replace screen image {Name} ({Index}): more than 256 tiles ({distinctTiles.Count}) generated from image; please use a less complex image");
                return 256;
            }

            int tileImageHeight = (distinctTiles.Count * 8 / 256 + 1) * 8;
            SKBitmap newTileImage = new(256, tileImageHeight);
            using SKCanvas newTileCanvas = new(newTileImage);

            int currentIndex = 0;
            for (int y = 0; y < tileImageHeight && currentIndex < distinctTiles.Count; y += 8)
            {
                for (int x = 0; x < 256 && currentIndex < distinctTiles.Count; x += 8)
                {
                    newTileCanvas.DrawBitmap(distinctTiles[currentIndex++], new SKPoint(x, y));
                }
            }
            newTileCanvas.Flush();

            palette.AddRange(new SKColor[48 - palette.Count]);
            associatedTiles.SetPalette(palette);
            associatedTiles.SetImage(newTileImage, newSize: true);

            ScreenData.Clear();
            foreach (SKBitmap tile in tiles)
            {
                ScreenData.Add(new()
                {
                    Palette = 0,
                    Index = (byte)(distinctTiles.FindIndex(t => t.Pixels.SequenceEqual(tile.Pixels)) + 1),
                });
            }

            return 256;
        }

        /// <summary>
        /// Gets the associated screen tiles from grp.bin
        /// </summary>
        /// <param name="grp">ArchiveFile object for grp.bin</param>
        /// <returns>A GraphicsFile of the screen image's tiles</returns>
        public GraphicsFile GetAssociatedScreenTiles(ArchiveFile<GraphicsFile> grp)
        {
            GraphicsFile associatedTiles = grp.Files.FirstOrDefault(f => f.FileFunction == Function.SHTX && f.Name.StartsWith(Name[0..^3]));
            associatedTiles ??= grp.Files.FirstOrDefault(f => f.FileFunction == Function.SHTX && f.Name.StartsWith(Name[0..^8]));
            associatedTiles ??= grp.GetFileByName("BG_SLG_T00DNX");

            return associatedTiles;
        }

        /// <summary>
        /// Screen data entry struct
        /// </summary>
        public struct ScreenDataEntry
        {
            /// <summary>
            /// Index of tile in tile graphic
            /// </summary>
            public byte Index { get; set; }
            /// <summary>
            /// Palette to use for this tile
            /// </summary>
            public byte Palette { get; set; }
            /// <summary>
            /// Direction to flip this tile
            /// </summary>
            public ScreenTileFlip Flip { get; set; }
        }

        /// <summary>
        /// Direction to flip a screen tile
        /// </summary>
        [Flags]
        public enum ScreenTileFlip : byte
        {
            /// <summary>
            /// Flip horizontally
            /// </summary>
            HORIZONTAL = 0x04,
            /// <summary>
            /// Flip vertically
            /// </summary>
            VERTICAL = 0x08,
        }
    }
}
