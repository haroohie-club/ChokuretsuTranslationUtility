using HaruhiChokuretsuLib.Archive;
using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Archive.Graphics;
using HaruhiChokuretsuLib.Util;
using Mono.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Hashing;
using System.Linq;

namespace HaruhiChokuretsuCLI
{
    public class ExportMapCommand : Command
    {
        private string _dat, _grp, _outputFolder;
        private string[] _mapNames;
        private int[] _mapIndices, _maxLayoutIndices;
        private bool _allMaps = false, _listMaps = false, _animated = false;
        public ExportMapCommand() : base("export-map", "Export a map graphic from the game")
        {
            Options = new()
            {
                { "d|dat=", "DAT archive", d => _dat = d },
                { "g|grp=", "GRP archive", g => _grp = g },
                { "n|names|map-names=", "Comma-delimited list of map names", n => _mapNames = n.Split(',') },
                { "i|indices|map-indices=", "Comma-delimited list of map indices", i => _mapIndices = i.Split(',').Select(ind => int.Parse(ind)).ToArray() },
                { "m|max-layout-indices=", "Comma-delimited list of max layout indices", m => _maxLayoutIndices = m.Split(',').Select(ind => int.Parse(ind)).ToArray() },
                { "animated", "Indicates that maps with animation should be exported as WEBM (requires ffmpeg)", an => _animated = true },
                { "a|all-maps", "Indicates all maps should be exported", a => _allMaps = true },
                { "l|list-maps", "Lists maps available for export (still requires dat.bin)", l => _listMaps = true },
                { "o|output|output-folder|output-directory=", "Output directory", o => _outputFolder = o },
            };
        }

