using FFMpegCore;
using FFMpegCore.Pipes;
using HaruhiChokuretsuLib.Archive;
using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Archive.Graphics;
using HaruhiChokuretsuLib.Util;
using Mono.Options;
using SkiaSharp;
using System;
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
                { "s|i|sprite|sprite-index=", "Index of chibi to export", s => _spriteIndex = int.Parse(s) },
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

            CharacterDataFile chrdata = dat.Files.First(f => f.Name == "CHRDATAS").CastTo<CharacterDataFile>();
            CharacterSprite sprite = chrdata.Sprites[_spriteIndex];

            if (_lipFlap)
            {
                List<(SKBitmap frame, int timing)> animationFrames = sprite.GetLipFlapAnimation(grp, dat.Files.First(f => f.Name == "MESSINFOS").CastTo<MessageInfoFile>());
                List<SKBitmap> frames = new();
                foreach (var frame in animationFrames)
                {
                    for (int i = 0; i < frame.timing; i++)
                    {
                        frames.Add(frame.frame);
                    }
                }

                IEnumerable<SKBitmapFrame> videoFrames = frames.Select(f => new SKBitmapFrame(f));
                List<SKBitmapFrame> loopedFrames = new();

                for (int i = 0; i < 5; i++)
                {
                    loopedFrames.AddRange(videoFrames);
                }

                RawVideoPipeSource pipeSource = new(loopedFrames) { FrameRate = 60 };

                Console.WriteLine("Creating MP4 video from frames...");

                if (!FFMpegArguments
                .FromPipeInput(pipeSource)
                    .OutputToFile(Path.Combine(_outputFolder, $"{grp.Files.First(f => f.Index == sprite.EyeAnimationIndex).Name[0..^5]}.mp4"), overwrite: true)
                    .ProcessSynchronously())
                {
                    Console.WriteLine("FFMpeg error!");
                    return 1;
                }
            }

            return 0;
        }
    }
}
