using HaruhiChokuretsuLib.Util;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HaruhiChokuretsuLib.Archive.Graphics;

public partial class GraphicsFile
{
    /// <summary>
    /// In an animation file, the list of animation entries
    /// </summary>
    public List<AnimationEntry> AnimationEntries { get; set; } = [];
    /// <summary>
    /// The X coordinate of where the animation should be drawn on the layout canvas
    /// </summary>
    public short AnimationX { get; set; }
    /// <summary>
    /// The Y coordinate of where the animation should be drawn on the layout canvas
    /// </summary>
    public short AnimationY { get; set; }
    /// <summary>
    /// The type of chibi animation (if this is a chibi animation)
    /// </summary>
    public short ChibiAnimationType { get; set; }

    /// <summary>
    /// Gets the frames of an animation
    /// </summary>
    /// <param name="texture">A graphics file to pull texture data from</param>
    /// <returns>A list of GraphicsFiles each representing a distinct animation frame</returns>
    public List<GraphicsFile> GetAnimationFrames(GraphicsFile texture)
    {
        List<GraphicsFile> graphicFrames = [];
        if (texture.FileFunction != Function.SHTX)
        {
            return graphicFrames;
        }

        if (AnimationEntries[0].GetType() == typeof(FrameAnimationEntry) && AnimationEntries.Cast<FrameAnimationEntry>().All(f => f.FrameOffset >= 0))
        {
            foreach (FrameAnimationEntry animationEntry in AnimationEntries.Cast<FrameAnimationEntry>())
            {
                GraphicsFile frame = new()
                {
                    FileFunction = Function.SHTX,
                    ImageForm = Form.TEXTURE,
                    ImageTileForm = TileForm.GBA_8BPP,
                    Log = Log,
                    Palette = texture.Palette
                };
                if (frame.Palette.Count > 0)
                {
                    frame.Palette[0] = SKColors.Transparent;
                }
                frame.PaletteData = texture.PaletteData;
                frame.Width = animationEntry.FrameWidth;
                frame.Height = animationEntry.FrameHeight;
                frame.PixelData = [];
                for (int i = animationEntry.FrameOffset; i < (texture.PixelData?.Count ?? 0); i += AnimationEntries.DistinctBy(a => ((FrameAnimationEntry)a).FrameOffset).Count() * animationEntry.FrameWidth)
                {
                    frame.PixelData.AddRange(texture.PixelData!.Skip(i).Take(animationEntry.FrameWidth));
                }
                graphicFrames.Add(frame);
            }
        }
        else if (AnimationEntries[0].GetType() == typeof(PaletteRotateAnimationEntry))
        {
            int numFrames = Helpers.LeastCommonMultiple(AnimationEntries
                .Where(a => ((PaletteRotateAnimationEntry)a).FramesPerTick * ((PaletteRotateAnimationEntry)a).SwapAreaSize != 0)
                .Select(a => ((PaletteRotateAnimationEntry)a).FramesPerTick * ((PaletteRotateAnimationEntry)a).SwapAreaSize));
            for (int f = 0; f < numFrames; f++)
            {
                foreach (PaletteRotateAnimationEntry animationEntry in AnimationEntries.Cast<PaletteRotateAnimationEntry>())
                {
                    if (animationEntry.FramesPerTick > 0 && f % animationEntry.FramesPerTick == 0)
                    {
                        switch (animationEntry.AnimationType)
                        {
                            case 1:
                                texture.SetPalette(texture.Palette.RotateSectionRight(animationEntry.PaletteOffset, animationEntry.SwapAreaSize).ToList(), suppressOutput: true);
                                texture.Data = [.. texture.GetBytes()];
                                break;
                            case 2:
                                texture.SetPalette(texture.Palette.RotateSectionLeft(animationEntry.PaletteOffset, animationEntry.SwapAreaSize).ToList(), suppressOutput: true);
                                texture.Data = [.. texture.GetBytes()];
                                break;
                            case 3:
                                Log.LogError($"Case 3 animation frames not implemented.");
                                break;
                            default:
                                Log.LogError($"Invalid animation type on palette rotation animation entry ({animationEntry.AnimationType})");
                                break;
                        }
                    }
                }
                graphicFrames.Add(texture.CastTo<GraphicsFile>()); // creates a new instance of the graphics file
            }
        }
        else if (AnimationEntries[0].GetType() == typeof(PaletteColorAnimationEntry))
        {
            SKColor[] originalPalette = [];
            foreach (PaletteColorAnimationEntry animationEntry in AnimationEntries.Cast<PaletteColorAnimationEntry>())
            {
                animationEntry.Prepare(texture);
            }

            int numFrames = 1080;

            int iterator = 0;
            bool changedOnce = false, changedTwice = false;
            for (int f = 0; f < numFrames; f++)
            {
                foreach (PaletteColorAnimationEntry animationEntry in AnimationEntries.Cast<PaletteColorAnimationEntry>())
                {
                    animationEntry.ColorIndexer = 0;
                    if (animationEntry.ColorArray[1] != 0)
                    {
                        int colorArrayIndex = 0;
                        int localIterator = 0;
                        int v13 = 0;
                        for (; colorArrayIndex < animationEntry.ColorArray.Count - 3; colorArrayIndex += 3)
                        {
                            if (animationEntry.ColorArray[colorArrayIndex + 1] == 0)
                            {
                                short newColorIndex = animationEntry.ColorArray[3 * animationEntry.ColorIndexer - 3];
                                SKColor newColor = Helpers.Rgb555ToSKColor(newColorIndex);
                                if ((animationEntry.Determinant & 0x10) == 0)
                                {
                                    newColor = texture.Palette[newColorIndex];
                                }
                                animationEntry.RedComponent = (short)(newColor.Red >> 3);
                                animationEntry.GreenComponent = (short)(newColor.Green >> 3);
                                animationEntry.BlueComponent = (short)(newColor.Blue >> 3);
                                colorArrayIndex = 0;
                                animationEntry.ColorIndexer = 0;
                            }

                            if (animationEntry.ColorArray[colorArrayIndex + 1] > iterator - localIterator)
                            {
                                break;
                            }

                            animationEntry.RedComponent = (byte)(animationEntry.ColorArray[colorArrayIndex] & 0x1F);
                            animationEntry.GreenComponent = (byte)(animationEntry.ColorArray[colorArrayIndex] >> 5 & 0x1F);
                            animationEntry.BlueComponent = (byte)(animationEntry.ColorArray[colorArrayIndex] >> 10 & 0x1F);
                            localIterator += animationEntry.ColorArray[colorArrayIndex + 1];
                            animationEntry.ColorIndexer++;
                        }
                        animationEntry.ColorArray[colorArrayIndex + 2] = (short)(iterator - localIterator);
                        int v17 = animationEntry.Determinant;
                        int v23 = 0;
                        if ((v17 & 0x10) != 0 && animationEntry.ColorArray[colorArrayIndex + 1] != 0)
                        {
                            bool v18 = (v17 & 0x10) == 0;
                            if ((animationEntry.Determinant & 0x10) != 0)
                            {
                                v13 = animationEntry.ColorArray[colorArrayIndex];
                            }
                            else
                            {
                                v17 = animationEntry.ColorArray[colorArrayIndex];
                            }
                            if (v18)
                            {
                                v13 = Helpers.SKColorToRgb555(texture.Palette[v17]);
                            }

                            int internalIterator = 0;
                            int redComponent13 = v13 & 0x1F;
                            int greenComponent13 = v13 >> 5 & 0x1F;
                            int blueComponent13 = v13 >> 10 & 0x1F;
                            do
                            {
                                int v22 = internalIterator switch
                                {
                                    0 => animationEntry.RedComponent,
                                    1 => animationEntry.GreenComponent,
                                    _ => animationEntry.BlueComponent,
                                };
                                switch (internalIterator)
                                {
                                    case 0:
                                        redComponent13 = v22 + (redComponent13 - v22) * animationEntry.ColorArray[colorArrayIndex + 2] / animationEntry.ColorArray[colorArrayIndex + 1];
                                        break;
                                    case 1:
                                        greenComponent13 = v22 + (greenComponent13 - v22) * animationEntry.ColorArray[colorArrayIndex + 2] / animationEntry.ColorArray[colorArrayIndex + 1];
                                        break;
                                    case 2:
                                        blueComponent13 = v22 + (blueComponent13 - v22) * animationEntry.ColorArray[colorArrayIndex + 2] / animationEntry.ColorArray[colorArrayIndex + 1];
                                        break;
                                }
                                internalIterator++;
                            } while (internalIterator < 3);
                            v23 = redComponent13 & 0x1F | greenComponent13 << 5 & 0x3FF | blueComponent13 << 10 & 0x7FFF;
                        }
                        else
                        {
                            v23 = animationEntry.ColorArray[colorArrayIndex];
                        }
                        texture.Palette[animationEntry.PaletteOffset] = Helpers.Rgb555ToSKColor((short)v23);
                    }
                }
                iterator += 0x20;
                if (iterator >= 0x17E0)
                {
                    iterator = 0;
                }
                texture.SetPalette(texture.Palette, suppressOutput: true);
                texture.Data = [.. texture.GetBytes()];
                graphicFrames.Add(texture.CastTo<GraphicsFile>());

                if (f == 0)
                {
                    originalPalette = [.. texture.Palette];
                }
                else if (f > 0 && !changedOnce && !graphicFrames[f].Palette.SequenceEqual(originalPalette))
                {
                    changedOnce = true;
                    originalPalette = [.. texture.Palette];
                }
                else if (f > 0 && changedOnce && !changedTwice && !graphicFrames[f].Palette.SequenceEqual(originalPalette))
                {
                    changedTwice = true;
                }
                else if (changedTwice && graphicFrames[f].Palette.SequenceEqual(originalPalette))
                {
                    break;
                }
            }
        }

        return graphicFrames;
    }

