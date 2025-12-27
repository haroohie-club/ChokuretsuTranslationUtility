using HaruhiChokuretsuLib.Archive;
using HaruhiChokuretsuLib.Util;
using Mono.Options;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace HaruhiChokuretsuCLI;

public class HexSearchCommand : Command
{
    private string _archive;
    private readonly List<byte> _hexString = [];
    private bool _showHelp;

    public HexSearchCommand() : base("hex-search", "Searches an archive for a hex string")
    {
        Options = new()
        {
            "Searches the decompressed files in an archive for a particular hex string and returns files/locations where it is found",
            "Usage: HaruhiChokuretsuCLI hex-search -a [archive] -s [hexString]",
            "",
            { "a|archive=", "Archive to search", a => _archive = a },
            { "s|search=", "Hex string to search for", s =>
                {
                    for (int i = 0; i < s.Length; i += 2)
                    {
                        _hexString.Add(byte.Parse(s.Substring(i, 2), NumberStyles.HexNumber));
                    }
                }
            },
            { "h|help", "Shows this help screen", _ => _showHelp = true },
        };
    }

    public override int Invoke(IEnumerable<string> arguments)
    {
        Options.Parse(arguments);
        ConsoleLogger log = new();

        if (_showHelp || string.IsNullOrEmpty(_archive) || _hexString.Count == 0)
        {
            int returnValue = 0;
            if (string.IsNullOrEmpty(_archive))
            {
                CommandSet.Out.WriteLine("Archive not provided, please supply -a or --archive");
                returnValue = 1;
            }
            if (_hexString.Count == 0)
            {
                CommandSet.Out.WriteLine("Hex string not provided, please supply -s or --search");
                returnValue = 1;
            }
            Options.WriteOptionDescriptions(CommandSet.Out);
            return returnValue;
        }

        ArchiveFile<FileInArchive> archive = ArchiveFile<FileInArchive>.FromFile(_archive, log);

        Dictionary<int, List<int>> matches = new();
        foreach (FileInArchive file in archive.Files)
        {
            if (file.Data is null)
            {
                continue;
            }
            for (int i = 0; i < file.Data.Count - _hexString.Count; i++)
            {
                if (file.Data.Skip(i).Take(_hexString.Count).SequenceEqual(_hexString))
                {
                    if (!matches.ContainsKey(file.Index))
                    {
                        matches.Add(file.Index, []);
                    }
                    matches[file.Index].Add(i);
                }
            }
        }

        foreach (int file in matches.Keys)
        {
            CommandSet.Out.WriteLine($"Match(es) found in file #{file:X3}:");
            foreach (int index in matches[file])
            {
                CommandSet.Out.WriteLine($"\tAt offset 0x{index:X8}");
            }
        }

        return 0;
    }
}