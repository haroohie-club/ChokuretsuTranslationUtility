using HaruhiChokuretsuLib.Audio;
using HaruhiChokuretsuLib.Util;
using Mono.Options;
using NAudio.Wave;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.IO;

namespace HaruhiChokuretsuCLI
{
    public class ConvertAudioCommand : Command
    {
        private string _directory;

        public ConvertAudioCommand() : base("convert-audio", "Converts all the audio files in a directory")
        {
            Options = new()
            {
                "Converts all ADX/AHX files in a directory to WAV.",
                "Usage: HaruhiChokuretsuCLI convert-audio -d [AUDIO_DIRECTORY]",
                "",
                { "d|directory=", "The directory of audio files to convert", d => _directory = d }
            };
        }

        public override int Invoke(IEnumerable<string> arguments)
        {
            Options.Parse(arguments);
            ConsoleLogger log = new();

            foreach (string file in Directory.GetFiles(_directory, "*.bin"))
            {
                byte[] bytes = File.ReadAllBytes(file);
                IAdxDecoder decoder;
                if (bytes[0x04] < 0x10) // file is ADX
                {
                    decoder = new AdxDecoder(bytes, log);
                }
                else
                {
                    decoder = new AhxDecoder(bytes, log);
                }
                AdxWaveProvider waveProvider = new(decoder);
                WaveFileWriter.CreateWaveFile($"{Path.GetFileNameWithoutExtension(file)}.wav", waveProvider);
            }

            return 0;
        }
    }

}