    /// <summary>
    /// For frame animations, given a list of frames and timings and a palette, creates a new animation and texture for that animation
    /// </summary>
    /// <param name="framesAndTimings">A list of tuples containing the SKBitmap frames and short timings for each animation frame</param>
    /// <param name="palette">The palette to use for the texture file</param>
    /// <returns>A GraphicsFile representing the texture the animation file will use</returns>
    public GraphicsFile SetFrameAnimationAndGetTexture(List<(SKBitmap Frame, short Time)> framesAndTimings, List<SKColor> palette)
    {
        if (framesAndTimings is null || framesAndTimings.Count == 0)
        {
            Log.LogError("No frames provided!");
            return null;
        }
        if (framesAndTimings.DistinctBy(ft => ft.Frame.Width).Count() > 1 || framesAndTimings.DistinctBy(ft => ft.Frame.Height).Count() > 1)
        {
            Log.LogError("Frames are not all the same width/height!");
            return null;
        }

        List<SKBitmap> uniqueFrames = [];
        List<(int index, short time)> indicesAndTimings = [];
        foreach ((SKBitmap frame, short time) in framesAndTimings)
        {
            if (!uniqueFrames.Any(f => f.Pixels.SequenceEqual(frame.Pixels)))
            {
                indicesAndTimings.Add((uniqueFrames.Count, time));
                uniqueFrames.Add(frame);
            }
            else
            {
                indicesAndTimings.Add((uniqueFrames.FindIndex(f => f.Pixels.SequenceEqual(frame.Pixels)), time));
            }
        }

        int frameWidth = framesAndTimings.First().Frame.Width;
        int frameHeight = framesAndTimings.First().Frame.Height;
        SKBitmap newTextureBitmap = new(Helpers.NextPowerOf2(frameWidth * uniqueFrames.Count), Helpers.NextPowerOf2(frameHeight));
        SKColor[] newPixels = new SKColor[newTextureBitmap.Pixels.Length];

        for (int y = 0; y < frameHeight; y++)
        {
            for (int i = 0; i < uniqueFrames.Count; i++)
            {
                Array.Copy(uniqueFrames[i].Pixels, y * frameWidth, newPixels, (i + y * uniqueFrames.Count) * frameWidth, frameWidth);
            }
        }
        newTextureBitmap.Pixels = newPixels;

        palette[0] = new(palette[0].Red, palette[0].Green, palette[0].Blue, 0); // transparent
        GraphicsFile newTexture = new()
        {
            FileFunction = Function.SHTX,
            ImageForm = Form.TEXTURE,
            ImageTileForm = TileForm.GBA_8BPP,
            Log = Log,
            Palette = palette,
            Width = newTextureBitmap.Width,
            Height = newTextureBitmap.Height,
            PixelData = [],
            PaletteData = [],
        };
        newTexture.SetImage(newTextureBitmap, newSize: true);

        AnimationEntries.Clear();
        foreach ((int index, short time) in indicesAndTimings)
        {
            AnimationEntries.Add(new FrameAnimationEntry(index * frameWidth, (short)frameWidth, (short)frameHeight, time));
        }

        return newTexture;
    }
}

