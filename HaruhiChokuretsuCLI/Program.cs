using HaruhiChokuretsuLib;
using HaruhiChokuretsuLib.Archive;
using HaruhiChokuretsuLib.Font;
using HaruhiChokuretsuLib.Overlay;
using Mono.Options;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace HaruhiChokuretsuCLI
{
    class Program
    {
        public enum Mode
        {
            UNPACK,
            EXTRACT,
            REPLACE,
            IMPORT_RESX,
            PATCH_OVERLAYS,
        }

        public static void Main(string[] args)
        {
            Mode mode = Mode.UNPACK;
            int imageWidth = 0;
            string inPath = "", outPath = "", replacementFolder = "", langCode = "", fontMapPath = "", romInfoPath = "";

            OptionSet options = new()
            {
                "Usage: HaruhiChokuretsuCLI [-u|-x|-r|--import-resx] -i INPUT_FILE -o OUTPUT_FILE [OPTIONS]+",
                { "u|unpack", m => mode = Mode.UNPACK },
                { "x|extract", e => mode = Mode.EXTRACT },
                { "r|replace", r => mode = Mode.REPLACE },
                { "import-resx", r => mode = Mode.IMPORT_RESX },
                { "p|patch-overlays", r => mode = Mode.PATCH_OVERLAYS },
                { "i|input=", i => inPath = i},
                { "o|output=", o => outPath = o },
                { "f|folder=", f => replacementFolder = f },
                { "image-width=", w => imageWidth = int.Parse(w) },
                { "l|lang-code=", l => langCode = l },
                { "font-map=", f => fontMapPath = f },
                { "rom-info=", r => romInfoPath = r },
            };

            try
            {
                options.Parse(args);
            }
            catch (OptionException e)
            {
                Console.WriteLine($"Error: {e.Message}");
                options.WriteOptionDescriptions(Console.Out);
            }

            if (mode == Mode.UNPACK)
            {
                UnpackAll(inPath, outPath);
            }
            else if (mode == Mode.EXTRACT)
            {
                if (imageWidth != 0)
                {
                    ExtractSingle(inPath, outPath, imageWidth);
                }
                else
                {
                    ExtractSingle(inPath, outPath);
                }
            }
            else if (mode == Mode.REPLACE && !string.IsNullOrEmpty(replacementFolder))
            {
                ReplaceFromFolder(replacementFolder, inPath, outPath);
            }
            else if (mode == Mode.IMPORT_RESX && !string.IsNullOrEmpty(replacementFolder) && !string.IsNullOrEmpty(langCode))
            {
                ImportResxFolder(replacementFolder, langCode, fontMapPath, inPath, outPath);
            }
            else if (mode == Mode.PATCH_OVERLAYS && !string.IsNullOrEmpty(replacementFolder))
            {
                PatchOverlays(inPath, replacementFolder, outPath, romInfoPath);
            }
            else
            {
                options.WriteOptionDescriptions(Console.Out);
            }
        }

        private static int? GetIndexByFileName(string filePath)
        {
            try
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

                return int.Parse(fileName.Split('_')[0], NumberStyles.HexNumber);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Unpacks all files (compressed)
        /// </summary>
        /// <param name="inPath"></param>
        /// <param name="outPath"></param>
        private static void UnpackAll(string inPath, string outPath)
        {
            var name = new FileInfo(inPath).Name;

            if (!Directory.Exists(outPath))
            {
                Directory.CreateDirectory(outPath);
            }

            var archive = ArchiveFile<FileInArchive>.FromFile(inPath);

            archive.Files.ForEach(x => File.WriteAllBytes(Path.Combine(outPath, $"{x.Index:X3}.bin"), x.CompressedData));
        }

        /// <summary>
        /// Extract a single file from the archive
        /// </summary>
        /// <param name="inputArc"></param>
        /// <param name="outputFile"></param>
        /// <param name="width"></param>
        private static void ExtractSingle(string inputArc, string outputFile, int width = 256)
        {
            var inputName = new FileInfo(inputArc).Name;

            var outputFileInfo = new FileInfo(outputFile);
            var index = int.Parse(outputFileInfo.Name.Replace(outputFileInfo.Extension, ""), NumberStyles.HexNumber);

            var outFolder = outputFileInfo.DirectoryName;

            if (!Directory.Exists(outFolder))
                Directory.CreateDirectory(outFolder);

            try
            {
                if (inputName.ToLower().StartsWith("grp"))
                {
                    var grpArc = ArchiveFile<GraphicsFile>.FromFile(inputArc);

                    var file = grpArc.Files.FirstOrDefault(x => x.Index == index);

                    if (index == 0xE50)
                        file.InitializeFontFile();

                    file.GetImage(width).Save(outputFile, ImageFormat.Png);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Cannot extract file from {inputName} #{index:X3}");
            }
        }

        /// <summary>
        /// Replace graphics file by converting and compressing it too
        /// </summary>
        /// <param name="arc"></param>
        /// <param name="filePath"></param>
        /// <param name="index"></param>
        private static void ReplaceSingleGraphicsFile(ArchiveFile<FileInArchive> arc, string filePath, int index)
        {
            FileInArchive file = arc.Files.FirstOrDefault(x => x.Index == index);

            byte[] decompressedData = Helpers.DecompressData(file.CompressedData);

            GraphicsFile grpFile = new GraphicsFile();
            grpFile.Initialize(decompressedData, file.Offset);
            grpFile.Index = index;

            if (index == 0xE50)
            {
                grpFile.InitializeFontFile();
            }

            int transparentIndex = -1;
            Match transparentIndexMatch = Regex.Match(filePath, @"tidx(?<transparentIndex>\d+)");
            if (transparentIndexMatch.Success)
            {
                transparentIndex = int.Parse(transparentIndexMatch.Groups["transparentIndex"].Value);
            }

            grpFile.SetImage(filePath, setPalette: filePath.Contains("newpal"), transparentIndex: transparentIndex);

            arc.Files[arc.Files.IndexOf(file)] = grpFile;
        }

        private static void AddGraphicsFile(ArchiveFile<FileInArchive> arc, string filePath)
        {
            GraphicsFile graphicsFile = new();
            graphicsFile.NewFile(filePath);
            arc.AddFile(graphicsFile);
        }

        /// <summary>
        /// Replace any compressed file type
        /// </summary>
        /// <param name="arc"></param>
        /// <param name="filePath"></param>
        /// <param name="index"></param>
        private static void ReplaceSingleFile(ArchiveFile<FileInArchive> arc, string filePath, int index)
        {
            var file = arc.Files.FirstOrDefault(x => x.Index == index);
            file.Data = File.ReadAllBytes(filePath).ToList();
            file.Edited = true;
            arc.Files[arc.Files.IndexOf(file)] = file;
        }

        /// <summary>
        /// Replace file without decompressing the others first
        /// </summary>
        /// <param name="inputFolder"></param>
        /// <param name="inputArc"></param>
        /// <param name="outputArc"></param>
        private static void ReplaceFromFolder(string inputFolder, string inputArc, string outputArc)
        {
            var inputArcName = new FileInfo(inputArc).Name;

            try
            {
                var arc = ArchiveFile<FileInArchive>.FromFile(inputArc);
                var files = Directory.EnumerateFiles(inputFolder, "*.*", SearchOption.AllDirectories);

                foreach (var filePath in files)
                {
                    var index = GetIndexByFileName(filePath);

                    if (index.HasValue)
                    {
                        if (index >= 0)
                        {
                            Console.Write($"Replacing #{index:X3}... ");
                        }
                        else
                        {
                            Console.Write($"Adding new file from {Path.GetFileName(filePath)}... ");
                        }

                        try
                        {
                            if (Path.GetFileName(filePath).StartsWith("new", StringComparison.OrdinalIgnoreCase) && inputArcName.StartsWith("grp", StringComparison.OrdinalIgnoreCase))
                            {
                                AddGraphicsFile(arc, filePath);
                            }
                            else if (filePath.EndsWith(".bin", StringComparison.OrdinalIgnoreCase))
                            {
                                ReplaceSingleFile(arc, filePath, index.Value);
                            }
                            else if (inputArcName.StartsWith("grp", StringComparison.OrdinalIgnoreCase))
                            {
                                ReplaceSingleGraphicsFile(arc, filePath, index.Value);
                            }

                            Console.WriteLine("OK");
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"NOT OK: {e.Message}");
                        }
                    }
                }

                File.WriteAllBytes(outputArc, arc.GetBytes());
            }
            catch (Exception e)
            {
                Console.WriteLine($"Fatal error: {e.Message}");
            }
        }

        /// <summary>
        /// Imports a set of resx files from a folder given a specified language code and replaces the strings in a given evt file with them
        /// </summary>
        /// <param name="inputFolder"></param>
        /// <param name="languageCode"></param>
        /// <param name="inputArc"></param>
        /// <param name="outputArc"></param>
        private static void ImportResxFolder(string inputFolder, string languageCode, string fontMapPath, string inputArc, string outputArc)
        {
            try
            {
                ArchiveFile<EventFile> evtFile = ArchiveFile<EventFile>.FromFile(inputArc);
                if (Path.GetFileName(inputArc).StartsWith("dat"))
                {
                    evtFile.Files.ForEach(f => f.InitializeDialogueForSpecialFiles());
                }
                else
                {
                    evtFile.Files.Where(f => f.Index >= 580 && f.Index <= 581).ToList().ForEach(f => f.InitializeDialogueForSpecialFiles());
                }
                FontReplacementDictionary fontReplacementDictionary = new();
                fontReplacementDictionary.AddRange(JsonSerializer.Deserialize<List<FontReplacement>>(File.ReadAllText(fontMapPath)));
                evtFile.Files.ForEach(e => e.FontReplacementMap = fontReplacementDictionary);

                string[] files = Directory.GetFiles(inputFolder)
                            .Where(f => f.EndsWith($".{languageCode}.resx", StringComparison.OrdinalIgnoreCase)).ToArray();
                Console.WriteLine($"Replacing strings for {files.Length} files...");
                foreach (string file in files)
                {
                    int fileIndex = int.Parse(Regex.Match(file, @"(\d{3})\.[\w-]+\.resx").Groups[1].Value, NumberStyles.Integer);
                    evtFile.Files.FirstOrDefault(f => f.Index == fileIndex).ImportResxFile(file);
                }
                File.WriteAllBytes(outputArc, evtFile.GetBytes());
                Console.WriteLine("Done.");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Fatal error: {e.Message}");
            }
        }

        /// <summary>
        /// Patches overlays given an XML file with a Riivolution-style patch format and a path to the rominfo.xml file
        /// </summary>
        /// <param name="inputPatch">The Riivolution-style patch XML document</param>
        /// <param name="inputFolder">The folder containing unpatched overlays</param>
        /// <param name="outputFolder">The folder where patched overlays should be placed</param>
        /// <param name="romInfoPath">The path to the rominfo.xml that contains an overlay table (will be modified in place)</param>
        private static void PatchOverlays(string inputPatch, string inputFolder, string outputFolder, string romInfoPath)
        {
            List<Overlay> overlays = new();
            foreach (string file in Directory.GetFiles(inputFolder))
            {
                overlays.Add(new(file));
            }

            XmlSerializer serializer = new(typeof(OverlayPatchDocument));
            OverlayPatchDocument patchDoc = (OverlayPatchDocument)serializer.Deserialize(File.OpenRead(inputPatch));
            foreach (OverlayXml overlay in patchDoc.Overlays)
            {
                Overlay overlayToModify = overlays.First(o => o.Name == overlay.Name);
                Console.WriteLine($"Patching overlay '{overlay.Name}'...");
                foreach (OverlayPatchXml patch in overlay.Patches)
                {
                    overlayToModify.Patch((int)(patch.Location - overlay.Start), patch.Value);
                }
                if (overlay.appendFunction is not null)
                {
                    overlayToModify.Append(overlay.AppendFunction, romInfoPath);
                }
            }

            foreach (Overlay overlay in overlays)
            {
                overlay.Save(Path.Combine(outputFolder, $"{overlay.Name}.bin"));
            }
        }
    }
}
