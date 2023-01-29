using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Archive.Graphics;
using HaruhiChokuretsuLib.Util;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HaruhiChokuretsuLib.Archive.Data
{
    public class CharacterDataFile : DataFile
    {
        public List<CharacterSprite> Sprites { get; set; } = new();

        public override void Initialize(byte[] decompressedData, int offset, ILogger log)
        {
            _log = log;

            int numSections = IO.ReadInt(decompressedData, 0);
            if (numSections != 1)
            {
                _log.LogError($"Character data file should only have 1 section; {numSections} specified");
                return;
            }
            int sectionStart = IO.ReadInt(decompressedData, 0x0C);
            int sectionCount = IO.ReadInt(decompressedData, 0x10);

            for (int i = 0; i < sectionCount; i++)
            {
                Sprites.Add(new(decompressedData.Skip(sectionStart + 0x18 * i).Take(0x18)));
            }
        }

        public override string GetSource(Dictionary<string, IncludeEntry[]> includes)
        {
            if (!includes.ContainsKey("GRPBIN"))
            {
                _log.LogError("Includes needs GRPBIN to be present.");
                return null;
            }

            StringBuilder sb = new();

            sb.AppendLine(".include \"GRPBIN.INC\"");
            sb.AppendLine();
            sb.AppendLine($".word 1");
            sb.AppendLine(".word END_POINTERS");
            sb.AppendLine(".word FILE_START");
            sb.AppendLine(".word SPRITE_LIST");
            sb.AppendLine($".word {Sprites.Count}");

            sb.AppendLine();
            sb.AppendLine("FILE_START:");
            sb.AppendLine("SPRITE_LIST:");

            foreach (CharacterSprite sprite in Sprites)
            {
                sb.AppendLine($".word {sprite.SpriteType}");
                sb.AppendLine($".short {(int)sprite.Character}");
                sb.AppendLine($".short {sprite.TextureIndex1}");
                sb.AppendLine($".short {sprite.TextureIndex2}");
                sb.AppendLine($".short {sprite.LayoutIndex}");
                sb.AppendLine($".short {sprite.TextureIndex3}");
                sb.AppendLine($".short {sprite.Padding}");
                sb.AppendLine($".short {sprite.EyeTextureIndex}");
                sb.AppendLine($".short {sprite.MouthTextureIndex}");
                sb.AppendLine($".short {sprite.EyeAnimationIndex}");
                sb.AppendLine($".short {sprite.MouthAnimationIndex}");
            }

            sb.AppendLine("END_POINTERS:");
            sb.AppendLine(".word 0");

            return sb.ToString();
        }
    }

    public class CharacterSprite
    {
        public int SpriteType { get; set; }
        public Speaker Character { get; set; }
        public short TextureIndex1 { get; set; }
        public short TextureIndex2 { get; set; }
        public short LayoutIndex { get; set; }
        public short TextureIndex3 { get; set; }
        public short Padding { get; set; }
        public short EyeTextureIndex { get; set; }
        public short MouthTextureIndex { get; set; }
        public short EyeAnimationIndex { get; set; }
        public short MouthAnimationIndex { get; set; }

        public CharacterSprite(IEnumerable<byte> data)
        {
            SpriteType = IO.ReadInt(data, 0);
            Character = (Speaker)IO.ReadShort(data, 0x04);
            TextureIndex1 = IO.ReadShort(data, 0x06);
            TextureIndex2 = IO.ReadShort(data, 0x08);
            LayoutIndex = IO.ReadShort(data, 0x0A);
            TextureIndex3 = IO.ReadShort(data, 0x0C);
            Padding = IO.ReadShort(data, 0x0E);
            EyeTextureIndex = IO.ReadShort(data, 0x10);
            MouthTextureIndex = IO.ReadShort(data, 0x12);
            EyeAnimationIndex = IO.ReadShort(data, 0x14);
            MouthAnimationIndex = IO.ReadShort(data, 0x16);
        }

        public List<(SKBitmap frame, int timing)> GetLipFlapAnimation(ArchiveFile<GraphicsFile> grp, MessageInfoFile messageInfoFile)
        {
            List<(SKBitmap, int)> frames = new();

            if (SpriteType == 0)
            {
                return frames;
            }

            List<GraphicsFile> textures = new() { grp.Files.First(f => f.Index == TextureIndex1), grp.Files.First(f => f.Index == TextureIndex2), grp.Files.First(f => f.Index == TextureIndex3) };
            GraphicsFile layout = grp.Files.First(f => f.Index == LayoutIndex);
            GraphicsFile eyeTexture = grp.Files.First(f => f.Index == EyeTextureIndex);
            GraphicsFile eyeAnimation = grp.Files.First(f => f.Index == EyeAnimationIndex);
            GraphicsFile mouthTexture = grp.Files.First(f => f.Index == MouthTextureIndex);
            GraphicsFile mouthAnimation = grp.Files.First(f => f.Index == MouthAnimationIndex);
            MessageInfo messageInfo = messageInfoFile.MessageInfos[(int)Character];

            (SKBitmap spriteBitmap, _) = layout.GetLayout(textures, 0, layout.LayoutEntries.Count, darkMode: false, preprocessedList: true);
            SKBitmap[] eyeFrames = eyeAnimation.GetAnimationFrames(eyeTexture).Select(f => f.GetImage()).ToArray();
            SKBitmap[] mouthFrames = mouthAnimation.GetAnimationFrames(mouthTexture).Select(f => f.GetImage()).ToArray();

            int e = 0, m = 0;
            int currentEyeTime = ((FrameAnimationEntry)eyeAnimation.AnimationEntries[e]).Time;
            int currentMouthTime = messageInfo.TextTimer * 30;
            for (int f = 0; f < Math.Max(eyeAnimation.AnimationEntries.Sum(a => ((FrameAnimationEntry)a).Time), 
                mouthAnimation.AnimationEntries.Sum(a => ((FrameAnimationEntry)a).Time));)
            {
                SKBitmap frame = new(spriteBitmap.Width, spriteBitmap.Height);
                SKCanvas canvas = new(frame);
                canvas.DrawBitmap(spriteBitmap, new SKPoint(0, 0));
                canvas.DrawBitmap(eyeFrames[e], new SKPoint(eyeAnimation.AnimationX, eyeAnimation.AnimationY));
                canvas.DrawBitmap(mouthFrames[m], new SKPoint(mouthAnimation.AnimationX, mouthAnimation.AnimationY));
                int time;

                if (currentEyeTime == currentMouthTime)
                {
                    f += currentEyeTime;
                    time = currentEyeTime;
                    e++;
                    m++;
                    if (e >= eyeAnimation.AnimationEntries.Count)
                    {
                        e = 0;
                    }
                    if (m >= mouthAnimation.AnimationEntries.Count)
                    {
                        m = 0;
                    }

                    currentEyeTime = ((FrameAnimationEntry)eyeAnimation.AnimationEntries[e]).Time;
                    currentMouthTime = messageInfo.TextTimer * 30;
                }
                else if (currentEyeTime < currentMouthTime)
                {
                    f += currentEyeTime;
                    time = currentEyeTime;
                    currentMouthTime -= currentEyeTime;
                    e++;
                    if (e >= eyeAnimation.AnimationEntries.Count)
                    {
                        e = 0;
                    }
                    currentEyeTime = ((FrameAnimationEntry)eyeAnimation.AnimationEntries[e]).Time;
                }
                else
                {
                    f += currentMouthTime;
                    time = currentMouthTime;
                    currentEyeTime -= currentMouthTime;
                    m++;
                    if (m >= mouthAnimation.AnimationEntries.Count)
                    {
                        m = 0;
                    }
                    currentMouthTime = messageInfo.TextTimer;
                }

                canvas.Flush();
                frames.Add((frame, time));
            }

            return frames;
        }
    }
}