/// <summary>
/// Inherited class for animation entries
/// </summary>
public class AnimationEntry
{
}

/// <summary>
/// Animation entry for frame-based animation files
/// </summary>
public class FrameAnimationEntry : AnimationEntry
{
    /// <summary>
    /// The offset into the texture file for this frame
    /// </summary>
    public int FrameOffset { get; set; }
    /// <summary>
    /// The width of the frame
    /// </summary>
    public short FrameWidth { get; set; }
    /// <summary>
    /// The height of the frame
    /// </summary>
    public short FrameHeight { get; set; }
    /// <summary>
    /// The amount of time to display the frame (in frames as in fps)
    /// </summary>
    public short Time { get; set; }

    /// <summary>
    /// Parameterless constructor for serialization
    /// </summary>
    public FrameAnimationEntry()
    {
    }

    /// <summary>
    /// Creates an animation frame from scratch
    /// </summary>
    /// <param name="frameOffset">The frame offset</param>
    /// <param name="frameWidth">The frame width</param>
    /// <param name="frameHeight">The frame height</param>
    /// <param name="time">The amount of time to display the frame</param>
    public FrameAnimationEntry(int frameOffset, short frameWidth, short frameHeight, short time)
    {
        FrameOffset = frameOffset;
        FrameWidth = frameWidth;
        FrameHeight = frameHeight;
        Time = time;
    }
    /// <summary>
    /// Creates an animation frame from data
    /// </summary>
    /// <param name="data">Animation frame data</param>
    public FrameAnimationEntry(byte[] data)
    {
        FrameOffset = IO.ReadInt(data, 0);
        FrameWidth = IO.ReadShort(data, 4);
        FrameHeight = IO.ReadShort(data, 6);
        Time = IO.ReadShort(data, 8);
    }

