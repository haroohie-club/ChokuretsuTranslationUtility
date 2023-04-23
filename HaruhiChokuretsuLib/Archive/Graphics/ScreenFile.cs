using HaruhiChokuretsuLib.Util;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HaruhiChokuretsuLib.Archive.Graphics
{
    public partial class GraphicsFile
    {
        public List<ScreenDataEntry> ScreenData { get; set; } = new();
        public SKBitmap GetScreenImage(GraphicsFile tilesGrp)
        {
            SKBitmap bitmap = new(256, 192);
            List<SKBitmap> tileImages = new();
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

        public int SetScreenImage(SKBitmap bitmap, PnnLABQuantizer quantizer, GraphicsFile associatedTiles)
        {
            if (bitmap.Width != 256 || bitmap.Height != 192)
            {
                _log.LogError("Screen image size must be 256x192");
                return 256;
            }

            List<SKBitmap> tiles = new();
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

            List<SKColor> palette = Helpers.GetPaletteFromImage(bitmap, 16);
            foreach (SKBitmap tile in tiles)
            {
                for (int y = 0; y < tile.Height; y++)
                {
                    for (int x = 0; x < tile.Width; x++)
                    {
                        tile.SetPixel(x, y, palette[Helpers.ClosestColorIndex(palette, tile.GetPixel(x, y), true)]);
                    }
                }
            }

            List<SKBitmap> distinctTiles = new();
            foreach (SKBitmap tile in tiles)
            {
                if (!distinctTiles.Any(t => t.Pixels.SequenceEqual(tile.Pixels)))
                {
                    distinctTiles.Add(tile);
                }
            }

            if (distinctTiles.Count > 255)
            {
                _log.LogError($"Error attempting to replace screen image {Name} ({Index}): more than 256 tiles ({distinctTiles.Count}) generated from image; please use a less complex image");
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

            associatedTiles.SetImage(newTileImage, newSize: true);
            associatedTiles.SetPalette(palette);

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

        public GraphicsFile GetAssociatedScreenTiles(ArchiveFile<GraphicsFile> grp)
        {
            GraphicsFile associatedTiles = grp.Files.FirstOrDefault(f => f.FileFunction == Function.SHTX && f.Name.StartsWith(Name[0..^3]));
            associatedTiles ??= grp.Files.FirstOrDefault(f => f.FileFunction == Function.SHTX && f.Name.StartsWith(Name[0..^8]));

            return associatedTiles;
        }

        public struct ScreenDataEntry
        {
            public byte Index { get; set; }
            public byte Palette { get; set; }
            public ScreenTileFlip Flip { get; set; }
        }

        [Flags]
        public enum ScreenTileFlip : byte
        {
            HORIZONTAL = 0x04,
            VERTICAL = 0x08,
        }
    }
}
