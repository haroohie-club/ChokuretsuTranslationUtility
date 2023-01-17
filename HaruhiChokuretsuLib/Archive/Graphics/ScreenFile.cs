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

        public GraphicsFile GetAssociateScreenTiles(ArchiveFile<GraphicsFile> grp)
        {
            GraphicsFile associatedTiles = grp.Files.FirstOrDefault(f => f.FileFunction == Function.SHTX && f.Name.StartsWith(Name[0..^3]));
            if (associatedTiles is null)
            {
                associatedTiles = grp.Files.FirstOrDefault(f => f.FileFunction == Function.SHTX && f.Name.StartsWith(Name[0..^8]));
            }

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
