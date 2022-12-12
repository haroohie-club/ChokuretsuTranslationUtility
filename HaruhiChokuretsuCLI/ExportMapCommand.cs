using HaruhiChokuretsuLib;
using HaruhiChokuretsuLib.Archive;
using HaruhiChokuretsuLib.Archive.Data;
using Mono.Options;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HaruhiChokuretsuCLI
{
    public class ExportMapCommand : Command
    {
        private string _dat, _grp, _outputFolder;
        private string[] _mapNames;
        private int[] _mapIndices;
        private bool _allMaps = false, _listMaps = false, _animated = false;
        public ExportMapCommand() : base("export-map", "Export a map graphic from the game")
        {
            Options = new()
            {
                { "d|dat=", "DAT archive", d => _dat = d },
                { "g|grp=", "GRP archive", g => _grp = g },
                { "n|names|map-names=", "Comma-delimited list of map names", n => _mapNames = n.Split(',') },
                { "i|indices|map-indices=", "Comma-delimited list of map indices", i => _mapIndices = i.Split(',').Select(ind => int.Parse(ind)).ToArray() },
                { "animated", "Indicates that maps with animation should be exported as APNGs", an => _animated = true },
                { "a|all-maps", "Indicates all maps should be exported", a => _allMaps = true },
                { "l|list-maps", "Lists maps available for export (still requires dat.bin)", l => _listMaps = true },
                { "o|output|output-folder|output-directory=", "Output directory", o => _outputFolder = o },
            };
        }

        public override int Invoke(IEnumerable<string> arguments)
        {
            Options.Parse(arguments);

            if (string.IsNullOrEmpty(_dat))
            {
                CommandSet.Out.WriteLine("ERROR: DAT archive must be provided with -d or --dat.");
                return 1;
            }
            ArchiveFile<DataFile> dat = ArchiveFile<DataFile>.FromFile(_dat);
            List<byte> qmapData = dat.Files.First(f => f.Name == "QMAPS").Data;
            List<string> mapFileNames = new();
            for (int i = 0; i < BitConverter.ToInt32(qmapData.Skip(0x10).Take(4).ToArray()); i++)
            {
                mapFileNames.Add(Encoding.ASCII.GetString(qmapData.Skip(BitConverter.ToInt32(qmapData.Skip(0x14 + i * 8).Take(4).ToArray())).TakeWhile(b => b != 0).ToArray()).Replace(".", ""));
            }
            mapFileNames = mapFileNames.Take(mapFileNames.Count - 1).ToList();

            if (_listMaps)
            {
                CommandSet.Out.WriteLine("Available maps:");
                CommandSet.Out.WriteLine(string.Join('\n', mapFileNames.Select(f => f[0..^1])));
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

            if (_mapNames is not null && _mapNames.Any(m => !mapFileNames.Select(f => f[0..^1]).Contains(m)))
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

            ArchiveFile<GraphicsFile> grp = ArchiveFile<GraphicsFile>.FromFile(_grp);

            for (int i = 1; i < dat.Files.Count + 1; i++)
            {
                if (mapFileNames.Contains(dat.Files[i - 1].Name))
                {
                    DataFile oldMapFile = dat.Files.First(f => f.Index == i);
                    MapFile mapFile = dat.Files.First(f => f.Index == i).CastTo<MapFile>();
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

            foreach (string mapName in mapsToExport)
            {
                CommandSet.Out.WriteLine($"Exporting map for {mapName[0..^1]}...");
                MapFile map = (MapFile)dat.Files.First(f => f.Name == mapName);

                if (_animated && map.Settings.SecondaryAnimationFileIndex > 0)
                {
                    GraphicsFile animatedTexture = grp.Files.First(f => f.Index == map.Settings.TextureFileIndices[1]);
                    GraphicsFile animation = grp.Files.First(f => f.Index == map.Settings.SecondaryAnimationFileIndex);
                    List<GraphicsFile> graphicFrames = animation.AnimationEntries[0].GetAnimationFrames(animatedTexture);
                    Console.WriteLine($"Animated map will have {graphicFrames.Count} frames.");

                    List<SKBitmap> frames = new();
                    for (int i = 0; i < graphicFrames.Count; i++)
                    {
                        Console.WriteLine($"Creating bitmap for frame {i + 1}...");
                        frames.Add(map.GetMapImages(grp, graphicFrames[i]).mapBitmap);
                    }

                    //byte[] encodedWebp = Helpers.EncodeAnimatedWebp(frames, 60 / animation.AnimationEntries[0].FramesPerTick);
                    //File.WriteAllBytes(Path.Combine(_outputFolder, $"{mapName[0..^1]}.webp"), encodedWebp);
                    //Image image = Helpers.GetEncodedAnimatedWebp(frames, 60 / animation.AnimationEntries[0].FramesPerTick);
                    //image.SaveAsGif(Path.Combine(_outputFolder, $"{mapName[0..^1]}.webp"));

                    for (int i = 0; i < frames.Count; i++)
                    {
                        using FileStream mapStream = new(Path.Combine(_outputFolder, $"{mapName[0..^1]}-{i}.png"), FileMode.Create);
                        frames[i].Encode(mapStream, SKEncodedImageFormat.Png, GraphicsFile.PNG_QUALITY);
                    }
                }
                else
                {
                    (SKBitmap mapBitmap, SKBitmap bgBitmap) = map.GetMapImages(grp);

                    using FileStream mapStream = new(Path.Combine(_outputFolder, $"{mapName[0..^1]}.png"), FileMode.Create);
                    mapBitmap.Encode(mapStream, SKEncodedImageFormat.Png, GraphicsFile.PNG_QUALITY);

                    if (bgBitmap is not null)
                    {
                        using FileStream bgStream = new(Path.Combine(_outputFolder, $"{mapName[0..^1]}-BG.png"), FileMode.Create);
                        bgBitmap.Encode(bgStream, SKEncodedImageFormat.Png, GraphicsFile.PNG_QUALITY);
                    }
                }
            }

            return 0;
        }
    }
}
