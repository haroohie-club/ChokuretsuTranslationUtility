using HaruhiChokuretsuLib.Archive;
using Mono.Options;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace HaruhiChokuretsuCLI
{
    public class ExtractCommand : Command
    {
        private string _inputArchive, _outputFile;
        private int _fileIndex = -1, _imageWidth = -1;
        private bool _showHelp, _compressed;

        public ExtractCommand() : base("extract", "Extracts a file from an archive")
        {
            Options = new()
            {
                "Extracts a single file from an archive (including images from grp.bin or RESX files from evt.bin or dat.bin)",
                "Usage: HaruhiChokuretsuCLI extract -i [inputArchive] -o [outputFile] [OPTIONS]",
                "",
                { "i|input-archive=", "Archive to extract file from", i => _inputArchive = i },
                { "n|index=", "Index of file to extract (prefix with 0x to use hex number); this can be omitted if output file is named as a hex integer",
                    n => _fileIndex = n.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? int.Parse(n[2..], NumberStyles.HexNumber) : int.Parse(n) },
                { "o|output-file=", "File name of extracted file (if ends in PNG or RESX, will extract to those formats; otherwise extracts raw binary data)", o => _outputFile = o},
                { "w|image-width=", "Width of an image to extract (defaults to the image's encoded width)", w => _imageWidth = int.Parse(w) },
                { "c|compressed", "Extract compressed file", c => _compressed = true },
                { "h|help", "Shows this help screen", h => _showHelp = true },
            };
        }

        public override int Invoke(IEnumerable<string> arguments)
        {
            Options.Parse(arguments);

            if (_showHelp || string.IsNullOrEmpty(_inputArchive) || string.IsNullOrEmpty(_outputFile))
            {
                int returnValue = 0;
                if (string.IsNullOrEmpty(_inputArchive))
                {
                    CommandSet.Out.WriteLine("Input archive not provided, please supply -i or --input-archive");
                    returnValue = 1;
                }
                if (string.IsNullOrEmpty(_outputFile))
                {
                    CommandSet.Out.WriteLine("Output file not provided, please supply -o or --output-file");
                    returnValue = 1;
                }
                Options.WriteOptionDescriptions(CommandSet.Out);
                return returnValue;
            }

            if (_fileIndex == -1 && !int.TryParse(Path.GetFileNameWithoutExtension(_outputFile), NumberStyles.HexNumber, new CultureInfo("en-US"), out _fileIndex))
            {
                CommandSet.Out.WriteLine("Either file index (-n or --index) must be set or output file name (-o or --output-file) must be a hex integer.");
                Options.WriteOptionDescriptions(CommandSet.Out);
                return 1;
            }

            string directory = Path.GetDirectoryName(_outputFile);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            try
            {
                if (Path.GetExtension(_outputFile).Equals(".png", StringComparison.OrdinalIgnoreCase))
                {
                    var grpArchive = ArchiveFile<GraphicsFile>.FromFile(_inputArchive);
                    GraphicsFile grpFile = grpArchive.Files.First(f => f.Index == _fileIndex);

                    if (grpFile.Index == 0xE50)
                    {
                        grpFile.InitializeFontFile();
                    }

                    CommandSet.Out.Write($"Extracting file #{grpFile.Index:X3} as image from archive {grpArchive.FileName}... ");
                    using FileStream fileStream = new(_outputFile, FileMode.Create);
                    grpFile.GetImage(_imageWidth).Encode(fileStream, SKEncodedImageFormat.Png, GraphicsFile.PNG_QUALITY);
                    CommandSet.Out.WriteLine("OK");
                }
                else if (Path.GetExtension(_outputFile).Equals(".resx", StringComparison.OrdinalIgnoreCase))
                {
                    var evtArchive = ArchiveFile<EventFile>.FromFile(_inputArchive);
                    EventFile evtFile = evtArchive.Files.First(f => f.Index == _fileIndex);

                    if (Path.GetFileName(_inputArchive).StartsWith("dat", StringComparison.OrdinalIgnoreCase) || (_fileIndex >= 580 && _fileIndex <= 581))
                    {
                        evtFile.InitializeDialogueForSpecialFiles();
                    }
                    else if (_fileIndex == 589)
                    {
                        VoiceMapFile vmFile = new();
                        vmFile.Initialize(evtFile.Data.ToArray(), evtFile.Offset);
                        evtFile = vmFile;
                    }

                    CommandSet.Out.Write($"Extracting file #{evtFile.Index:X3} as RESX from archive {evtArchive.FileName}... ");
                    evtFile.WriteResxFile(_outputFile);
                    CommandSet.Out.WriteLine("OK");
                }
                else
                {
                    var archive = ArchiveFile<FileInArchive>.FromFile(_inputArchive);
                    FileInArchive file = archive.Files.First(f => f.Index == _fileIndex);

                    if (_compressed)
                    {
                        CommandSet.Out.Write($"Extracting compressed file #{file.Index:X3} from archive {archive.FileName}... ");
                        File.WriteAllBytes(_outputFile, file.CompressedData);
                    }
                    else
                    {
                        CommandSet.Out.Write($"Extracting file #{file.Index:X3} from archive {archive.FileName}... ");
                        File.WriteAllBytes(_outputFile, file.GetBytes());
                    }
                }
            }
            catch (Exception e)
            {
                CommandSet.Out.WriteLine($"Failed to extract file #0x{_fileIndex:X3} from archive {_inputArchive}, exception '{e.Message}'");
            }

            return 0;
        }
    }
}
