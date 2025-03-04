using HaruhiChokuretsuLib.Archive;
using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Archive.Graphics;
using HaruhiChokuretsuLib.Util;
using Mono.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.PixelFormats;
using SkiaSharp;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HaruhiChokuretsuCLI
{
    public class ExportCharacterSpriteCommand : Command
    {
        private string _dat, _grp, _outputFolder;
        private int _spriteIndex;
        private bool _lipFlap;

        public ExportCharacterSpriteCommand() : base("export-character-sprite", "Exports a character sprite from CHRDATA.S")
        {
            Options = new()
            {
                { "d|dat=", "Location of dat.bin", d => _dat = d },
                { "g|grp=", "Location of grp.bin", g => _grp = g },
                { "s|i|sprite|sprite-index=", "Index of character sprite to export", s => _spriteIndex = int.Parse(s) },
                { "l|lip-flap", "If used, will include lip flap animation", l => _lipFlap = true },
                { "o|output|output-dir=", "Output directory for chibi animations", o => _outputFolder = o },
            };
        }

        public override int Invoke(IEnumerable<string> arguments)
        {
            Options.Parse(arguments);

            ConsoleLogger log = new();
            var dat = ArchiveFile<DataFile>.FromFile(_dat, log);
            var grp = ArchiveFile<GraphicsFile>.FromFile(_grp, log);

            if (!Directory.Exists(_outputFolder))
            {
                Directory.CreateDirectory(_outputFolder);
            }

            CharacterDataFile chrdata = dat.GetFileByName("CHRDATAS").CastTo<CharacterDataFile>();
            CharacterSprite sprite = chrdata.Sprites[_spriteIndex];

            List<(SKBitmap frame, int timing)> animationFrames;
            if (_lipFlap)
            {
                animationFrames = sprite.GetLipFlapAnimation(grp, dat.GetFileByName("MESSINFOS").CastTo<MessageInfoFile>());
            }
            else
            {
                animationFrames = sprite.GetClosedMouthAnimation(grp, dat.GetFileByName("MESSINFOS").CastTo<MessageInfoFile>());
            }
            List<SKBitmap> frames = [];
            foreach (var frame in animationFrames)
            {
                for (int i = 0; i < frame.timing; i++)
                {
                    frames.Add(frame.frame);
                }
            }

            using Image<Rgba32> gif = new(frames.Max(f => f.Width), frames.Max(f => f.Height));
            gif.Metadata.GetGifMetadata().RepeatCount = 0;

            IEnumerable<Image<Rgba32>> gifFrames = frames.Select(f => Image.LoadPixelData<Rgba32>(f.Pixels.Select(c => new Rgba32(c.Red, c.Green, c.Blue, c.Alpha)).ToArray(), f.Width, f.Height));
            foreach (Image<Rgba32> gifFrame in gifFrames)
            {
                GifFrameMetadata metadata = gifFrame.Frames.RootFrame.Metadata.GetGifMetadata();
                metadata.FrameDelay = 2;
                metadata.DisposalMethod = GifDisposalMethod.RestoreToBackground;
                gif.Frames.AddFrame(gifFrame.Frames.RootFrame);
            }
            gif.Frames.RemoveFrame(0);

            gif.SaveAsGif(Path.Combine(_outputFolder, $"{grp.GetFileByIndex(sprite.EyeAnimationIndex).Name[..^5]}.gif"));

            return 0;
        }
    }
}
