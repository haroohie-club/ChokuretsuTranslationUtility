using HaruhiChokuretsuLib.Archive.Graphics;
using Mono.Options;
using SkiaSharp;
using SkiaTextRenderer;
using System.Collections.Generic;
using System.IO;

namespace HaruhiChokuretsuCLI
{
    public class VersionScreenCommand : Command
    {
        private string _version, _splashScreenPath, _fontFile, _outputPath;

        public VersionScreenCommand() : base("version-screen", "Creates a versioned splash screen")
        {
            Options = new()
            {
                "Produces a splash screen image with a specified version stamp.",
                "Usage: HaruhiChokuretsuCLI version-screen -v VERSION -s PATH_TO_SPLASHSCREEN",
                "",
                { "v|version=", "The version to be written on the splash screen", v => _version = v },
                { "s|splash-screen-path=", "The path to the splash screen image (without version)", s => _splashScreenPath = s },
                { "f|font-file=", "Font file to use while drawing", f => _fontFile = f },
                { "o|output-path=", "Output path for versioned splash screen", o => _outputPath = o },
            };
        }

        public override int Invoke(IEnumerable<string> arguments)
        {
            Options.Parse(arguments);

            string[] semVers = _version.Split('.');
            if (semVers.Length > 3)
            {
                _version = $"{semVers[0]}.{semVers[1]}.\n{semVers[2]}.\n{semVers[3]}";
            }

            SKBitmap splashScreenVersionless = SKBitmap.Decode(_splashScreenPath);

            using SKCanvas canvas = new(splashScreenVersionless);
            int y = semVers.Length <= 3 ? 556 : 526;
            int height = semVers.Length <= 3 ? 9 : 27;
            SKRect bounds = new(0, y, 64, y + height);
            TextRendererSk.DrawText(canvas, _version, new Font(SKTypeface.FromFile(_fontFile), 11.0f), bounds, SKColors.Black, TextFormatFlags.Left);

            using FileStream fileStream = new(_outputPath, FileMode.Create);
            splashScreenVersionless.Encode(fileStream, SKEncodedImageFormat.Png, GraphicsFile.PNG_QUALITY);

            CommandSet.Out.WriteLine($"Generated new splash screen with version '{_version}'.");
            return 0;
        }
    }
}