    /// <summary>
    /// Gets binary representation of this frame animation entry
    /// </summary>
    /// <returns>Byte array of the frame animation entry data</returns>
    public List<byte> GetBytes()
    {
        List<byte> bytes =
        [
            .. BitConverter.GetBytes(FrameOffset),
            .. BitConverter.GetBytes(FrameWidth),
            .. BitConverter.GetBytes(FrameHeight),
            .. BitConverter.GetBytes(Time),
        ];
        return bytes;
    }
}

/// <summary>
/// Animation entry for palette color animations
/// </summary>
public class PaletteColorAnimationEntry : AnimationEntry
{
    /// <summary>
    /// The offset into the palette where the animation will take place
    /// </summary>
    public short PaletteOffset { get; set; }
    internal byte Determinant { get; set; }
    internal byte ColorIndexer { get; set; }
    /// <summary>
    /// A little complicated, but an array of colors that helps determine the animation
    /// </summary>
    public List<short> ColorArray { get; set; } = [];
    /// <summary>
    /// Red shift component
    /// </summary>
    public short RedComponent { get; set; }
    /// <summary>
    /// Green shift component
    /// </summary>
    public short GreenComponent { get; set; }
    /// <summary>
    /// Blue shift component
    /// </summary>
    public short BlueComponent { get; set; }
    /// <summary>
    /// Overall color shift
    /// </summary>
    public short Color { get; set; }

