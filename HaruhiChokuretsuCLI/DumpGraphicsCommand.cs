using System;
using System.Collections.Generic;
using System.IO;
using HaruhiChokuretsuLib.Archive;
using HaruhiChokuretsuLib.Archive.Graphics;
using HaruhiChokuretsuLib.Util;
using Mono.Options;
using SkiaSharp;

namespace HaruhiChokuretsuCLI;

public class DumpGraphicsCommand : Command
{
    private string _grp = string.Empty, _output = string.Empty;
    
    public DumpGraphicsCommand() : base("dump-graphics", "Dumps all texture/tile files from grp.bin")
    {
        Options = new()
        {
            { "i|g|input|grp=", "Input grp.bin", g => _grp = g },
            { "o|output=", "Output directory", o => _output = o },
        };
    }

    public override int Invoke(IEnumerable<string> arguments)
    {
        Options.Parse(arguments);

        if (!Directory.Exists(_output))
        {
            Directory.CreateDirectory(_output);
        }
        
        ArchiveFile<GraphicsFile> grp = ArchiveFile<GraphicsFile>.FromFile(_grp, new ConsoleLogger());
        foreach (GraphicsFile file in grp.Files)
        {
            if (file.Name.EndsWith("DNX"))
            {
                using FileStream fs = File.Create(Path.Combine(_output, $"{file.Name[..^3]}.png"));
                file.GetImage().Encode(fs, SKEncodedImageFormat.Png, GraphicsFile.PNG_QUALITY);
            }
        }
        
        return 0;
    }
}