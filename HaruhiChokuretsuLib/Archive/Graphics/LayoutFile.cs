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
        /// In layout files, the list of layout layers
        /// </summary>
        public List<LayoutEntry> LayoutEntries { get; set; } = [];

        /// <summary>
        /// Renders a preview of a layout
        /// </summary>
        /// <param name="grpFiles">A list of graphics files to pull texture data from</param>
        /// <param name="entryIndex">The index of the first layer to render</param>
        /// <param name="numEntries">The number of layers to render</param>
        /// <param name="darkMode">If true, will render the background dark</param>
        /// <param name="preprocessedList">(Optional) If true, assumes the passed grp files are already in order; otherwise, assumes you're dropping all of grp.bin on it</param>
        /// <returns>A tuple containing a rendered SKBitmap of the layout and a list of the layout entries</returns>
        public (SKBitmap bitmap, List<LayoutEntry> layoutEntries) GetLayout(List<GraphicsFile> grpFiles, int entryIndex, int numEntries, bool darkMode, bool preprocessedList = false)
        {
            return GetLayout(grpFiles, LayoutEntries.Skip(entryIndex).Take(numEntries).ToList(), darkMode, preprocessedList);
        }

        /// <summary>
        /// Renders a preview of a layout
        /// </summary>
        /// <param name="grpFiles">A list of graphics files to pull texture data from</param>
        /// <param name="layoutEntries">A list of layout entries to render</param>
        /// <param name="darkMode">If true, will render the background dark</param>
        /// <param name="preprocessedList">(Optional) If true, assumes the passed grp files are already in order; otherwise, assumes you're dropping all of grp.bin on it</param>
        /// <returns>A tuple containing a rendered SKBitmap of the layout and a list of the layout entries</returns>
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

            Dictionary<int, SKBitmap> textures;
            if (preprocessedList)
            {
                textures = grpFiles.Select((g, i) => (i, g.IsTexture() ? g.GetTexture(transparentIndex: 0) : g.GetTiles(transparentIndex: 0))).ToDictionary();
            }
            else
            {
                IEnumerable<short> relativeIndices = layoutEntries.Select(l => l.RelativeShtxIndex).Distinct();
                textures = [];

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
                    GraphicsFile curGrp = grpFiles.First(g => g.Index == grpIndex - 1);
                    textures.Add(index, curGrp.IsTexture() ? curGrp.GetTexture(transparentIndex: 0) : curGrp.GetTiles(transparentIndex: 0));
                }
            }

            foreach (LayoutEntry currentEntry in layoutEntries)
            {
                if (currentEntry.RelativeShtxIndex < 0)
                {
                    continue;
                }

                canvas.DrawBitmap(currentEntry.GetTileBitmap(textures), currentEntry.GetDestination());
            }

            return (layoutBitmap, layoutEntries);
        }
    }

    /// <summary>
    /// Represents a layout layer
    /// </summary>
    public class LayoutEntry
    {
        /// <summary>
        /// Unknown
        /// </summary>
        public short UnknownShort1 { get; set; }
        /// <summary>
        /// Index of the texture to use
        /// </summary>
        public short RelativeShtxIndex { get; set; }
        /// <summary>
        /// Unknown
        /// </summary>
        public short UnknownShort2 { get; set; }
        /// <summary>
        /// X position of the cropped texture on the screen
        /// </summary>
        public short ScreenX { get; set; }
        /// <summary>
        /// Y position of the cropped texture on the screen
        /// </summary>
        public short ScreenY { get; set; }
        /// <summary>
        /// Width of the cropped texture
        /// </summary>
        public short TextureW { get; set; }
        /// <summary>
        /// Height of the cropped texture
        /// </summary>
        public short TextureH { get; set; }
        /// <summary>
        /// X position of the crop on the texture
        /// </summary>
        public short TextureX { get; set; }
        /// <summary>
        /// Y position of the crop on the texture
        /// </summary>
        public short TextureY { get; set; }
        /// <summary>
        /// Width of the cropped texture on the screen
        /// </summary>
        public short ScreenW { get; set; }
        /// <summary>
        /// Height of the cropped texture on the screen
        /// </summary>
        public short ScreenH { get; set; }
        /// <summary>
        /// Unknown
        /// </summary>
        public short UnknownShort3 { get; set; }
        /// <summary>
        /// Color to tint the texture while rendering
        /// </summary>
        public SKColor Tint { get; set; }

        /// <summary>
        /// Empty constructor, used for serialization
        /// </summary>
        public LayoutEntry()
        {
        }
        
        /// <summary>
        /// Create a layout entry from data
        /// </summary>
        /// <param name="data">Binary data representing layout entry</param>
        /// <exception cref="ArgumentException"></exception>
        public LayoutEntry(byte[] data)
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

        /// <summary>
        /// Constructor for building a layout entry from scratch
        /// </summary>
        /// <param name="shtxIndex">The SHTX index of this entry</param>
        /// <param name="srcX">The X position to begin cropping from on the source texture</param>
        /// <param name="srcY">The Y position to begin cropping from on the source texture</param>
        /// <param name="srcW">The width of the quad to crop from the source texture</param>
        /// <param name="srcH">The height of the quad to crop from the source texture</param>
        /// <param name="dstX">The X position on the screen to draw the quad</param>
        /// <param name="dstY">The Y position on the screen to draw the quad</param>
        /// <param name="dstW">The width of the quad to draw to the screen</param>
        /// <param name="dstH">The height of the quad to draw to the screen</param>
        /// <param name="tint">the tint to apply to the texture on the screen</param>
        public LayoutEntry(short shtxIndex, short srcX, short srcY, short srcW, short srcH, short dstX, short dstY, short dstW, short dstH, SKColor tint)
        {
            RelativeShtxIndex = shtxIndex;
            TextureX = srcX;
            TextureY = srcY;
            TextureW = srcW;
            TextureH = srcH;
            ScreenX = dstX;
            ScreenY = dstY;
            ScreenW = dstW;
            ScreenH = dstH;
            Tint = tint;
        }

        /// <summary>
        /// Gets binary representation of the layout
        /// </summary>
        /// <returns>Byte array of that binary representation</returns>
        public byte[] GetBytes()
        {
            List<byte> data =
            [
                .. BitConverter.GetBytes(UnknownShort1),
                .. BitConverter.GetBytes(RelativeShtxIndex),
                .. BitConverter.GetBytes(UnknownShort2),
                .. BitConverter.GetBytes(ScreenX),
                .. BitConverter.GetBytes(ScreenY),
                .. BitConverter.GetBytes(TextureW),
                .. BitConverter.GetBytes(TextureH),
                .. BitConverter.GetBytes(TextureX),
                .. BitConverter.GetBytes(TextureY),
                .. BitConverter.GetBytes(ScreenW),
                .. BitConverter.GetBytes(ScreenH),
                .. BitConverter.GetBytes(UnknownShort3),
                .. BitConverter.GetBytes((uint)Tint),
            ];
            return [.. data];
        }

        /// <summary>
        /// Gets an SKBitmap representation of the layout entry's tile
        /// </summary>
        /// <param name="textures">The dictionary of textures needed by this layout</param>
        /// <returns>A tile representation of the layout with appropriate cropping, transforming, and tinting</returns>
        public SKBitmap GetTileBitmap(Dictionary<int, SKBitmap> textures)
        {
            SKRect boundingBox = GetTileBounds();

            SKBitmap texture = textures[RelativeShtxIndex];
            int tileWidth = (int)Math.Abs(boundingBox.Right - boundingBox.Left);
            int tileHeight = (int)Math.Abs(boundingBox.Bottom - boundingBox.Top);
            SKBitmap tile = new(tileWidth, tileHeight);
            SKCanvas transformCanvas = new(tile);

            if (ScreenW < 0)
            {
                transformCanvas.Scale(-1, 1, tileWidth / 2.0f, 0);
            }
            if (ScreenH < 0)
            {
                transformCanvas.Scale(1, -1, 0, tileHeight / 2.0f);
            }
            transformCanvas.DrawBitmap(texture, boundingBox, new SKRect(0, 0, Math.Abs(tileWidth), Math.Abs(tileHeight)));
            transformCanvas.Flush();

            if (Tint != SKColors.White)
            {
                for (int x = 0; x < tileWidth; x++)
                {
                    for (int y = 0; y < tileHeight; y++)
                    {
                        SKColor pixelColor = tile.GetPixel(x, y);
                        tile.SetPixel(x, y, new((byte)(pixelColor.Red * Tint.Red / 255),
                            (byte)(pixelColor.Green * Tint.Green / 255),
                            (byte)(pixelColor.Blue * Tint.Blue / 255),
                            (byte)(pixelColor.Alpha * Tint.Alpha / 255)));
                    }
                }
            }

            return tile;
        }

        /// <summary>
        /// Gets the bounding box of the tile on the source texture
        /// </summary>
        /// <returns>An SKRect representing the bounding box of the tile on the source texture</returns>
        public SKRect GetTileBounds()
        {
            return new()
            {
                Left = TextureX,
                Top = TextureY,
                Right = TextureX + TextureW,
                Bottom = TextureY + TextureH,
            };
        }

        /// <summary>
        /// Gets the definition of the location/size of the layout entry's tile to be drawn to the screen
        /// </summary>
        /// <returns>An SKRect representing the position and size of the tile on the screen</returns>
        public SKRect GetDestination()
        {
            return new()
            {
                Left = ScreenX,
                Top = ScreenY,
                Right = ScreenX + Math.Abs(ScreenW),
                Bottom = ScreenY + Math.Abs(ScreenH),
            };
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"Index: {RelativeShtxIndex}; TX: {TextureX} {TextureY} {TextureX + TextureW} {TextureY + TextureH}, SC: {ScreenX} {ScreenY} {ScreenX + ScreenW} {ScreenY + ScreenH}";
        }
    }
}
