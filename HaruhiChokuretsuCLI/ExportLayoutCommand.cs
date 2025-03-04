using HaruhiChokuretsuLib.Archive;
using HaruhiChokuretsuLib.Archive.Graphics;
using HaruhiChokuretsuLib.Util;
using Mono.Options;
using SkiaSharp;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace HaruhiChokuretsuCLI;

public class ExportLayoutCommand : Command
{
    private string _grp, _layoutName, _outputFile;
    private int _layoutIndex, _layoutStart, _layoutEnd;
    private int[] _indices;
    private string[] _names;
    private bool _json;
    public ExportLayoutCommand() : base("export-layout", "Exports a layout given a series of texture files")
    {
        Options = new()
        {
            { "g|grp=", "Input grp.bin", g => _grp = g },
            { "l|layout=", "Layout name or index", l =>
                {
                    if (!int.TryParse(l, out _layoutIndex))
                    {
                        _layoutIndex = -1;
                        _layoutName = l;
                    }
                } 
            },
            { "i|indices=", "List of comma-delimited file indices to build the layout with", i => _indices = i.Split(',').Select(ind => int.Parse(ind)).ToArray() },
            { "n|names=", "List of comma-delimited file names to build the layout with", n => _names = n.Split(",") },
            { "s|layout-start=", "Layout starting index", s => _layoutStart = int.Parse(s) },
            { "e|layout-end=", "Layout ending index", e => _layoutEnd = int.Parse(e) },
            { "o|output=", "Output PNG file location", o => _outputFile = o },
            { "j|json", "If specified, will output JSON of the layout entries as well", j => _json = true },
        };
    }

    public override int Invoke(IEnumerable<string> arguments)
    {
        Options.Parse(arguments);
        ConsoleLogger log = new();

        ArchiveFile<GraphicsFile> grp = ArchiveFile<GraphicsFile>.FromFile(_grp, log);

        GraphicsFile layout;
        if (_layoutIndex < 0)
        {
            layout = grp.GetFileByName(_layoutName);
        }
        else
        {
            layout = grp.GetFileByIndex(_layoutIndex);
        }

        List<GraphicsFile> layoutTextures;
        if (_indices is null || _indices.Length == 0)
        {
            layoutTextures = _names.Select(grp.GetFileByName).ToList();
        }
        else
        {
            layoutTextures = _indices.Select(grp.GetFileByIndex).ToList();
        }

        if (_layoutEnd == 0)
        {
            _layoutEnd = layout.LayoutEntries.Count;
        }

        (SKBitmap layoutImage, List<LayoutEntry> layoutEntries) = layout.GetLayout(layoutTextures, _layoutStart, _layoutEnd - _layoutStart, darkMode: false, preprocessedList: true);

        using FileStream layoutStream = new(_outputFile, FileMode.Create);
        layoutImage.Encode(layoutStream, SKEncodedImageFormat.Png, GraphicsFile.PNG_QUALITY);

        if (_json)
        {
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(_outputFile), $"{Path.GetFileNameWithoutExtension(_outputFile)}.json"), JsonSerializer.Serialize(layoutEntries, ReplaceCommand.SERIALIZER_OPTIONS));
        }

        return 0;
    }
}