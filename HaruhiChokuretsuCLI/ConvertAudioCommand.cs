using HaruhiChokuretsuLib.Audio;
using HaruhiChokuretsuLib.Util;
using Mono.Options;
using NAudio.Wave;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HaruhiChokuretsuCLI
{
    public class ConvertAudioCommand : Command
    {
        private string _directory, _adx2wav, _ahx2wav;

        public ConvertAudioCommand() : base("convert-audio", "Converts all the audio files in a directory")
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
            ConsoleLogger log = new();

            foreach (string file in Directory.GetFiles(_directory, "*.bin"))
            {
                byte[] bytes = File.ReadAllBytes(file);
                if (bytes[0x04] < 0x10) // file is ADX
                {
                    AdxDecoder adxDecoder = new(bytes, log);
                    using WaveFileWriter wavWriter = new(Path.Combine(Path.GetDirectoryName(file), $"{Path.GetFileNameWithoutExtension(file)}.wav"), new((int)adxDecoder.Header.SampleRate, adxDecoder.Header.ChannelCount));

                    Sample nextSample = adxDecoder.NextSample();
                    while (nextSample is not null)
                    {
                        wavWriter.Write(nextSample.SelectMany(s => BigEndianIO.GetBytes(s)).ToArray());
                        nextSample = adxDecoder.NextSample();
                    }
                }
                else // file is AHX
                {

                }

                //if (Encoding.ASCII.GetString(File.ReadAllBytes(file).TakeLast(12).ToArray()).StartsWith("AHXE(c)CRI"))
                //{
                //    Process p = Process.Start(_ahx2wav, file);
                //    p.WaitForExit();
                //}
                //else
                //{
                //    Process p = Process.Start(_adx2wav, file);
                //    p.WaitForExit();
                //}
            }



            return 0;
        }
    }
}
