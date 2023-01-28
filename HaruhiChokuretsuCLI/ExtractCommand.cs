using HaruhiChokuretsuLib.Archive;
using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Archive.Graphics;
using HaruhiChokuretsuLib.Util;
using Mono.Options;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace HaruhiChokuretsuCLI
{
    public class ExtractCommand : Command
    {
        private string _inputArchive, _outputFile, _fileName;
        private string[] _includes;
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
                { "n|index=", "Index of file to extract (prefix with 0x to use hex number); this can be omitted if output file is named as a hex integer or if using filename",
                    n => _fileIndex = n.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? int.Parse(n[2..], NumberStyles.HexNumber) : int.Parse(n) },
                { "name=", "Name of the file to extract", name => _fileName = name },
                { "o|output-file=", "File name of extracted file (if ends in PNG, RESX, or S, will extract to those formats; otherwise extracts raw binary data)", o => _outputFile = o},
                { "w|image-width=", "Width of an image to extract (defaults to the image's encoded width)", w => _imageWidth = int.Parse(w) },
                { "includes=", "Comma-separated list of include files to use when producing a source file", include => _includes = include.Split(',') },
                { "c|compressed", "Extract compressed file", c => _compressed = true },
                { "h|help", "Shows this help screen", h => _showHelp = true },
            };
        }

        public override int Invoke(IEnumerable<string> arguments)
        {
            Options.Parse(arguments);
            ConsoleLogger log = new();

            if (_showHelp || string.IsNullOrEmpty(_inputArchive) || string.IsNullOrEmpty(_outputFile))
            {
                int returnValue = 0;
                if (string.IsNullOrEmpty(_inputArchive))
                {
                    CommandSet.Error.WriteLine("Input archive not provided, please supply -i or --input-archive");
                    returnValue = 1;
                }
                if (string.IsNullOrEmpty(_outputFile))
                {
                    CommandSet.Error.WriteLine("Output file not provided, please supply -o or --output-file");
                    returnValue = 1;
                }
                Options.WriteOptionDescriptions(CommandSet.Out);
                return returnValue;
            }

            if (string.IsNullOrEmpty(_fileName) && _fileIndex == -1 && !int.TryParse(Path.GetFileNameWithoutExtension(_outputFile), NumberStyles.HexNumber, new CultureInfo("en-US"), out _fileIndex))
            {
                CommandSet.Error.WriteLine("Either file index (-n or --index) or file name (--name) must be set or output file name (-o or --output-file) must be a hex integer.");
                Options.WriteOptionDescriptions(CommandSet.Out);
                return 1;
            }

            string directory = Path.GetDirectoryName(_outputFile);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (Path.GetExtension(_outputFile).Equals(".png", StringComparison.OrdinalIgnoreCase))
            {
                var grpArchive = ArchiveFile<GraphicsFile>.FromFile(_inputArchive, log);

                int fileIndex = _fileIndex;
                if (fileIndex < 0)
                {
                    fileIndex = grpArchive.Files.First(f => f.Name == _fileName).Index;
                }

                GraphicsFile grpFile = grpArchive.Files.First(f => f.Index == fileIndex);

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
                var evtArchive = ArchiveFile<EventFile>.FromFile(_inputArchive, log);
                int fileIndex = _fileIndex;
                if (fileIndex < 0)
                {
                    fileIndex = evtArchive.Files.First(f => f.Name == _fileName).Index;
                }
                EventFile evtFile = evtArchive.Files.First(f => f.Index == fileIndex);

                if (Path.GetFileName(_inputArchive).StartsWith("dat", StringComparison.OrdinalIgnoreCase) || (_fileIndex >= 580 && _fileIndex <= 581))
                {
                    evtFile.InitializeDialogueForSpecialFiles();
                }
                else if (_fileIndex == 589)
                {
                    VoiceMapFile vmFile = evtFile.CastTo<VoiceMapFile>();
                }

                CommandSet.Out.Write($"Extracting file #{evtFile.Index:X3} as RESX from archive {evtArchive.FileName}... ");
                evtFile.WriteResxFile(_outputFile);
                CommandSet.Out.WriteLine("OK");
            }
            else if (Path.GetExtension(_outputFile).Equals(".s", StringComparison.OrdinalIgnoreCase))
            {
                var archive = ArchiveFile<DataFile>.FromFile(_inputArchive, log);
                int fileIndex = _fileIndex;
                if (fileIndex < 0)
                {
                    fileIndex = archive.Files.First(f => f.Name == _fileName).Index;
                }
                DataFile file = archive.Files.First(f => f.Index == fileIndex);

                Dictionary<string, IncludeEntry[]> includes = new();
                if (_includes is not null)
                {
                    foreach (string include in _includes)
                    {
                        IncludeEntry[] entries = File.ReadAllLines(include).Where(l => l.Length > 1).Select(l => new IncludeEntry(l)).ToArray();
                        includes.Add(Path.GetFileNameWithoutExtension(include).ToUpper(), entries);
                    }
                }

                // There's not a better way to do this that I can think of
                // Sorry
                ISourceFile sourceFile;
                if (archive.FileName.StartsWith("dat", StringComparison.OrdinalIgnoreCase))
                {
                    DataFile qmapDataFile = archive.Files.First(f => f.Name == "QMAPS");
                    QMapFile qmapFile = qmapDataFile.CastTo<QMapFile>();
                    archive.Files[archive.Files.IndexOf(qmapDataFile)] = qmapFile;

                    if (qmapFile.QMaps.Select(q => q.Name).Contains(file.Name))
                    {
                        file = file.CastTo<MapFile>();
                    }
                    else
                    {
                        switch (file.Name)
                        {
                            case "BGTBLS":
                                file = file.CastTo<BgTableFile>();
                                break;
                            case "MESSINFOS":
                                file = file.CastTo<MessageInfoFile>();
                                break;
                            case "PLACES":
                                file = file.CastTo<PlaceFile>();
                                break;
                            case "QMAPS":
                                file = qmapFile;
                                break;
                            case "SYSTEXS":
                                file = file.CastTo<SystemTextureFile>();
                                break;
                        }
                    }
                    sourceFile = file;
                }
                else if (archive.FileName.StartsWith("evt", StringComparison.OrdinalIgnoreCase))
                {
                    EventFile evtFile = file.CastTo<EventFile>();
                    sourceFile = evtFile;
                }
                else
                {
                    CommandSet.Error.WriteLine("Source files are only contained in dat.bin and evt.bin; please use one of those two bin archives.");
                    return 1;
                }

                CommandSet.Out.Write($"Extracting file #{file.Index:X3} as ASM from archive {archive.FileName}... ");
                File.WriteAllText(_outputFile, sourceFile.GetSource(includes));
                CommandSet.Out.WriteLine("OK");
            }
            else
            {
                var archive = ArchiveFile<FileInArchive>.FromFile(_inputArchive, log);
                int fileIndex = _fileIndex;
                if (fileIndex < 0)
                {
                    fileIndex = archive.Files.First(f => f.Name == _fileName).Index;
                }
                FileInArchive file = archive.Files.First(f => f.Index == fileIndex);

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
                CommandSet.Out.WriteLine("OK");
            }

            return 0;
        }
    }
}
