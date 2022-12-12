using HaruhiChokuretsuLib;
using HaruhiChokuretsuLib.Archive;
using Mono.Options;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace HaruhiChokuretsuCLI
{
    public class ReplaceCommand : Command
    {
        string _inputArchive, _outputArchive, _replacement;
        private bool _showHelp;
        private Dictionary<int, List<SKColor>> _palettes = new();

        public ReplaceCommand() : base("replace", "Replaces a file or set of files in an archive")
        {
            Options = new()
            {
                "Replaces a single file (given a file) or a set of files (given a directory) in an archive",
                "Usage: HaruhiChokuretsuCLI replace -i [inputArchive] -o [outputArchive] -r [replacementFileOrDirectory]",
                "",
                { "i|input-archive=", "Archive to replace file(s) in", i => _inputArchive = i },
                { "o|output-archive=", "Location to save modified archive", o => _outputArchive = o },
                { "r|replacement=", "File or directory to replace with/from; images must be .PNG files and other files must be .BIN files. " +
                                    "File names must follow a specific format: " +
                                    "\n\t\"(hex)|new[_newpal|_sharedpal(num)[_tidx(num)]][_(comments)].(ext)\"\n\t(hex) is a hex number representing the index " +
                                    "of the file to replace. You can alternatively specify \"new\" to add a file to the archive instead. New graphics " +
                                    "files must additionally specify whether they are 4bpp or 8bpp and tiles or textures as well." +
                                    "\n\tnewpal is an optional component for graphics files to specify that a new palette should be created for the" +
                                    "replaced image." +
                                    "\n\tsharedpal(num) is an optional component that can be used in place of newpal when multiple files must share the" +
                                    "same palette. All files with the same index specified by (num) will have a single palette generated for them." +
                                    "\n\ttidx(num) is an optional component for graphics files to specify the transparent index (typically 0) " +
                                    "for a new palette." +
                                    "\n\t(comments) are any comments on the contents of the image. These will be ignored during file replacement." +
                                    "\n\tFinally, a file containing the term \"ignore\" will be ignored by the replacement process.",
                    r => _replacement = r },
                { "h|help", "Shows this help screen", h => _showHelp = true },
            };
        }

        public override int Invoke(IEnumerable<string> arguments)
        {
            Options.Parse(arguments);

            if (_showHelp || string.IsNullOrEmpty(_inputArchive) || string.IsNullOrEmpty(_outputArchive) || string.IsNullOrEmpty(_replacement))
            {
                int returnValue = 0;
                if (string.IsNullOrEmpty(_inputArchive))
                {
                    CommandSet.Out.WriteLine("Input archive not provided, please supply -i or --input-archive");
                    returnValue = 1;
                }
                if (string.IsNullOrEmpty(_outputArchive))
                {
                    CommandSet.Out.WriteLine("Output archive not provided, please supply -o or --output-archive");
                    returnValue = 1;
                }
                if (string.IsNullOrEmpty(_replacement))
                {
                    CommandSet.Out.WriteLine("Replacement not provided, please supply -r or --replacement");
                    returnValue = 1;
                }
                Options.WriteOptionDescriptions(CommandSet.Out);
                return returnValue;
            }

            string outputDirectory = Path.GetDirectoryName(_outputArchive);
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            List<string> filePaths = new();

            if (Path.HasExtension(_replacement))
            {
                filePaths.Add(_replacement);
            }
            else
            {
                filePaths.AddRange(Directory.EnumerateFiles(_replacement, "*.*", SearchOption.AllDirectories));
            }

            try
            {
                var archive = ArchiveFile<FileInArchive>.FromFile(_inputArchive);
                CommandSet.Out.WriteLine($"Beginning file replacement in {archive.FileName}...");

                Dictionary<int, List<string>> filesWithSharedPalettes = new();
                for (int i = 0; i < filePaths.Count; i++)
                {
                    Match match = Regex.Match(filePaths[i], @"sharedpal(?<index>\d+)", RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        int index = int.Parse(match.Groups["index"].Value);
                        if (!_palettes.ContainsKey(index))
                        {
                            if (!filesWithSharedPalettes.ContainsKey(index))
                            {
                                filesWithSharedPalettes.Add(index, new List<string>());
                            }
                            filesWithSharedPalettes[index].Add(filePaths[i]);
                        }
                    }
                }

                foreach (int key in filesWithSharedPalettes.Keys)
                {
                    CommandSet.Out.WriteLine($"Generating shared palette for set {key}...");
                    List<SKBitmap> images = filesWithSharedPalettes[key].Select(f => SKBitmap.Decode(f)).ToList();
                    _palettes.Add(key, Helpers.GetPaletteFromImages(images, 256));
                }

                foreach (string filePath in filePaths)
                {
                    int? index = GetIndexByFileName(filePath);
                    if (index is not null)
                    {
                        if (index >= 0)
                        {
                            CommandSet.Out.Write($"Replacing #{index:X3}... ");
                        }
                        else
                        {
                            CommandSet.Out.Write($"Adding new file from {Path.GetFileName(filePath)}... ");
                        }

                        try
                        {
                            if (Path.GetFileName(filePath).StartsWith("new", StringComparison.OrdinalIgnoreCase))
                            {
                                AddNewFile(archive, filePath);
                            }
                            else if (Path.GetExtension(filePath).Equals(".png", StringComparison.OrdinalIgnoreCase))
                            {
                                ReplaceSingleGraphicsFile(archive, filePath, index.Value, _palettes);
                            }
                            else if (Path.GetExtension(filePath).Equals(".bin", StringComparison.OrdinalIgnoreCase))
                            {
                                ReplaceSingleFile(archive, filePath, index.Value);
                            }
                            else
                            {
                                throw new ArgumentException($"Unsure what to do with file '{Path.GetFileName(filePath)}'");
                            }

                            CommandSet.Out.WriteLine("OK");
                        }
                        catch (Exception e)
                        {
                            CommandSet.Out.WriteLine($"NOT OK: {e.Message}");
                        }

                    }
                }
                File.WriteAllBytes(_outputArchive, archive.GetBytes());
            }
            catch (Exception e)
            {
                CommandSet.Out.WriteLine($"Fatal exception: {e.Message}");
                return 1;
            }

            CommandSet.Out.WriteLine("Done.");
            return 0;
        }

        private static int? GetIndexByFileName(string filePath)
        {
            string fileName = Path.GetFileNameWithoutExtension(filePath);

            if (fileName.Contains("ignore", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (fileName.StartsWith("new", StringComparison.OrdinalIgnoreCase))
            {
                return -1;
            }

            if (int.TryParse(fileName.Split('_')[0], NumberStyles.HexNumber, new CultureInfo("en-US"), out int index))
            {
                return index;
            }

            return null;
        }

        private static void AddNewFile(ArchiveFile<FileInArchive> archive, string filePath)
        {
            if (Path.GetExtension(filePath).Equals(".png", StringComparison.OrdinalIgnoreCase))
            {
                GraphicsFile graphicsFile = new();
                graphicsFile.NewFile(filePath);
                archive.AddFile(graphicsFile);
            }
            else if (filePath.EndsWith("_voicemap.csv", StringComparison.OrdinalIgnoreCase))
            {
                VoiceMapFile voiceMapFile = new();
                voiceMapFile.NewFile(filePath);
                archive.AddFile(voiceMapFile);
            }
            else
            {
                FileInArchive file = new();
                file.Data = File.ReadAllBytes(filePath).ToList();
                archive.AddFile(file);
            }
        }

        /// <summary>
        /// Replace graphics file by converting a PNG image
        /// </summary>
        /// <param name="archive"></param>
        /// <param name="filePath"></param>
        /// <param name="index"></param>
        private static void ReplaceSingleGraphicsFile(ArchiveFile<FileInArchive> archive, string filePath, int index, Dictionary<int, List<SKColor>> sharedPalettes)
        {
            FileInArchive file = archive.Files.FirstOrDefault(f => f.Index == index);
            GraphicsFile grpFile = file.CastTo<GraphicsFile>();

            if (index == 0xE50)
            {
                grpFile.InitializeFontFile();
            }

            int transparentIndex = -1;
            Match transparentIndexMatch = Regex.Match(filePath, @"tidx(?<transparentIndex>\d+)", RegexOptions.IgnoreCase);
            if (transparentIndexMatch.Success)
            {
                transparentIndex = int.Parse(transparentIndexMatch.Groups["transparentIndex"].Value);
            }
            Match sharedPaletteMatch = Regex.Match(filePath, @"sharedpal(?<index>\d+)", RegexOptions.IgnoreCase);
            if (sharedPaletteMatch.Success)
            {
                grpFile.SetPalette(sharedPalettes[int.Parse(sharedPaletteMatch.Groups["index"].Value)]);
            }
            bool newSize = filePath.Contains("newsize");

            grpFile.SetImage(filePath, setPalette: Path.GetFileNameWithoutExtension(filePath).Contains("newpal", StringComparison.OrdinalIgnoreCase), transparentIndex: transparentIndex, newSize: newSize);

            archive.Files[archive.Files.IndexOf(file)] = grpFile;
        }

        /// <summary>
        /// Replace any file
        /// </summary>
        /// <param name="archive">An archive to replace a file in</param>
        /// <param name="filePath">Path to decompressed file</param>
        /// <param name="index">The index of the file in the archive to replace</param>
        private static void ReplaceSingleFile(ArchiveFile<FileInArchive> archive, string filePath, int index)
        {
            FileInArchive file = archive.Files.FirstOrDefault(f => f.Index == index);
            file.Data = File.ReadAllBytes(filePath).ToList();
            file.Edited = true;
            archive.Files[archive.Files.IndexOf(file)] = file;
        }
    }
}
