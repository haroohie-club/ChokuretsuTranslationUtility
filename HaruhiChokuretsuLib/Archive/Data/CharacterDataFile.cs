using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Archive.Graphics;
using HaruhiChokuretsuLib.Util;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HaruhiChokuretsuLib.Archive.Data;

/// <summary>
/// A representation of CHRDATA.S in dat.bin
/// </summary>
public class CharacterDataFile : DataFile
{
    /// <summary>
    /// The list of character sprites contained in the character data file
    /// </summary>
    public List<CharacterSprite> Sprites { get; set; } = [];

    /// <inheritdoc/>
    public override void Initialize(byte[] decompressedData, int offset, ILogger log)
    {
        Log = log;

        int numSections = IO.ReadInt(decompressedData, 0);
        if (numSections != 1)
        {
            Log.LogError($"Character data file should only have 1 section; {numSections} specified");
            return;
        }
        int sectionStart = IO.ReadInt(decompressedData, 0x0C);
        int sectionCount = IO.ReadInt(decompressedData, 0x10);

        for (int i = 0; i < sectionCount; i++)
        {
            Sprites.Add(new(decompressedData[(sectionStart + 0x18 * i)..(sectionStart + 0x18 * (i + 1))]));
        }
    }

    /// <inheritdoc/>
    public override string GetSource(Dictionary<string, IncludeEntry[]> includes)
    {
        if (!includes.ContainsKey("GRPBIN"))
        {
            Log.LogError("Includes needs GRPBIN to be present.");
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
            sb.AppendLine($".short {sprite.Unknown00}");
            sb.AppendLine($".short {(sprite.IsLarge ? 1 : 0)}");
            sb.AppendLine($".short {(short)sprite.Character}");
            sb.AppendLine($".short {(sprite.TextureIndex1 > 0 ? includes["GRPBIN"].First(inc => inc.Value == sprite.TextureIndex1).Name : 0)}");
            sb.AppendLine($".short {(sprite.TextureIndex2 > 0 ? includes["GRPBIN"].First(inc => inc.Value == sprite.TextureIndex2).Name : 0)}");
            sb.AppendLine($".short {(sprite.LayoutIndex > 0 ? includes["GRPBIN"].First(inc => inc.Value == sprite.LayoutIndex).Name : 0)}");
            sb.AppendLine($".short {(sprite.TextureIndex3 > 0 ? includes["GRPBIN"].First(inc => inc.Value == sprite.TextureIndex3).Name : 0)}");
            sb.AppendLine($".short {sprite.Padding}");
            sb.AppendLine($".short {(sprite.EyeTextureIndex > 0 ? includes["GRPBIN"].First(inc => inc.Value == sprite.EyeTextureIndex).Name : 0)}");
            sb.AppendLine($".short {(sprite.MouthTextureIndex > 0 ? includes["GRPBIN"].First(inc => inc.Value == sprite.MouthTextureIndex).Name : 0)}");
            sb.AppendLine($".short {(sprite.EyeAnimationIndex > 0 ? includes["GRPBIN"].First(inc => inc.Value == sprite.EyeAnimationIndex).Name : 0)}");
            sb.AppendLine($".short {(sprite.MouthAnimationIndex > 0 ? includes["GRPBIN"].First(inc => inc.Value == sprite.MouthAnimationIndex).Name : 0)}");
            sb.AppendLine();
        }

        sb.AppendLine("END_POINTERS:");
        sb.AppendLine(".word 0");

        return sb.ToString();
    }
}

/// <summary>
/// A representation of a character sprite as displayed on screen during Chokuretsu's VN sections;
/// defined in CHRDATA.S
/// </summary>
public class CharacterSprite
{
    /// <summary>
    /// Unknown
    /// </summary>
    public short Unknown00 { get; set; }
    /// <summary>
    /// Is true if the sprite is large
    /// </summary>
    public bool IsLarge { get; set; }
    /// <summary>
    /// The character depicted in the sprite (defined with the same Speaker value used in scripts)
    /// </summary>
    public Speaker Character { get; set; }
    /// <summary>
    /// The grp.bin index of the first texture used in the sprite layout
    /// </summary>
    public short TextureIndex1 { get; set; }
    /// <summary>
    /// The grp.bin index of the second texture used in the sprite layout
    /// </summary>
    public short TextureIndex2 { get; set; }
    /// <summary>
    /// The grp.bin index of the sprite layout
    /// </summary>
    public short LayoutIndex { get; set; }
    /// <summary>
    /// The grp.bin index of the third texture used in the sprite layout
    /// </summary>
    public short TextureIndex3 { get; set; }
    /// <summary>
    /// Unused
    /// </summary>
    public short Padding { get; set; }
    /// <summary>
    /// The grp.bin index of the eye texture
    /// </summary>
    public short EyeTextureIndex { get; set; }
    /// <summary>
    /// The grp.bin index of the mouth texture
    /// </summary>
    public short MouthTextureIndex { get; set; }
    /// <summary>
    /// The grp.bin index of the eye animation file
    /// </summary>
    public short EyeAnimationIndex { get; set; }
    /// <summary>
    /// The grp.bin index of the mouth animation file
    /// </summary>
    public short MouthAnimationIndex { get; set; }

    /// <summary>
    /// Parameterless constructor for serialization
    /// </summary>
    public CharacterSprite()
    {
    }