        public override int Invoke(IEnumerable<string> arguments)
        {
            Options.Parse(arguments);
            ConsoleLogger log = new();

            if (string.IsNullOrEmpty(_dat))
            {
                CommandSet.Out.WriteLine("ERROR: DAT archive must be provided with -d or --dat.");
                return 1;
            }
            ArchiveFile<DataFile> dat = ArchiveFile<DataFile>.FromFile(_dat, log);
            DataFile qmapDataFile = dat.GetFileByName("QMAPS");
            QMapFile qmapFile = qmapDataFile.CastTo<QMapFile>();
            dat.Files[dat.Files.IndexOf(qmapDataFile)] = qmapFile;
            List<string> mapFileNames = qmapFile.QMaps.Select(q => q.Name).ToList();

            if (_listMaps)
            {
                CommandSet.Out.WriteLine("Available maps:");
                CommandSet.Out.WriteLine(string.Join('\n', mapFileNames.Select(f => f[0..^2])));
                return 0;
            }

            if (string.IsNullOrEmpty(_grp))
            {
                CommandSet.Out.WriteLine("ERROR: GRP archive must be provided with -g or --grp.");
                return 1;
            }

            if (_mapNames is null && _mapIndices is null && !_allMaps)
            {
                CommandSet.Out.WriteLine("ERROR: Maps must be provided, either by name, index, or by indicating all should be exported through -a or --all-maps");
            }

            if (_mapNames is not null && _mapNames.Any(m => !mapFileNames.Select(f => f[0..^2]).Contains(m)))
            {
                foreach (string name in _mapNames)
                {
                    if (!mapFileNames.Contains($"{name}S"))
                    {
                        CommandSet.Out.WriteLine($"Map name {name} is not valid. Please check valid map names with -l or --list-maps");
                    }
                }

                return 1;
            }

            ArchiveFile<GraphicsFile> grp = ArchiveFile<GraphicsFile>.FromFile(_grp, log);

            for (int i = 1; i < dat.Files.Count + 1; i++)
            {
                if (mapFileNames.Contains(dat.Files[i - 1].Name))
                {
                    DataFile oldMapFile = dat.GetFileByIndex(i);
                    MapFile mapFile = dat.GetFileByIndex(i).CastTo<MapFile>();
                    dat.Files[dat.Files.IndexOf(oldMapFile)] = mapFile;
                }
            }

            List<string> mapsToExport = new();
            if (_allMaps)
            {
                mapsToExport.AddRange(mapFileNames);
            }
            else
            {
                if (_mapNames is not null)
                {
                    mapsToExport.AddRange(_mapNames.Select(n => $"{n}S"));
                }
                if (_mapIndices is not null)
                {
                    mapsToExport.AddRange(dat.Files.Where(f => _mapIndices.Contains(f.Index)).Select(f => f.Name));
                }
            }

            _outputFolder ??= "";
            if (!Directory.Exists(_outputFolder))
            {
                Directory.CreateDirectory(_outputFolder);
            }

            for (int m = 0; m < mapsToExport.Count; m++)
            {
                string mapName = mapsToExport[m];
                int maxLayoutIndex = -1;
                if (_maxLayoutIndices is not null)
                {
                    maxLayoutIndex = _maxLayoutIndices[m];
                }
                CommandSet.Out.WriteLine($"Exporting map for {mapName[0..^1]}...");
                MapFile map = dat.GetFileByName(mapName).CastTo<MapFile>();

                if (_animated && (map.Settings.PaletteAnimationFileIndex > 0 || map.Settings.ColorAnimationFileIndex > 0))
                {
                    GraphicsFile animation;
                    int replacementIndex = 1;
                    if (map.Settings.ColorAnimationFileIndex > 0)
                    {
                        animation = grp.GetFileByIndex(map.Settings.ColorAnimationFileIndex);
                    }
                    else
                    {
                        animation = grp.GetFileByIndex(map.Settings.PaletteAnimationFileIndex);
                    }
                    if (animation.Name.Contains("_BG_"))
                    {
                        replacementIndex = 0;
                    }
                    else if (animation.Name.Contains("_BOOK_"))
                    {
                        replacementIndex = 1;
                    }
                    else // OBJ
                    {
                        replacementIndex = 2;
                    }
                    GraphicsFile animatedTexture = grp.GetFileByIndex(map.Settings.TextureFileIndices[replacementIndex]);
                    List<GraphicsFile> graphicFrames = animation.GetAnimationFrames(animatedTexture);
                    Console.WriteLine($"Animated map will have {graphicFrames.Count} frames.");

                    List<SKBitmap> frames = new();
                    Dictionary<uint, SKBitmap> frameMapping = new();
                    for (int i = 0; i < graphicFrames.Count; i++)
                    {
                        uint frameHashCode = BitConverter.ToUInt32(Crc32.Hash(graphicFrames[i].Data.ToArray()));
                        if (!frameMapping.ContainsKey(frameHashCode))
                        {
                            Console.WriteLine($"Generating unique bitmap for frame {i}...");
                            frameMapping.Add(frameHashCode, map.GetMapImages(grp, maxLayoutIndex, graphicFrames[i], replacementIndex).mapBitmap);
                        }
                    }
                    foreach (GraphicsFile frame in graphicFrames)
                    {
                        frames.Add(frameMapping[BitConverter.ToUInt32(Crc32.Hash(frame.Data.ToArray()))]);
                    }

                    using Image<Rgba32> gif = new(frames.Max(f => f.Width), frames.Max(f => f.Height));
                    gif.Metadata.GetGifMetadata().RepeatCount = 0;

                    IEnumerable<Image<Rgba32>> gifFrames = frames.Select(f => Image.LoadPixelData<Rgba32>(f.Pixels.Select(c => new Rgba32(c.Red, c.Green, c.Blue, c.Alpha)).ToArray(), f.Width, f.Height));
                    foreach (Image<Rgba32> gifFrame in gifFrames.Skip(1))
                    {
                        gifFrame.Frames.RootFrame.Metadata.GetGifMetadata().FrameDelay = 2;
                        gif.Frames.AddFrame(gifFrame.Frames.RootFrame);
                    }
                    gif.Frames.RemoveFrame(0);

                    gif.SaveAsGif(Path.Combine(_outputFolder, $"{mapName[0..^1]}.gif"));
                }
                else
                {
                    (SKBitmap mapBitmap, SKBitmap bgBitmap) = map.GetMapImages(grp, maxLayoutIndex: maxLayoutIndex);

                    using FileStream mapStream = new(Path.Combine(_outputFolder, $"{mapName[0..^1]}.png"), FileMode.Create);
                    mapBitmap.Encode(mapStream, SKEncodedImageFormat.Png, GraphicsFile.PNG_QUALITY);

                    if (bgBitmap is not null)
                    {
                        using FileStream bgStream = new(Path.Combine(_outputFolder, $"{mapName[0..^1]}-BG.png"), FileMode.Create);
                        bgBitmap.Encode(bgStream, SKEncodedImageFormat.Png, GraphicsFile.PNG_QUALITY);
                    }
                    else
                    {
                        using FileStream bgStream = new(Path.Combine(_outputFolder, $"{mapName[0..^1]}-BG.png"), FileMode.Create);
                        map.GetBackgroundGradient().Encode(bgStream, SKEncodedImageFormat.Png, GraphicsFile.PNG_QUALITY);
                    }
                }
            }

            return 0;
        }
    }
}
