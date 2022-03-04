using Mono.Options;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HaruhiChokuretsuCLI
{
    public class VersionScreenCommand : Command
    {
        private string _version, _splashScreenPath, _fontFile, _outputPath;

        public VersionScreenCommand() : base("version-screen")
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
            if (semVers.Length > 2)
            {
                _version = $"{semVers[0]}.{semVers[1]}.\n{semVers[2]}.\n{semVers[3]}";
            }

            Bitmap splashScreenVersionless = new(_splashScreenPath);
            PrivateFontCollection pfc = new();
            pfc.AddFontFile(_fontFile);

            Graphics g = Graphics.FromImage(splashScreenVersionless);
            int height = semVers.Length <= 2 ? 556 : 526;
            Point point = new(0, height);
            g.DrawString(_version, new Font(pfc.Families.FirstOrDefault(f => f.Name == Path.GetFileNameWithoutExtension(_fontFile).Replace('-', ' ')), 6.0f), Brushes.Black, point);

            splashScreenVersionless.Save(_outputPath);

            CommandSet.Out.WriteLine($"Generated new splash screen with version '{_version}'.");
            return 0;
        }
    }
}
