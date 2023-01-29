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
    public class ExportChibiCommand : Command
    {
        private string _dat, _grp, _outputFolder;
        private int _chibiIndex;

        public ExportChibiCommand() : base("export-chibi", "Exports a series of animated chibis from CHIBI.S")
        {
            Options = new()
            {
                { "d|dat=", "Location of dat.bin", d => _dat = d },
                { "g|grp=", "Location of grp.bin", g => _grp = g },
                { "c|i|chibi|chibi-index=", "Index of chibi to export", c => _chibiIndex = int.Parse(c) },
                { "o|output|output-dir=", "Output directory for chibi animations", o => _outputFolder = o },
            };
        }

        public override int Invoke(IEnumerable<string> arguments)
        {
            Options.Parse(arguments);

            ConsoleLogger log = new();
            var dat = ArchiveFile<DataFile>.FromFile(_dat, log);
            var grp = ArchiveFile<GraphicsFile>.FromFile(_grp, log);

            _outputFolder = Path.Combine(_outputFolder, $"CHIBI{_chibiIndex:D2}");

            if (!Directory.Exists(_outputFolder))
            {
                Directory.CreateDirectory(_outputFolder);
            }

            ChibiFile chibiFile = dat.Files.First(f => f.Name == "CHIBIS").CastTo<ChibiFile>();
            foreach (ChibiEntry chibiEntry in chibiFile.Chibis[_chibiIndex - 1].ChibiEntries)
            {
                if (chibiEntry.Texture == 0 || chibiEntry.Animation == 0)
                {
                    continue;
                }
                GraphicsFile texture = grp.Files.First(f => f.Index == chibiEntry.Texture);
                GraphicsFile animation = grp.Files.First(f => f.Index == chibiEntry.Animation);

                List<GraphicsFile> animationFrames = animation.GetAnimationFrames(texture);
                foreach (GraphicsFile animationFrame in animationFrames)
                {
                    animationFrame.Height = animationFrames.Max(f => f.Height);
                    animationFrame.Height += animationFrame.Height % 2 == 0 ? 0 : 1;
                }

                List<SKBitmap> frames = new();
                for (int i = 0; i < animationFrames.Count; i++)
                {
                    SKBitmap frame = animationFrames[i].GetImage();
                    for (int j = 0; j < ((FrameAnimationEntry)animation.AnimationEntries[i]).Time; j++)
                    {
                        frames.Add(frame);
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
                    .OutputToFile(Path.Combine(_outputFolder, $"{texture.Name[0..^3]}.mp4"), overwrite: true)
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
