using HaruhiChokuretsuLib.Util;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HaruhiChokuretsuLib.Archive.Data
{
    public class MapFile : DataFile
    {
        public int NumSections { get; set; }
        public int EndPointersOffset { get; set; }
        public int HeaderEndPointer { get; set; }
        public List<DataFileSection> SectionOffsetsAndCounts { get; set; } = new();

        public MapFileSettings Settings { get; set; }
        public List<UnknownMapObject2> UnknownMapObject2s { get; set; } = new();
        public List<UnknownMapObject3> UnknownMapObject3s { get; set; } = new();
        public List<InteractableObject> InteractableObjects { get; set; } = new();
        public byte[][] PathingMap { get; set; }

        public override void Initialize(byte[] decompressedData, int offset, ILogger log)
        {
            _log = log;
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            Offset = offset;
            Data = decompressedData.ToList();

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
            if (Settings.YOriented)
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

        public (SKBitmap mapBitmap, SKBitmap bgBitmap) GetMapImages(ArchiveFile<GraphicsFile> grp, int maxLayoutIndex = -1, GraphicsFile grpReplacement = null, int replacementIndex = 1)
        {
            List<GraphicsFile> textures = Settings.TextureFileIndices.Select(i => grp.Files.First(f => f.Index == i)).ToList();
            if (grpReplacement is not null)
            {
                textures[replacementIndex] = grpReplacement;
            }
            GraphicsFile layout = grp.Files.First(f => f.Index == Settings.LayoutFileIndex);

            int mapEndIndex = maxLayoutIndex >= 0 ? maxLayoutIndex : (Settings.BackgroundLayoutStartIndex == 0 ? layout.Length : Settings.BackgroundLayoutStartIndex);

            SKBitmap mapBitmap = layout.GetLayout(textures, Settings.ForegroundLayoutStartIndex, mapEndIndex, false, true).bitmap;
            SKBitmap bgBitmap = null;

            if (Settings.BackgroundLayoutStartIndex != 0 || Settings.BackgroundLayoutEndIndex != 0)
            {
                bgBitmap = layout.GetLayout(textures, Settings.BackgroundLayoutStartIndex, Settings.BackgroundLayoutEndIndex - Settings.BackgroundLayoutStartIndex + 1, false, true).bitmap;
            }

            return (mapBitmap, bgBitmap);
        }

        public SKBitmap GetMapImages(ArchiveFile<GraphicsFile> grp, int start, int length)
        {
            List<GraphicsFile> textures = Settings.TextureFileIndices.Select(i => grp.Files.First(f => f.Index == i)).ToList();
            GraphicsFile layout = grp.Files.First(f => f.Index == Settings.LayoutFileIndex);

            return layout.GetLayout(textures, start, length, false, true).bitmap;
        }

        public SKBitmap GetPathingImage()
        {
            SKBitmap pathingImage;
            pathingImage = new((Settings.MapWidth + Settings.MapHeight) * 10, (Settings.MapWidth + Settings.MapHeight) * 10);
            SKCanvas canvas = new(pathingImage);

            SKPathEffect rotatePathEffect = SKPathEffect.Create1DPath(
                SKPath.ParseSvgPathData("M -5 0 L 0 -5, 5 0, 0 5 Z"), 20, 0, SKPath1DPathEffectStyle.Rotate);
            
            if (Settings.YOriented)
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

        public SKBitmap GetBackgroundGradient()
        {
            SKBitmap bgGradient = new(64, 64);
            SKCanvas canvas = new(bgGradient);
            SKRect rect = new(0, 0, 64, 64);

            using SKPaint paint = new();
            paint.Shader = SKShader.CreateLinearGradient(
                new SKPoint((rect.Left + rect.Right) / 2, rect.Top),
                new SKPoint((rect.Left + rect.Right) / 2, rect.Bottom),
                new SKColor[] { Settings.TopGradient, Settings.BottomGradient },
                new float[] { 0, 1 },
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
            if (Settings.YOriented)
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

    public class MapFileSettings
    {
        public bool YOriented { get; set; }
        public int MapWidth { get; set; }
        public int MapHeight { get; set; }
        public List<short> TextureFileIndices { get; set; } = new();
        public short LayoutFileIndex { get; set; }
        public int ForegroundLayoutStartIndex { get; set; }
        public int Unknown18 { get; set; }
        public int UnknownLayoutIndex1C { get; set; }
        public int UnknownLayoutIndex20 { get; set; }
        public int UnknownLayoutIndex24 { get; set; }
        public int Unknown28 { get; set; }
        public int BackgroundLayoutStartIndex { get; set; }
        public int BackgroundLayoutEndIndex { get; set; }
        public int Unknown34 { get; set; }
        public SKColor TopGradient { get; set; }
        public SKColor BottomGradient { get; set; }
        public int UnknownLayoutIndex40 { get; set; }
        public int Unknown44 { get; set; }
        public int Unknown48 { get; set; }
        public (int x, int y) StartingPosition { get; set; }
        public int ColorAnimationFileIndex { get; set; }
        public int PaletteAnimationFileIndex { get; set; }
        public int Unknown5C { get; set; }
        public int Unknown2Count { get; set; }
        public int Unknown2SectionPointer { get; set; }
        public int InteractableObjectsCount { get; set; }
        public int InteractableObjectsSectionPointer { get; set; }
        public int WalkabilityMapPointer { get; set; }
        public int Unknown3Count { get; set; }
        public int Unknown3SectionPointer { get; set; }

        public MapFileSettings(IEnumerable<byte> data)
        {
            YOriented = BitConverter.ToInt32(data.Take(4).ToArray()) > 0;
            MapWidth = BitConverter.ToInt32(data.Skip(0x04).Take(4).ToArray());
            MapHeight = BitConverter.ToInt32(data.Skip(0x08).Take(4).ToArray());
            TextureFileIndices.Add(BitConverter.ToInt16(data.Skip(0x0C).Take(2).ToArray()));
            TextureFileIndices.Add(BitConverter.ToInt16(data.Skip(0x10).Take(2).ToArray()));
            TextureFileIndices.Add(BitConverter.ToInt16(data.Skip(0x0E).Take(2).ToArray()));
            LayoutFileIndex = BitConverter.ToInt16(data.Skip(0x12).Take(2).ToArray());
            ForegroundLayoutStartIndex = BitConverter.ToInt32(data.Skip(0x14).Take(4).ToArray());
            Unknown18 = BitConverter.ToInt32(data.Skip(0x18).Take(4).ToArray());
            UnknownLayoutIndex1C = BitConverter.ToInt32(data.Skip(0x1C).Take(4).ToArray());
            UnknownLayoutIndex20 = BitConverter.ToInt32(data.Skip(0x20).Take(4).ToArray());
            UnknownLayoutIndex24 = BitConverter.ToInt32(data.Skip(0x24).Take(4).ToArray());
            Unknown28 = BitConverter.ToInt32(data.Skip(0x28).Take(4).ToArray());
            BackgroundLayoutStartIndex = BitConverter.ToInt32(data.Skip(0x2C).Take(4).ToArray());
            BackgroundLayoutEndIndex = BitConverter.ToInt32(data.Skip(0x30).Take(4).ToArray());
            Unknown34 = BitConverter.ToInt32(data.Skip(0x34).Take(4).ToArray());
            TopGradient = new(data.ElementAt(0x3A), data.ElementAt(0x39), data.ElementAt(0x38));
            BottomGradient = new(data.ElementAt(0x3E), data.ElementAt(0x3D), data.ElementAt(0x3C));
            UnknownLayoutIndex40 = BitConverter.ToInt32(data.Skip(0x40).Take(4).ToArray());
            Unknown44 = BitConverter.ToInt32(data.Skip(0x44).Take(4).ToArray());
            Unknown48 = BitConverter.ToInt32(data.Skip(0x48).Take(4).ToArray());
            StartingPosition = (BitConverter.ToInt32(data.Skip(0x4C).Take(4).ToArray()), BitConverter.ToInt32(data.Skip(0x50).Take(4).ToArray()));
            ColorAnimationFileIndex = BitConverter.ToInt32(data.Skip(0x54).Take(4).ToArray());
            PaletteAnimationFileIndex = BitConverter.ToInt32(data.Skip(0x58).Take(4).ToArray());
            Unknown5C = BitConverter.ToInt32(data.Skip(0x5C).Take(4).ToArray());
            Unknown3Count = BitConverter.ToInt32(data.Skip(0x60).Take(4).ToArray());
            Unknown3SectionPointer = BitConverter.ToInt32(data.Skip(0x64).Take(4).ToArray());
            InteractableObjectsCount = BitConverter.ToInt32(data.Skip(0x68).Take(4).ToArray());
            InteractableObjectsSectionPointer = BitConverter.ToInt32(data.Skip(0x6C).Take(4).ToArray());
            WalkabilityMapPointer = BitConverter.ToInt32(data.Skip(0x70).Take(4).ToArray());
            Unknown2Count = BitConverter.ToInt32(data.Skip(0x74).Take(4).ToArray());
            Unknown2SectionPointer = BitConverter.ToInt32(data.Skip(0x78).Take(4).ToArray());
        }

        public string GetAsm(int indent, List<DataFileSection> sections, ref int currentPointer)
        {
            StringBuilder sb = new();
            sb.AppendLine($"{Helpers.Indent(indent)}.word {(YOriented ? 1 : 0)}");
            sb.AppendLine($"{Helpers.Indent(indent)}.word {MapWidth}");
            sb.AppendLine($"{Helpers.Indent(indent)}.word {MapHeight}");
            sb.AppendLine($"{Helpers.Indent(indent)}.short {TextureFileIndices[0]}");
            sb.AppendLine($"{Helpers.Indent(indent)}.short {TextureFileIndices[2]}");
            sb.AppendLine($"{Helpers.Indent(indent)}.short {TextureFileIndices[1]}");
            sb.AppendLine($"{Helpers.Indent(indent)}.short {LayoutFileIndex}");
            sb.AppendLine($"{Helpers.Indent(indent)}.word {ForegroundLayoutStartIndex}");
            sb.AppendLine($"{Helpers.Indent(indent)}.word {Unknown18}");
            sb.AppendLine($"{Helpers.Indent(indent)}.word {UnknownLayoutIndex1C}");
            sb.AppendLine($"{Helpers.Indent(indent)}.word {UnknownLayoutIndex20}");
            sb.AppendLine($"{Helpers.Indent(indent)}.word {UnknownLayoutIndex24}");
            sb.AppendLine($"{Helpers.Indent(indent)}.word {Unknown28}");
            sb.AppendLine($"{Helpers.Indent(indent)}.word {BackgroundLayoutStartIndex}");
            sb.AppendLine($"{Helpers.Indent(indent)}.word {BackgroundLayoutEndIndex}");
            sb.AppendLine($"{Helpers.Indent(indent)}.word {Unknown34}");
            sb.AppendLine($"{Helpers.Indent(indent)}.word 0x{TopGradient.Red << 16 | TopGradient.Green << 8 | TopGradient.Blue:X6}");
            sb.AppendLine($"{Helpers.Indent(indent)}.word 0x{BottomGradient.Red << 16 | BottomGradient.Green << 8 | BottomGradient.Blue:X6}");
            sb.AppendLine($"{Helpers.Indent(indent)}.word {UnknownLayoutIndex40}");
            sb.AppendLine($"{Helpers.Indent(indent)}.word {Unknown44}");
            sb.AppendLine($"{Helpers.Indent(indent)}.word {Unknown48}");
            sb.AppendLine($"{Helpers.Indent(indent)}.word {StartingPosition.x}");
            sb.AppendLine($"{Helpers.Indent(indent)}.word {StartingPosition.y}");
            sb.AppendLine($"{Helpers.Indent(indent)}.word {ColorAnimationFileIndex}");
            sb.AppendLine($"{Helpers.Indent(indent)}.word {PaletteAnimationFileIndex}");
            sb.AppendLine($"{Helpers.Indent(indent)}.word {Unknown5C}");
            sb.AppendLine($"{Helpers.Indent(indent)}.word {Unknown3Count}");
            sb.AppendLine($"{Helpers.Indent(indent)}POINTER{currentPointer++:D2}: .word {sections[2].Name}");
            sb.AppendLine($"{Helpers.Indent(indent)}.word {InteractableObjectsCount}");
            sb.AppendLine($"{Helpers.Indent(indent)}POINTER{currentPointer++:D2}: .word {sections[3].Name}");
            sb.AppendLine($"{Helpers.Indent(indent)}POINTER{currentPointer++:D2}: .word {sections[4].Name}");
            sb.AppendLine($"{Helpers.Indent(indent)}.word {Unknown2Count}");
            sb.AppendLine($"{Helpers.Indent(indent)}POINTER{currentPointer++:D2}: .word {sections[1].Name}");
            sb.AppendLine($"{Helpers.Indent(indent)}.skip 0x40");
            return sb.ToString();
        }
    }

    public class UnknownMapObject2
    {
        public short UnknownShort1 { get; set; }
        public short UnknownShort2 { get; set; }

        public UnknownMapObject2(IEnumerable<byte> data)
        {
            UnknownShort1 = BitConverter.ToInt16(data.Take(2).ToArray());
            UnknownShort2 = BitConverter.ToInt16(data.Skip(2).Take(2).ToArray());
        }

        public string GetAsm(int indent)
        {
            StringBuilder sb = new();
            sb.AppendLine($"{Helpers.Indent(indent)}.short {UnknownShort1}");
            sb.AppendLine($"{Helpers.Indent(indent + 3)}.short {UnknownShort2}");
            sb.AppendLine($"{Helpers.Indent(indent + 3)}.skip 0x44");
            return sb.ToString();
        }
    }

    public class UnknownMapObject3
    {
        public short UnknownShort1 { get; set; }
        public short UnknownShort2 { get; set; }
        public short UnknownShort3 { get; set; }

        public UnknownMapObject3(IEnumerable<byte> data)
        {
            UnknownShort1 = BitConverter.ToInt16(data.Take(2).ToArray());
            UnknownShort2 = BitConverter.ToInt16(data.Skip(2).Take(2).ToArray());
            UnknownShort3 = BitConverter.ToInt16(data.Skip(4).Take(2).ToArray());
        }

        public string GetAsm(int indent)
        {
            StringBuilder sb = new();
            sb.AppendLine($"{Helpers.Indent(indent)}.short {UnknownShort1}");
            sb.AppendLine($"{Helpers.Indent(indent + 3)}.short {UnknownShort2}");
            sb.AppendLine($"{Helpers.Indent(indent + 3)}.short {UnknownShort3}");
            sb.AppendLine($"{Helpers.Indent(indent + 3)}.skip 6");
            return sb.ToString();
        }
    }

    public class InteractableObject
    {
        public short ObjectX { get; set; }
        public short ObjectY { get; set; }
        public int ObjectId { get; set; }
        public string ObjectName { get; set; }

        public InteractableObject(IEnumerable<byte> data, int offset)
        {
            ObjectX = BitConverter.ToInt16(data.Skip(offset).Take(2).ToArray());
            ObjectY = BitConverter.ToInt16(data.Skip(offset + 2).Take(2).ToArray());
            ObjectId = BitConverter.ToInt32(data.Skip(offset+ 4).Take(4).ToArray());
            ObjectName = Encoding.GetEncoding("Shift-JIS").GetString(data.Skip(BitConverter.ToInt32(data.Skip(offset + 8).Take(4).ToArray())).TakeWhile(b => b != 0x00).ToArray());
        }

        public string GetAsm(int indent, ref int currentPointer)
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
