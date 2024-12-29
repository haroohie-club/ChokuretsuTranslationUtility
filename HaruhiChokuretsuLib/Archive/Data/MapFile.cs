using HaruhiChokuretsuLib.Archive.Graphics;
using HaruhiChokuretsuLib.Util;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HaruhiChokuretsuLib.Archive.Data
{
    /// <summary>
    /// Representation of a map file contained in dat.bin
    /// </summary>
    public class MapFile : DataFile
    {
        internal int NumSections { get; set; }
        internal int EndPointersOffset { get; set; }
        internal int HeaderEndPointer { get; set; }
        /// <summary>
        /// Data file sections of the map file
        /// </summary>
        public List<DataFileSection> SectionOffsetsAndCounts { get; set; } = [];

        /// <summary>
        /// Map file's settings
        /// </summary>
        public MapFileSettings Settings { get; set; }
        /// <summary>
        /// Unknown
        /// </summary>
        public List<UnknownMapObject2> UnknownMapObject2s { get; set; } = [];
        /// <summary>
        /// Unknown
        /// </summary>
        public List<UnknownMapObject3> UnknownMapObject3s { get; set; } = [];
        /// <summary>
        /// List of interactable objects found on the map
        /// </summary>
        public List<InteractableObject> InteractableObjects { get; set; } = [];
        /// <summary>
        /// A 2D byte-array representing the pathing/walkability of each space on the map
        /// </summary>
        public byte[][] PathingMap { get; set; }
        
        /// <inheritdoc/>
        public override void Initialize(byte[] decompressedData, int offset, ILogger log)
        {
            Log = log;
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            Offset = offset;
            Data = [.. decompressedData];

            NumSections = BitConverter.ToInt32(Data.Take(4).ToArray());
            EndPointersOffset = BitConverter.ToInt32(Data.Skip(0x04).Take(4).ToArray());
            HeaderEndPointer = BitConverter.ToInt32(Data.Skip(0x08).Take(4).ToArray());
            for (int i = 0x0C; i < HeaderEndPointer; i += 0x08)
            {
                SectionOffsetsAndCounts.Add(new() { Offset = BitConverter.ToInt32(Data.Skip(i).Take(4).ToArray()), ItemCount = BitConverter.ToInt32(Data.Skip(i + 4).Take(4).ToArray()) });
            }

            Settings = new(Data.Skip(SectionOffsetsAndCounts[0].Offset).Take(0xBC));
            SectionOffsetsAndCounts[0].Name = "SETTINGS";

            SectionOffsetsAndCounts[1].Name = "UNKNOWNSECTION2";
            for (int i = 0; i < SectionOffsetsAndCounts[1].ItemCount; i++)
            {
                UnknownMapObject2s.Add(new(Data.Skip(SectionOffsetsAndCounts[1].Offset + i * 0x48)));
            }

            SectionOffsetsAndCounts[2].Name = "UNKNOWNSECTION3";
            for (int i = 0; i < SectionOffsetsAndCounts[2].ItemCount; i++)
            {
                UnknownMapObject3s.Add(new(Data.Skip(SectionOffsetsAndCounts[2].Offset + i * 0x0C)));
            }

            SectionOffsetsAndCounts[3].Name = "INTERACTABLEOBJECTS";
            for (int i = 0; i < SectionOffsetsAndCounts[3].ItemCount; i++)
            {
                InteractableObjects.Add(new(Data, SectionOffsetsAndCounts[3].Offset + i * 0x10));
            }

            int currentPathingByte = 0;
            byte[] pathingBytes = Data.Skip(SectionOffsetsAndCounts[4].Offset).Take(SectionOffsetsAndCounts[4].ItemCount).ToArray();
            SectionOffsetsAndCounts[4].Name = "PATHING_MAP";
            if (Settings.SlgMode)
            {
                PathingMap = new byte[Settings.MapWidth][];
                for (int x = 0; x < Settings.MapWidth; x++)
                {
                    PathingMap[x] = new byte[Settings.MapHeight];

                    for (int y = 0; y < Settings.MapHeight; y++)
                    {
                        PathingMap[x][y] = pathingBytes[currentPathingByte++];
                    }
                }
            }
            else
            {
                PathingMap = new byte[Settings.MapHeight][];
                for (int y = 0; y < Settings.MapHeight; y++)
                {
                    PathingMap[y] = new byte[Settings.MapWidth];

                    for (int x = 0; x < Settings.MapWidth; x++)
                    {
                        PathingMap[y][x] = pathingBytes[currentPathingByte++];
                    }
                }
            }
        }

        /// <summary>
        /// Generates an image of the overall map and (depending on the map) the map's background image
        /// </summary>
        /// <param name="grp">ArchiveFile object for grp.bin</param>
        /// <param name="maxLayoutIndex">(Optional) The maximum layout index to render up to (used to exclude layers not seen in-game when rendering previews)</param>
        /// <param name="grpReplacement">(Optional) A replacement graphics file for one of the map's textures (used to generate animation frames)</param>
        /// <param name="replacementIndex">(Optional) The index of the graphics file to replace with grpReplacement</param>
        /// <returns>A tuple of an SKBitmap of the rendered map and an SKBitmap of the map's background image (null if there is no BG image)</returns>
        public (SKBitmap mapBitmap, SKBitmap bgBitmap) GetMapImages(ArchiveFile<GraphicsFile> grp, int maxLayoutIndex = -1, GraphicsFile grpReplacement = null, int replacementIndex = 1)
        {
            List<GraphicsFile> textures = Settings.TextureFileIndices.Select(i => grp.GetFileByIndex(i)).ToList();
            if (grpReplacement is not null)
            {
                textures[replacementIndex] = grpReplacement;
            }
            GraphicsFile layout = grp.GetFileByIndex(Settings.LayoutFileIndex);

            int mapEndIndex = maxLayoutIndex >= 0 ? maxLayoutIndex : (Settings.ScrollingBgLayoutStartIndex == 0 ? layout.Length : Settings.ScrollingBgLayoutStartIndex);

            SKBitmap mapBitmap = layout.GetLayout(textures, Settings.LayoutBgLayerStartIndex, mapEndIndex, false, true).bitmap;
            SKBitmap bgBitmap = null;

            if (Settings.ScrollingBgLayoutStartIndex != 0 || Settings.ScrollingBgLayoutEndIndex != 0)
            {
                bgBitmap = layout.GetLayout(textures, Settings.ScrollingBgLayoutStartIndex, Settings.ScrollingBgLayoutEndIndex - Settings.ScrollingBgLayoutStartIndex + 1, false, true).bitmap;
            }

            return (mapBitmap, bgBitmap);
        }

        /// <summary>
        /// Generates an image of the overall map and (depending on the map) the map's background image; used for debugging primarily
        /// </summary>
        /// <param name="grp">ArchiveFile object for grp.bin</param>
        /// <param name="start">Starting layer to render on the map's layout</param>
        /// <param name="length">Number of layers to render on the map's layout</param>
        /// <returns>An SKBitmap of the rendered map</returns>
        public SKBitmap GetMapImages(ArchiveFile<GraphicsFile> grp, int start, int length)
        {
            List<GraphicsFile> textures = Settings.TextureFileIndices.Select(i => grp.GetFileByIndex(i)).ToList();
            GraphicsFile layout = grp.GetFileByIndex(Settings.LayoutFileIndex);

            return layout.GetLayout(textures, start, length, false, true).bitmap;
        }

        /// <summary>
        /// Generates an image of the pathing/walkability map
        /// </summary>
        /// <returns>An image of the pathing/walkability map for the map</returns>
        public SKBitmap GetPathingImage()
        {
            SKBitmap pathingImage;
            pathingImage = new((Settings.MapWidth + Settings.MapHeight) * 10, (Settings.MapWidth + Settings.MapHeight) * 10);
            SKCanvas canvas = new(pathingImage);

            SKPathEffect rotatePathEffect = SKPathEffect.Create1DPath(
                SKPath.ParseSvgPathData("M -5 0 L 0 -5, 5 0, 0 5 Z"), 20, 0, SKPath1DPathEffectStyle.Rotate);
            
            if (Settings.SlgMode)
            {
                for (int x = 0; x < PathingMap.Length; x++)
                {
                    for (int y = 0; y < PathingMap[x].Length; y++)
                    {
                        canvas.DrawRect(((Settings.MapWidth + Settings.MapHeight) * 10 / 2) - (x * 5) + (y * 5), (y * 5) + (x * 5) + 5, 5, 5, GetPathingSpacePaint(x, y, rotatePathEffect));
                        canvas.Flush();
                    }
                }
            }
            else
            {
                for (int y = 0; y < Settings.MapHeight; y++)
                {
                    for (int x = 0; x < Settings.MapWidth; x++)
                    {
                        canvas.DrawRect(((Settings.MapWidth + Settings.MapHeight) * 10 / 2) + (x * 5) - (y * 5), (y * 5) + (x * 5) + 5, 5, 5, GetPathingSpacePaint(y, x, rotatePathEffect));
                        canvas.Flush();
                    }
                }
            }

            return pathingImage;
        }

        /// <summary>
        /// Generates an image of the map's background gradient
        /// </summary>
        /// <returns>A 64x64 image of the map's background gradient</returns>
        public SKBitmap GetBackgroundGradient()
        {
            SKBitmap bgGradient = new(64, 64);
            SKCanvas canvas = new(bgGradient);
            SKRect rect = new(0, 0, 64, 64);

            using SKPaint paint = new();
            paint.Shader = SKShader.CreateLinearGradient(
                new SKPoint((rect.Left + rect.Right) / 2, rect.Top),
                new SKPoint((rect.Left + rect.Right) / 2, rect.Bottom),
                [Settings.TopGradient, Settings.BottomGradient],
                [0.0f, 1.0f],
                SKShaderTileMode.Repeat);

            canvas.DrawRect(rect, paint);
            canvas.Flush();

            return bgGradient;
        }

        private SKPaint GetPathingSpacePaint(int x, int y, SKPathEffect rotatePathEffect)
        {
            return PathingMap[x][y] switch
            {
                1 => new() { Color = SKColors.Gray, PathEffect = rotatePathEffect }, // walkable
                2 => new() { Color = SKColors.Teal, PathEffect = rotatePathEffect }, // spawnable
                _ => new() { Color = SKColors.Black, PathEffect = rotatePathEffect }, // unwalkable
            };
        }

        /// <inheritdoc/>
        public override string GetSource(Dictionary<string, IncludeEntry[]> includes)
        {
            StringBuilder sb = new();

            sb.AppendLine(string.Join("\n", includes.Select(i => $".include {i.Key}.INC")));

            sb.AppendLine($".word {NumSections}");
            sb.AppendLine(".word ENDPOINTERS");
            sb.AppendLine(".word FILE_START");

            foreach (var section in SectionOffsetsAndCounts)
            {
                sb.AppendLine($".word {section.Name}");
                sb.AppendLine($".word {section.ItemCount}");
            }

            sb.AppendLine();
            sb.AppendLine("FILE_START:");

            sb.AppendLine($"{SectionOffsetsAndCounts[4].Name}:");
            if (Settings.SlgMode)
            {
                for (int x = 0; x < Settings.MapWidth; x++)
                {
                    for (int y = 0; y < Settings.MapHeight; y++)
                    {
                        sb.AppendLine($"   .byte {PathingMap[x][y]}");
                    }
                }
            }
            else
            {
                for (int y = 0; y < Settings.MapHeight; y++)
                {
                    for (int x = 0; x < Settings.MapWidth; x++)
                    {
                        sb.AppendLine($"   .byte {PathingMap[y][x]}");
                    }
                }
            }
            sb.AppendLine($".skip {4 - PathingMap.Length * PathingMap[0].Length % 4}");
            sb.AppendLine();

            sb.AppendLine($"{SectionOffsetsAndCounts[1].Name}:");
            foreach (UnknownMapObject2 mapObject2 in UnknownMapObject2s)
            {
                sb.AppendLine(mapObject2.GetAsm(3));
            }
            sb.AppendLine();

            sb.AppendLine($"{SectionOffsetsAndCounts[2].Name}:");
            foreach (UnknownMapObject3 mapObject3 in UnknownMapObject3s)
            {
                sb.AppendLine(mapObject3.GetAsm(3));
            }
            sb.AppendLine();

            sb.AppendLine($"{SectionOffsetsAndCounts[3].Name}:");
            int currentPointer = 0;
            foreach (InteractableObject interactableObject in InteractableObjects)
            {
                sb.AppendLine(interactableObject.GetAsm(3, ref currentPointer));
            }
            foreach (InteractableObject interactableObject in InteractableObjects)
            {
                if (interactableObject.ObjectId != 0)
                {
                    sb.AppendLine($"{Helpers.Indent(3)}INTERACTABLEOBJECT{interactableObject.ObjectId}: .string \"{Helpers.EscapeShiftJIS(interactableObject.ObjectName)}\"");
                    sb.AppendLine($".skip {4 - (Encoding.GetEncoding("Shift-JIS").GetBytes(interactableObject.ObjectName).Length + 1) % 4}");
                }
            }
            sb.AppendLine();

            sb.AppendLine($"{SectionOffsetsAndCounts[0].Name}:");
            sb.AppendLine(Settings.GetAsm(3, SectionOffsetsAndCounts, ref currentPointer));
            sb.AppendLine();

            sb.AppendLine($"ENDPOINTERS: .word {currentPointer}");
            for (int i = 0; i < currentPointer; i++)
            {
                sb.AppendLine($"{Helpers.Indent(3)}.word POINTER{i:D2}");
            }

            return sb.ToString();
        }
    }

    /// <summary>
    /// A representation of the settings section of a map file
    /// </summary>
    public class MapFileSettings
    {
        /// <summary>
        /// "Singularity Mode", true indicates that this map is a puzzle phase map
        /// </summary>
        public bool SlgMode { get; set; }
        /// <summary>
        /// The width of the map in tiles
        /// </summary>
        public int MapWidth { get; set; }
        /// <summary>
        /// The height of the map in tiles
        /// </summary>
        public int MapHeight { get; set; }
        /// <summary>
        /// A list of grp.bin indices of the texture files used in the map layout
        /// </summary>
        public List<short> TextureFileIndices { get; set; } = [];
        /// <summary>
        /// The grp.bin index of the layout file which defines the map
        /// </summary>
        public short LayoutFileIndex { get; set; }
        /// <summary>
        /// The layout index where the background layer begins
        /// </summary>
        public int LayoutBgLayerStartIndex { get; set; }
        /// <summary>
        /// The number of special "definitions" (non-textures) at the beginning of the background layer
        /// </summary>
        public int NumBgLayerDefinitions { get; set; }
        /// <summary>
        /// The layout index where the background layer ends
        /// </summary>
        public int LayoutBgLayerEndIndex { get; set; }
        /// <summary>
        /// The layout index where the occlusion layer (where all entries fully occlude objects) begins
        /// </summary>
        public int LayoutOcclusionLayerStartIndex { get; set; }
        /// <summary>
        /// The layout index where the occlusion layer ends
        /// </summary>
        public int LayoutOcclusionLayerEndIndex { get; set; }
        /// <summary>
        /// Unknown
        /// </summary>
        public int LayoutBoundsIndex { get; set; }
        /// <summary>
        /// The index of the layout layer at which the scrolling background image starts
        /// </summary>
        public int ScrollingBgLayoutStartIndex { get; set; }
        /// <summary>
        /// The index of the layout layer at which the scrolling background image ends
        /// </summary>
        public int ScrollingBgLayoutEndIndex { get; set; }
        /// <summary>
        /// An integer defining the mode by which the scrolling background is flipped/rotated (not yet fully understood)
        /// </summary>
        public int TransformMode { get; set; }
        /// <summary>
        /// The color of the top of the background gradient
        /// </summary>
        public SKColor TopGradient { get; set; }
        /// <summary>
        /// The color of the bottom of the background gradient
        /// </summary>
        public SKColor BottomGradient { get; set; }
        /// <summary>
        /// The layout index where the scrolling background is defined
        /// </summary>
        public int ScrollingBgDefinitionLayoutIndex { get; set; }
        /// <summary>
        /// Unknown
        /// </summary>
        public int IntroCameraTruckingDefsStartIndex { get; set; }
        /// <summary>
        /// Unknown
        /// </summary>
        public int IntroCameraTruckingDefsEndIndex { get; set; }
        /// <summary>
        /// The starting position (in terms of tiles) of the player character
        /// </summary>
        public (int x, int y) StartingPosition { get; set; }
        /// <summary>
        /// If the map uses color animation, this is the grp.bin index of that animation file
        /// </summary>
        public int ColorAnimationFileIndex { get; set; }
        /// <summary>
        /// If the map uses palette animation, this is the grp.bin index of that animation file
        /// </summary>
        public int PaletteAnimationFileIndex { get; set; }
        /// <summary>
        /// Unknown
        /// </summary>
        public int Unknown5C { get; set; }
        internal int Unknown2Count { get; set; }
        internal int Unknown2SectionPointer { get; set; }
        internal int InteractableObjectsCount { get; set; }
        internal int InteractableObjectsSectionPointer { get; set; }
        internal int WalkabilityMapPointer { get; set; }
        internal int ObjectsCount { get; set; }
        internal int ObjectsSectionPointer { get; set; }

        /// <summary>
        /// Constructs map file settings
        /// </summary>
        /// <param name="data">The data from the map file</param>
        public MapFileSettings(IEnumerable<byte> data)
        {
            SlgMode = BitConverter.ToInt32(data.Take(4).ToArray()) > 0;
            MapWidth = BitConverter.ToInt32(data.Skip(0x04).Take(4).ToArray());
            MapHeight = BitConverter.ToInt32(data.Skip(0x08).Take(4).ToArray());
            TextureFileIndices.Add(BitConverter.ToInt16(data.Skip(0x0C).Take(2).ToArray()));
            TextureFileIndices.Add(BitConverter.ToInt16(data.Skip(0x10).Take(2).ToArray()));
            TextureFileIndices.Add(BitConverter.ToInt16(data.Skip(0x0E).Take(2).ToArray()));
            LayoutFileIndex = BitConverter.ToInt16(data.Skip(0x12).Take(2).ToArray());
            LayoutBgLayerStartIndex = BitConverter.ToInt32(data.Skip(0x14).Take(4).ToArray());
            NumBgLayerDefinitions = BitConverter.ToInt32(data.Skip(0x18).Take(4).ToArray());
            LayoutBgLayerEndIndex = BitConverter.ToInt32(data.Skip(0x1C).Take(4).ToArray());
            LayoutOcclusionLayerStartIndex = BitConverter.ToInt32(data.Skip(0x20).Take(4).ToArray());
            LayoutOcclusionLayerEndIndex = BitConverter.ToInt32(data.Skip(0x24).Take(4).ToArray());
            LayoutBoundsIndex = BitConverter.ToInt32(data.Skip(0x28).Take(4).ToArray());
            ScrollingBgLayoutStartIndex = BitConverter.ToInt32(data.Skip(0x2C).Take(4).ToArray());
            ScrollingBgLayoutEndIndex = BitConverter.ToInt32(data.Skip(0x30).Take(4).ToArray());
            TransformMode = BitConverter.ToInt32(data.Skip(0x34).Take(4).ToArray());
            TopGradient = new(data.ElementAt(0x3A), data.ElementAt(0x39), data.ElementAt(0x38));
            BottomGradient = new(data.ElementAt(0x3E), data.ElementAt(0x3D), data.ElementAt(0x3C));
            ScrollingBgDefinitionLayoutIndex = BitConverter.ToInt32(data.Skip(0x40).Take(4).ToArray());
            IntroCameraTruckingDefsStartIndex = BitConverter.ToInt32(data.Skip(0x44).Take(4).ToArray());
            IntroCameraTruckingDefsEndIndex = BitConverter.ToInt32(data.Skip(0x48).Take(4).ToArray());
            StartingPosition = (BitConverter.ToInt32(data.Skip(0x4C).Take(4).ToArray()), BitConverter.ToInt32(data.Skip(0x50).Take(4).ToArray()));
            ColorAnimationFileIndex = BitConverter.ToInt32(data.Skip(0x54).Take(4).ToArray());
            PaletteAnimationFileIndex = BitConverter.ToInt32(data.Skip(0x58).Take(4).ToArray());
            Unknown5C = BitConverter.ToInt32(data.Skip(0x5C).Take(4).ToArray());
            ObjectsCount = BitConverter.ToInt32(data.Skip(0x60).Take(4).ToArray());
            ObjectsSectionPointer = BitConverter.ToInt32(data.Skip(0x64).Take(4).ToArray());
            InteractableObjectsCount = BitConverter.ToInt32(data.Skip(0x68).Take(4).ToArray());
            InteractableObjectsSectionPointer = BitConverter.ToInt32(data.Skip(0x6C).Take(4).ToArray());
            WalkabilityMapPointer = BitConverter.ToInt32(data.Skip(0x70).Take(4).ToArray());
            Unknown2Count = BitConverter.ToInt32(data.Skip(0x74).Take(4).ToArray());
            Unknown2SectionPointer = BitConverter.ToInt32(data.Skip(0x78).Take(4).ToArray());
        }

        internal string GetAsm(int indent, List<DataFileSection> sections, ref int currentPointer)
        {
            StringBuilder sb = new();
            sb.AppendLine($"{Helpers.Indent(indent)}.word {(SlgMode ? 1 : 0)}");
            sb.AppendLine($"{Helpers.Indent(indent)}.word {MapWidth}");
            sb.AppendLine($"{Helpers.Indent(indent)}.word {MapHeight}");
            sb.AppendLine($"{Helpers.Indent(indent)}.short {TextureFileIndices[0]}");
            sb.AppendLine($"{Helpers.Indent(indent)}.short {TextureFileIndices[2]}");
            sb.AppendLine($"{Helpers.Indent(indent)}.short {TextureFileIndices[1]}");
            sb.AppendLine($"{Helpers.Indent(indent)}.short {LayoutFileIndex}");
            sb.AppendLine($"{Helpers.Indent(indent)}.word {LayoutBgLayerStartIndex}");
            sb.AppendLine($"{Helpers.Indent(indent)}.word {NumBgLayerDefinitions}");
            sb.AppendLine($"{Helpers.Indent(indent)}.word {LayoutBgLayerEndIndex}");
            sb.AppendLine($"{Helpers.Indent(indent)}.word {LayoutOcclusionLayerStartIndex}");
            sb.AppendLine($"{Helpers.Indent(indent)}.word {LayoutOcclusionLayerEndIndex}");
            sb.AppendLine($"{Helpers.Indent(indent)}.word {LayoutBoundsIndex}");
            sb.AppendLine($"{Helpers.Indent(indent)}.word {ScrollingBgLayoutStartIndex}");
            sb.AppendLine($"{Helpers.Indent(indent)}.word {ScrollingBgLayoutEndIndex}");
            sb.AppendLine($"{Helpers.Indent(indent)}.word {TransformMode}");
            sb.AppendLine($"{Helpers.Indent(indent)}.word 0x{TopGradient.Red << 16 | TopGradient.Green << 8 | TopGradient.Blue:X6}");
            sb.AppendLine($"{Helpers.Indent(indent)}.word 0x{BottomGradient.Red << 16 | BottomGradient.Green << 8 | BottomGradient.Blue:X6}");
            sb.AppendLine($"{Helpers.Indent(indent)}.word {ScrollingBgDefinitionLayoutIndex}");
            sb.AppendLine($"{Helpers.Indent(indent)}.word {IntroCameraTruckingDefsStartIndex}");
            sb.AppendLine($"{Helpers.Indent(indent)}.word {IntroCameraTruckingDefsEndIndex}");
            sb.AppendLine($"{Helpers.Indent(indent)}.word {StartingPosition.x}");
            sb.AppendLine($"{Helpers.Indent(indent)}.word {StartingPosition.y}");
            sb.AppendLine($"{Helpers.Indent(indent)}.word {ColorAnimationFileIndex}");
            sb.AppendLine($"{Helpers.Indent(indent)}.word {PaletteAnimationFileIndex}");
            sb.AppendLine($"{Helpers.Indent(indent)}.word {Unknown5C}");
            sb.AppendLine($"{Helpers.Indent(indent)}.word {sections[2].ItemCount - 1}");
            sb.AppendLine($"{Helpers.Indent(indent)}POINTER{currentPointer++:D2}: .word {sections[2].Name}");
            sb.AppendLine($"{Helpers.Indent(indent)}.word {sections[3].ItemCount - 1}");
            sb.AppendLine($"{Helpers.Indent(indent)}POINTER{currentPointer++:D2}: .word {sections[3].Name}");
            sb.AppendLine($"{Helpers.Indent(indent)}POINTER{currentPointer++:D2}: .word {sections[4].Name}");
            sb.AppendLine($"{Helpers.Indent(indent)}.word {sections[1].ItemCount - 1}");
            sb.AppendLine($"{Helpers.Indent(indent)}POINTER{currentPointer++:D2}: .word {sections[1].Name}");
            sb.AppendLine($"{Helpers.Indent(indent)}.skip 0x40");
            return sb.ToString();
        }
    }

    /// <summary>
    /// Unknown
    /// </summary>
    public class UnknownMapObject2(IEnumerable<byte> data)
    {
        /// <summary>
        /// Unknown
        /// </summary>
        public short UnknownShort1 { get; set; } = BitConverter.ToInt16(data.Take(2).ToArray());
        /// <summary>
        /// Unknown
        /// </summary>
        public short UnknownShort2 { get; set; } = BitConverter.ToInt16(data.Skip(2).Take(2).ToArray());

        internal string GetAsm(int indent)
        {
            StringBuilder sb = new();
            sb.AppendLine($"{Helpers.Indent(indent)}.short {UnknownShort1}");
            sb.AppendLine($"{Helpers.Indent(indent + 3)}.short {UnknownShort2}");
            sb.AppendLine($"{Helpers.Indent(indent + 3)}.skip 0x44");
            return sb.ToString();
        }
    }

    /// <summary>
    /// Unknown
    /// </summary>
    public class UnknownMapObject3(IEnumerable<byte> data)
    {
        /// <summary>
        /// Unknown
        /// </summary>
        public short UnknownShort1 { get; set; } = BitConverter.ToInt16(data.Take(2).ToArray());
        /// <summary>
        /// Unknown
        /// </summary>
        public short UnknownShort2 { get; set; } = BitConverter.ToInt16(data.Skip(2).Take(2).ToArray());
        /// <summary>
        /// Unknown
        /// </summary>
        public short UnknownShort3 { get; set; } = BitConverter.ToInt16(data.Skip(4).Take(2).ToArray());

        internal string GetAsm(int indent)
        {
            StringBuilder sb = new();
            sb.AppendLine($"{Helpers.Indent(indent)}.short {UnknownShort1}");
            sb.AppendLine($"{Helpers.Indent(indent + 3)}.short {UnknownShort2}");
            sb.AppendLine($"{Helpers.Indent(indent + 3)}.short {UnknownShort3}");
            sb.AppendLine($"{Helpers.Indent(indent + 3)}.skip 6");
            return sb.ToString();
        }
    }

    /// <summary>
    /// Representation of an interactable object on a map
    /// </summary>
    /// <param name="data">The interactable object entry data from the map file</param>
    /// <param name="offset">The starting offset of the interactable object data in the file</param>
    public class InteractableObject(IEnumerable<byte> data, int offset)
    {
        /// <summary>
        /// The X-position of the object (in terms of tiles) on the map
        /// </summary>
        public short ObjectX { get; set; } = BitConverter.ToInt16(data.Skip(offset).Take(2).ToArray());
        /// <summary>
        /// The Y-position of the object (in terms of tiles) on the map
        /// </summary>
        public short ObjectY { get; set; } = BitConverter.ToInt16(data.Skip(offset + 2).Take(2).ToArray());
        /// <summary>
        /// The ID of the object
        /// </summary>
        public int ObjectId { get; set; } = BitConverter.ToInt32(data.Skip(offset + 4).Take(4).ToArray());
        /// <summary>
        /// The name of the object
        /// </summary>
        public string ObjectName { get; set; } = Encoding.GetEncoding("Shift-JIS").GetString(data.Skip(BitConverter.ToInt32(data.Skip(offset + 8).Take(4).ToArray())).TakeWhile(b => b != 0x00).ToArray());

        internal string GetAsm(int indent, ref int currentPointer)
        {
            StringBuilder sb = new();
            sb.AppendLine($"{Helpers.Indent(indent)}.short {ObjectX}");
            sb.AppendLine($"{Helpers.Indent(indent)}.short {ObjectY}");
            sb.AppendLine($"{Helpers.Indent(indent)}.word {ObjectId}");
            if (ObjectId > 0)
            {
                sb.AppendLine($"{Helpers.Indent(indent)}POINTER{currentPointer++:D2}: .int INTERACTABLEOBJECT{ObjectId}");
            }
            else
            {
                sb.AppendLine($"{Helpers.Indent(indent)}.word 0");
            }
            sb.AppendLine($"{Helpers.Indent(indent)}.skip 4");
            sb.AppendLine();
            return sb.ToString();
        }
    }
}
