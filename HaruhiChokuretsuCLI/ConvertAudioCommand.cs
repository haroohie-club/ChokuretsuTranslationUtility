using Mono.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HaruhiChokuretsuCLI
{
    public class ConvertAudioCommand : Command
    {
        private string _directory, _adx2wav, _ahx2wav;

        public ConvertAudioCommand() : base("convert-audio")
        {
            Options = new()
            {
                "Converts all ADX/AHX files in a directory to WAV using adx2wav and ahx2wav.",
                "Usage: HaruhiChokuretsuCLI convert-audio -d [AUDIO_DIRECTORY] --adx2wav [PATH_TO_ADX2WAV] --ahx2wav [PATH_TO_AHX2WAV]",
                "",
                { "d|directory=", "The directory of audio files to convert", d => _directory = d },
                { "adx2wav=", "The path to adx2wavmod3.exe", a => _adx2wav = a },
                { "ahx2wav=", "The path to ahx2wav(_x64).exe", a => _ahx2wav = a },
            };
        }

        public override int Invoke(IEnumerable<string> arguments)
        {
            Options.Parse(arguments);

            foreach (string file in Directory.GetFiles(_directory, "*.bin"))
            {
                if (Encoding.ASCII.GetString(File.ReadAllBytes(file).TakeLast(12).ToArray()).StartsWith("AHXE(c)CRI"))
                {
                    Process p = Process.Start(_ahx2wav, file);
                    p.WaitForExit();
                }
                else
                {
                    Process p = Process.Start(_adx2wav, file);
                    p.WaitForExit();
                }
            }

            return 0;
        }
    }
}