    /// <summary>
    /// Constructs a character sprite from data
    /// </summary>
    /// <param name="data">Binary data representing the cahracter sprite</param>
    public CharacterSprite(byte[] data)
    {
        Unknown00 = IO.ReadShort(data, 0);
        IsLarge = IO.ReadShort(data, 0x02) == 1;
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

    /// <summary>
    /// Gets the animation of the sprite blinking without lip flap animation
    /// </summary>
    /// <param name="grp">The grp.bin ArchiveFile object</param>
    /// <param name="messageInfoFile">The MessageInfo file from dat.bin</param>
    /// <returns>A list of tuples containing SKBitmap frames and timings for how long those frames are to be displayed</returns>
    public List<(SKBitmap frame, int timing)> GetClosedMouthAnimation(ArchiveFile<GraphicsFile> grp, MessageInfoFile messageInfoFile)
    {
        return GetAnimation(grp, messageInfoFile, false, null, null, null, null, null, null);
    }

    /// <summary>
    /// Gets the animation of the sprite blinking and moving its lips
    /// </summary>
    /// <param name="grp">The grp.bin ArchiveFile object</param>
    /// <param name="messageInfoFile">The MessageInfo file from dat.bin</param>
    /// <returns>A list of tuples containing SKBitmap frames and timings for how long those frames are to be displayed</returns>
    public List<(SKBitmap frame, int timing)> GetLipFlapAnimation(ArchiveFile<GraphicsFile> grp, MessageInfoFile messageInfoFile)
    {
        return GetAnimation(grp, messageInfoFile, true, null, null, null, null, null, null);
    }

    /// <summary>
    /// Gets the animation of the sprite blinking without lip flap animation
    /// </summary>
    /// <param name="messageInfoFile">The MessageInfo file from dat.bin</param>
    /// <param name="bodyLayout">The associated layout graphic</param>
    /// <param name="bodyTextures">The associated main body texture graphic</param>
    /// <param name="eyeAnimation">The associated eye animation graphic</param>
    /// <param name="eyeTexture">The associated eye texture graphic</param>
    /// <param name="mouthAnimation">The associated mouth animation graphic</param>
    /// <param name="mouthTexture">The associated mouth texture graphic</param>
    /// <returns>A list of tuples containing SKBitmap frames and timings for how long those frames are to be displayed</returns>
    public List<(SKBitmap frame, int timing)> GetClosedMouthAnimation(MessageInfoFile messageInfoFile, GraphicsFile bodyLayout, IEnumerable<GraphicsFile> bodyTextures, GraphicsFile eyeAnimation, GraphicsFile eyeTexture, GraphicsFile mouthAnimation, GraphicsFile mouthTexture)
    {
        return GetAnimation(null, messageInfoFile, false, bodyLayout, bodyTextures, eyeAnimation, eyeTexture, mouthAnimation, mouthTexture);
    }

    /// <summary>
    /// Gets the animation of the sprite blinking and moving its lips
    /// </summary>
    /// <param name="messageInfoFile">The MessageInfo file from dat.bin</param>
    /// <param name="bodyLayout">The associated layout graphic</param>
    /// <param name="bodyTextures">The associated main body texture graphics</param>
    /// <param name="eyeAnimation">The associated eye animation graphic</param>
    /// <param name="eyeTexture">The associated eye texture graphic</param>
    /// <param name="mouthAnimation">The associated mouth animation graphic</param>
    /// <param name="mouthTexture">The associated mouth texture graphic</param>
    /// <returns>A list of tuples containing SKBitmap frames and timings for how long those frames are to be displayed</returns>
    public List<(SKBitmap frame, int timing)> GetLipFlapAnimation(MessageInfoFile messageInfoFile, GraphicsFile bodyLayout, IEnumerable<GraphicsFile> bodyTextures, GraphicsFile eyeAnimation, GraphicsFile eyeTexture, GraphicsFile mouthAnimation, GraphicsFile mouthTexture)
    {
        return GetAnimation(null, messageInfoFile, true, bodyLayout, bodyTextures, eyeAnimation, eyeTexture, mouthAnimation, mouthTexture);
    }

    private List<(SKBitmap frame, int timing)> GetAnimation(
        ArchiveFile<GraphicsFile> grp,
        MessageInfoFile messageInfoFile,
        bool lipFlap,
        GraphicsFile bodyLayout,
        IEnumerable<GraphicsFile> bodyTextures,
        GraphicsFile eyeAnimation,
        GraphicsFile eyeTexture,
        GraphicsFile mouthAnimation,
        GraphicsFile mouthTexture)
    {
        List<(SKBitmap, int)> frames = [];

        if (Unknown00 == 0)
        {
            return frames;
        }

        List<GraphicsFile> textures = [];
        textures.AddRange(grp is not null
            ? [grp.GetFileByIndex(TextureIndex1), grp.GetFileByIndex(TextureIndex2), grp.GetFileByIndex(TextureIndex3)]
            : bodyTextures);
        if (bodyLayout is null && grp is not null)
        {
            bodyLayout = grp.GetFileByIndex(LayoutIndex);
        }
        if (eyeTexture is null && grp is not null)
        {
            eyeTexture = grp.GetFileByIndex(EyeTextureIndex);
            eyeAnimation = grp.GetFileByIndex(EyeAnimationIndex);
        }
        if (mouthTexture is null && grp is not null)
        {
            mouthTexture = grp.GetFileByIndex(MouthTextureIndex);
            mouthAnimation = grp.GetFileByIndex(MouthAnimationIndex);
        }
        MessageInfo messageInfo = messageInfoFile.MessageInfos[(int)Character];

        (SKBitmap spriteBitmap, _) = bodyLayout!.GetLayout(textures, 0, bodyLayout.LayoutEntries.Count, darkMode: false, preprocessedList: true);
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
            canvas.DrawBitmap(lipFlap ? mouthFrames[m] : mouthFrames[0],
                new SKPoint(mouthAnimation.AnimationX, mouthAnimation.AnimationY));
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