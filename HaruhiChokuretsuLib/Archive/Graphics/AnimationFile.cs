using HaruhiChokuretsuLib.Util;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HaruhiChokuretsuLib.Archive.Graphics
{
    public partial class GraphicsFile
    {
        public List<AnimationEntry> AnimationEntries { get; set; } = new();

        public List<GraphicsFile> GetAnimationFrames(GraphicsFile texture)
        {
            List<GraphicsFile> graphicFrames = new();

            if (AnimationEntries[0].GetType() == typeof(PaletteRotateAnimationEntry))
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
                                    texture.Data = texture.GetBytes().ToList();
                                    break;
                                case 2:
                                    texture.SetPalette(texture.Palette.RotateSectionLeft(animationEntry.PaletteOffset, animationEntry.SwapAreaSize).ToList(), suppressOutput: true);
                                    texture.Data = texture.GetBytes().ToList();
                                    break;
                                case 3:
                                    throw new NotImplementedException();
                                default:
                                    throw new ArgumentException($"Invalid animation type on palette rotation animation entry ({animationEntry.AnimationType})");
                            }
                        }
                    }
                    graphicFrames.Add(texture.CastTo<GraphicsFile>()); // creates a new instance of the graphics file
                }
            }
            else if (AnimationEntries[0].GetType() == typeof(PaletteColorAnimationEntry))
            {
                SKColor[] originalPalette = Array.Empty<SKColor>();
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
                    texture.Data = texture.GetBytes().ToList();
                    graphicFrames.Add(texture.CastTo<GraphicsFile>());

                    if (f == 0)
                    {
                        originalPalette = texture.Palette.ToArray();
                    }
                    else if (f > 0 && !changedOnce && !graphicFrames[f].Palette.SequenceEqual(originalPalette))
                    {
                        changedOnce = true;
                        originalPalette = texture.Palette.ToArray();
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
    }

    public class AnimationEntry
    {
    }

    public class PaletteColorAnimationEntry : AnimationEntry
    {
        public short PaletteOffset { get; set; }
        public byte Determinant { get; set; }
        public byte ColorIndexer { get; set; }
        public List<short> ColorArray { get; set; } = new();
        public short RedComponent { get; set; }
        public short GreenComponent { get; set; }
        public short BlueComponent { get; set; }
        public short Color { get; set; }

        public int NumColors { get; set; }

        public PaletteColorAnimationEntry(IEnumerable<byte> data)
        {
            PaletteOffset = BitConverter.ToInt16(data.Take(2).ToArray());
            Determinant = data.ElementAt(0x02);
            ColorIndexer = data.ElementAt(0x03);
            for (int i = 0; i < 96; i++)
            {
                ColorArray.Add(BitConverter.ToInt16(data.Skip(0x04 + i * 2).Take(2).ToArray()));
            }
            RedComponent = BitConverter.ToInt16(data.Skip(0xC4).Take(2).ToArray());
            GreenComponent = BitConverter.ToInt16(data.Skip(0xC6).Take(2).ToArray());
            BlueComponent = BitConverter.ToInt16(data.Skip(0xC8).Take(2).ToArray());
            Color = BitConverter.ToInt16(data.Skip(0xCA).Take(2).ToArray());
        }

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

        public override string ToString()
        {
            return $"CAN Off: {PaletteOffset:X4} Unk: {Determinant:X4}";
        }
    }

    public class PaletteRotateAnimationEntry : AnimationEntry
    {
        public short PaletteOffset { get; set; }
        public short SwapSize { get; set; }
        public byte SwapAreaSize { get; set; }
        public byte FramesPerTick { get; set; }
        public short AnimationType { get; set; }

        public PaletteRotateAnimationEntry(IEnumerable<byte> data)
        {
            PaletteOffset = BitConverter.ToInt16(data.Take(2).ToArray());
            SwapSize = BitConverter.ToInt16(data.Skip(0x02).Take(2).ToArray());
            SwapAreaSize = data.ElementAt(0x04);
            FramesPerTick = data.ElementAt(0x05);
            AnimationType = BitConverter.ToInt16(data.Skip(0x06).Take(2).ToArray());
        }

        public override string ToString()
        {
            return $"PAN Off: {PaletteOffset:X4} Type: {AnimationType} FPT: {FramesPerTick} Size: {SwapSize}x{SwapAreaSize}";
        }
    }
}
