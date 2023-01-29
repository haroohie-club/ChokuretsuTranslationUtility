using HaruhiChokuretsuLib.Util;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HaruhiChokuretsuLib.Archive.Graphics
{
    public partial class GraphicsFile
    {
        public List<LayoutEntry> LayoutEntries { get; set; } = new();

        public (SKBitmap bitmap, List<LayoutEntry> layoutEntries) GetLayout(List<GraphicsFile> grpFiles, int entryIndex, int numEntries, bool darkMode, bool preprocessedList = false)
        {
            return GetLayout(grpFiles, LayoutEntries.Skip(entryIndex).Take(numEntries).ToList(), darkMode, preprocessedList);
        }

        public (SKBitmap bitmap, List<LayoutEntry> layouts) GetLayout(List<GraphicsFile> grpFiles, List<LayoutEntry> layoutEntries, bool darkMode, bool preprocessedList = false)
        {
            LayoutEntry maxX = LayoutEntries.OrderByDescending(l => l.ScreenX).First();
            LayoutEntry maxY = LayoutEntries.OrderByDescending(l => l.ScreenY).First();
            Width = maxX.ScreenX + maxX.ScreenW;
            Height = maxY.ScreenY + maxY.ScreenH;
            SKBitmap layoutBitmap = new(Width, Height);
            SKCanvas canvas = new(layoutBitmap);

            if (darkMode)
            {
                canvas.DrawRect(0, 0, layoutBitmap.Width, layoutBitmap.Height, new() { Color = SKColors.Black });
            }

            List<SKBitmap> textures;
            if (preprocessedList)
            {
                textures = grpFiles.Select(g => g.GetTexture(transparentIndex: 0)).ToList();
            }
            else
            {
                IEnumerable<short> relativeIndices = layoutEntries.Select(l => l.RelativeShtxIndex).Distinct();
                textures = new();

                foreach (short index in relativeIndices)
                {
                    int grpIndex = Index + 1;
                    for (int i = 0; i <= index && grpIndex < grpFiles.Count; grpIndex++)
                    {
                        if (grpFiles.First(g => g.Index == grpIndex).FileFunction == Function.SHTX)
                        {
                            i++;
                        }
                    }
                    textures.Add(grpFiles.First(g => g.Index == grpIndex - 1).GetTexture(transparentIndex: 0));
                }
            }

            foreach (LayoutEntry currentEntry in layoutEntries)
            {
                if (currentEntry.RelativeShtxIndex < 0)
                {
                    continue;
                }

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
                    Right = currentEntry.ScreenX + Math.Abs(currentEntry.ScreenW),
                    Bottom = currentEntry.ScreenY + Math.Abs(currentEntry.ScreenH),
                };

                SKBitmap texture = textures[currentEntry.RelativeShtxIndex];
                int tileWidth = (int)Math.Abs(boundingBox.Right - boundingBox.Left);
                int tileHeight = (int)Math.Abs(boundingBox.Bottom - boundingBox.Top);
                SKBitmap tile = new(tileWidth, tileHeight);
                SKCanvas transformCanvas = new(tile);

                if (currentEntry.ScreenW < 0)
                {
                    transformCanvas.Scale(-1, 1, tileWidth / 2.0f, 0);
                }
                if (currentEntry.ScreenH < 0)
                {
                    transformCanvas.Scale(1, -1, 0, tileHeight / 2.0f);
                }
                transformCanvas.DrawBitmap(texture, boundingBox, new SKRect(0, 0, Math.Abs(tileWidth), Math.Abs(tileHeight)));
                transformCanvas.Flush();

                if (currentEntry.Tint != SKColors.White)
                {
                    for (int x = 0; x < tileWidth; x++)
                    {
                        for (int y = 0; y < tileHeight; y++)
                        {
                            SKColor pixelColor = tile.GetPixel(x, y);
                            tile.SetPixel(x, y, new((byte)(pixelColor.Red * currentEntry.Tint.Red / 255),
                                (byte)(pixelColor.Green * currentEntry.Tint.Green / 255),
                                (byte)(pixelColor.Blue * currentEntry.Tint.Blue / 255),
                                (byte)(pixelColor.Alpha * currentEntry.Tint.Alpha / 255)));
                        }
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

            UnknownShort1 = IO.ReadShort(data, 0);
            RelativeShtxIndex = IO.ReadShort(data, 0x02);
            UnknownShort2 = IO.ReadShort(data, 0x04);
            ScreenX = IO.ReadShort(data, 0x06);
            ScreenY = IO.ReadShort(data, 0x08);
            TextureW = IO.ReadShort(data, 0x0A);
            TextureH = IO.ReadShort(data, 0x0C);
            TextureX = IO.ReadShort(data, 0x0E);
            TextureY = IO.ReadShort(data, 0x10);
            ScreenW = IO.ReadShort(data, 0x12);
            ScreenH = IO.ReadShort(data, 0x14);
            UnknownShort3 = IO.ReadShort(data, 0x16);
            uint tint = IO.ReadUInt(data, 0x18);
            if (tint == 0x80808080)
            {
                tint = 0xFFFFFFFF;
            }
            Tint = new(tint);
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
