using HaruhiChokuretsuLib.Archive;
using Mono.Options;
using System.Collections.Generic;
using System.IO;

namespace HaruhiChokuretsuCLI
{
    public class UnpackCommand : Command
    {
        private string _inputArchive = "", _outputDirectory = "";
        private bool _compressed, _decimal, _showHelp;

        public UnpackCommand() : base("unpack", "Unpacks an archive")
        {
            Options = new()
            {
                "Unpacks all files in an archive (either compressed or decompressed) to a specified directory",
                "Usage: HaruhiChokuretsuCLI unpack -i [inputArchive] -o [outputDirectory] [OPTIONS]",
                "",
                { "i|input-archive=", "The archive to unpack", i => _inputArchive = i },
                { "o|output-direcetory=", "The directory to unpack the archive files (will be created if does not exist)", o => _outputDirectory = o },
                { "c|compressed", "Add this flag if you want files to remain compressed", c => _compressed = true },
                { "d|decimal", "Switches the output from hexadecimal numbering to decimal", d => _decimal = true },
                { "h|help", "Show this help screen", h => _showHelp = true }
            };
        }

        public override int Invoke(IEnumerable<string> arguments)
        {
            Options.Parse(arguments);

            if (_showHelp || string.IsNullOrEmpty(_inputArchive) || string.IsNullOrEmpty(_outputDirectory))
            {
                int returnValue = 0;
                if (string.IsNullOrEmpty(_inputArchive))
                {
                    CommandSet.Out.WriteLine("Input archive not provided, please supply -i or --input-archive");
                    returnValue = 1;
                }
                if (string.IsNullOrEmpty(_outputDirectory))
                {
                    CommandSet.Out.WriteLine("Output directory not provided, please supply -o or --output-directory");
                    returnValue = 1;
                }
                Options.WriteOptionDescriptions(CommandSet.Out);
                return returnValue;
            }

            if (!Directory.Exists(_outputDirectory))
            {
                Directory.CreateDirectory(_outputDirectory);
            }

            var archive = ArchiveFile<FileInArchive>.FromFile(_inputArchive);

            if (_compressed)
            {
                archive.Files.ForEach(x => File.WriteAllBytes(Path.Combine(_outputDirectory, _decimal ? $"{x.Index:3}.bin" : $"{x.Index:X3}.bin"), x.CompressedData));
            }
            else
            {
                archive.Files.ForEach(x => File.WriteAllBytes(Path.Combine(_outputDirectory, _decimal ? $"{x.Index:D3}.bin" : $"{x.Index:X3}.bin"), x.Data.ToArray()));
            }

            CommandSet.Out.WriteLine($"Successfully unpacked {archive.Files.Count} from archive {archive.FileName}.");

            return 0;
        }
    }
}
