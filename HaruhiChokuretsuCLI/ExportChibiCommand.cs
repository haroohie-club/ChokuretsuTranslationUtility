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

                gif.SaveAsGif(Path.Combine(_outputFolder, $"{texture.Name[0..^3]}.gif"));
            }

            return 0;
        }
    }
}
