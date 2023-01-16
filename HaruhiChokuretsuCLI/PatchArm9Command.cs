using HaruhiChokuretsuLib.NDS.Nitro;
using Mono.Options;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace HaruhiChokuretsuCLI
{
    public class PatchArm9Command : Command
    {
        private string _inputDir, _outputDir;
        private uint _arenaLoOffset;

        public PatchArm9Command() : base("patch-arm9")
        {
            Options = new()
            {
                { "i|input-dir=", "Input directory containing arm9.bin and source", i => _inputDir = i },
                { "o|output-dir=", "Output directory for writing modified arm9.bin", o => _outputDir = o },
                { "a|arena-lo-offset=", "ArenaLoOffset provided as a hex number", a => _arenaLoOffset = uint.Parse(a, NumberStyles.HexNumber) },
            };
        }

        public override int Invoke(IEnumerable<string> arguments)
        {
            Options.Parse(arguments);

            if (string.IsNullOrEmpty(_inputDir))
            {
                _inputDir = Path.Combine(Environment.CurrentDirectory, "src");
            }
            if (string.IsNullOrEmpty(_outputDir))
            {
                _outputDir = Path.Combine(Environment.CurrentDirectory, "rom");
            }

            if (!Directory.Exists(_inputDir))
            {
                throw new ArgumentException($"Input directory {_inputDir} does not exist!");
            }
            if (!Directory.Exists(_outputDir))
            {
                Directory.CreateDirectory(_outputDir);
            }

            ARM9 arm9 = new(File.ReadAllBytes(Path.Combine(_inputDir, "arm9.bin")), 0x02000000);
            if (!ARM9AsmHack.Insert(_inputDir, arm9, _arenaLoOffset))
            {
                Console.WriteLine("ERROR: ASM hack insertion failed!");
                return 1;
            }
            File.WriteAllBytes(Path.Combine(_outputDir, "arm9.bin"), arm9.GetBytes());

            return 0;
        }
    }
}