    /// <summary>
    /// Unknown
    /// </summary>
    public int NumColors { get; set; }

    /// <summary>
    /// Parameterless constructor for serialization
    /// </summary>
    public PaletteColorAnimationEntry()
    {
    }
    
    /// <summary>
    /// Creates a palette color animation entry from data
    /// </summary>
    /// <param name="data">Data from animation file</param>
    public PaletteColorAnimationEntry(byte[] data)
    {
        PaletteOffset = IO.ReadShort(data, 0);
        Determinant = data.ElementAt(0x02);
        ColorIndexer = data.ElementAt(0x03);
        for (int i = 0; i < 96; i++)
        {
            ColorArray.Add(IO.ReadShort(data, 0x04 + i * 2));
        }
        RedComponent = IO.ReadShort(data, 0xC4);
        GreenComponent = IO.ReadShort(data, 0xC6);
        BlueComponent = IO.ReadShort(data, 0xC8);
        Color = IO.ReadShort(data, 0xCA);
    }

    /// <summary>
    /// Shift the colors on a graphics file for color animation
    /// </summary>
    /// <param name="texture"></param>
    public void Prepare(GraphicsFile texture)
    {
        ColorIndexer = 0;
        int colorIndex = 0;
        for (NumColors = 0; NumColors < 32 && ColorArray[colorIndex + 1] > 0; NumColors++)
        {
            ColorArray[colorIndex + 1] *= 32;
            ColorArray[colorIndex + 2] = 0;
            colorIndex += 3;
        }

        SKColor color = texture.Palette[PaletteOffset];
        RedComponent = (short)(color.Red / 8);
        GreenComponent = (short)(color.Green / 8);
        BlueComponent = (short)(color.Blue / 8);
        Color = (short)((ushort)RedComponent | (ushort)(GreenComponent << 5) | (ushort)(BlueComponent << 10));
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"CAN Off: {PaletteOffset:X4} Unk: {Determinant:X4}";
    }
}

/// <summary>
/// Animation enetry for palette rotation animations
/// </summary>
public class PaletteRotateAnimationEntry(byte[] data) : AnimationEntry
{
    /// <summary>
    /// Offset into the palette to rotate
    /// </summary>
    public short PaletteOffset { get; set; } = IO.ReadShort(data, 0);
    /// <summary>
    /// Number of colors to swap at once
    /// </summary>
    public short SwapSize { get; set; } = IO.ReadShort(data, 0x02);
    /// <summary>
    /// Size of rotation swatch
    /// </summary>
    public byte SwapAreaSize { get; set; } = data.ElementAt(0x04);
    /// <summary>
    /// Speed of the animation; higher values are slower
    /// </summary>
    public byte FramesPerTick { get; set; } = data.ElementAt(0x05);
    /// <summary>
    /// Unknown
    /// </summary>
    public short AnimationType { get; set; } = IO.ReadShort(data, 0x06);

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"PAN Off: {PaletteOffset:X4} Type: {AnimationType} FPT: {FramesPerTick} Size: {SwapSize}x{SwapAreaSize}";
    }
}