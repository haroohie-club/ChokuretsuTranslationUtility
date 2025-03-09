using HaruhiChokuretsuLib.Archive;
using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Font;
using HaruhiChokuretsuLib.Util;
using HunspellSharp;
using Mono.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HaruhiChokuretsuCLI;

public class ImportResxCommand : Command
{
    private string _inputArchive, _outputArchive, _resxDirectory, _langCode, _fontOffsetMap, _spellcheckDir, _warningLogFile;
    private bool _showHelp;

    public ImportResxCommand() : base("import-resx", "Import RESX files to replace strings in an archive")
    {
        Options = new()
        {
            "Imports a directory of RESX files (filtered by language code) to replace strings in an archive",
            "Usage: HaruhiChokuretsuCLI import-resx -i [inputArchive] -o [outputArchive] -r [resxDirectory] -l [langCode] -f [fontOffsetMap] -s [dictionaries]",
            "",
            { "i|input-archive=", "Input archive to replace strings in", i => _inputArchive = i },
            { "o|output-archive=", "Output file to save modified archive to", o => _outputArchive = o },
            { "r|resx-directory=", "Directory where RESX files are located", r => _resxDirectory = r },
            { "l|lang-code=", "Language code to of desired string target language (used to filter RESX files)", l => _langCode = l },
            { "f|font-map=", "Font offset mapping file", f => _fontOffsetMap = f },
            { "s|spell-check=", "Directory of spellcheck dictionaries to use for spellchecking (optional)", s => _spellcheckDir = s },
            { "w|warning-log=", "Log file to write warnings to (optional, if not specified will write to the console", w => _warningLogFile = w },
            { "h|help", "Shows this help screen", h => _showHelp = true },
        };
    }

    public override int Invoke(IEnumerable<string> arguments)
    {
        return InvokeAsync(arguments).GetAwaiter().GetResult();
    }

    public async Task<int> InvokeAsync(IEnumerable<string> arguments)
    {
        Options.Parse(arguments);
        ConsoleLogger log = new();

        if (_showHelp || string.IsNullOrEmpty(_inputArchive) || string.IsNullOrEmpty(_outputArchive) || string.IsNullOrEmpty(_resxDirectory) || string.IsNullOrEmpty(_langCode) || string.IsNullOrEmpty(_fontOffsetMap))
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
            if (string.IsNullOrEmpty(_resxDirectory))
            {
                CommandSet.Out.WriteLine("RESX directory not provided, please supply -r or --resx-directory");
                returnValue = 1;
            }
            if (string.IsNullOrEmpty(_langCode))
            {
                CommandSet.Out.WriteLine("Language code not provided, please supply -l or --lang-code");
                returnValue = 1;
            }
            if (string.IsNullOrEmpty(_fontOffsetMap))
            {
                CommandSet.Out.WriteLine("Font offset map not provided, please supply -f or --font-map");
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

        bool isDat = Path.GetFileName(_inputArchive).StartsWith("dat", StringComparison.OrdinalIgnoreCase);
        ArchiveFile<EventFile> evtArchive = ArchiveFile<EventFile>.FromFile(_inputArchive, log, generic: isDat);

        if (isDat)
        {
            evtArchive.Files.ForEach(f => f.InitializeDialogueForSpecialFiles());
        }
        else
        {
            evtArchive.Files.Where(f => f.Index is >= 580 and <= 581 || f.Index == 106).ToList().ForEach(f => f.InitializeDialogueForSpecialFiles());
            EventFile evtVmFile = evtArchive.GetFileByIndex(589);
            if (evtVmFile is not null)
            {
                VoiceMapFile newVmFile = new();
                evtArchive.Files[evtArchive.Files.IndexOf(evtVmFile)] = evtVmFile.CastTo<VoiceMapFile>();
            }
        }

        FontReplacementDictionary fontReplacementDictionary = new();
        fontReplacementDictionary.AddRange(JsonSerializer.Deserialize<List<FontReplacement>>(await File.ReadAllTextAsync(_fontOffsetMap)));
        evtArchive.Files.ForEach(e => e.FontReplacementMap = fontReplacementDictionary);

        Hunspell spellcheck = null;
        if (!string.IsNullOrEmpty(_spellcheckDir))
        {
            // Do it this way to force UTF-8
            using StreamReader affixFs = File.OpenText(Path.Combine(_spellcheckDir, "index.aff"));
            using StreamReader dictFs = File.OpenText(Path.Combine(_spellcheckDir, "index.dic"));
            spellcheck = new(affixFs.BaseStream, dictFs.BaseStream);
        }

        string[] files = Directory.GetFiles(_resxDirectory, $"*.{_langCode}.resx");
        CommandSet.Out.WriteLine($"Replacing strings for {files.Length} files...");
            
        TextWriter warningLog = !string.IsNullOrEmpty(_warningLogFile) ? new StreamWriter(File.OpenWrite(_warningLogFile)) : CommandSet.Out;
            
        foreach (string file in files)
        {
            int fileIndex = int.Parse(Regex.Match(file, @"(?<index>\d{3})\.[\w-]+\.resx").Groups["index"].Value);
            if (fileIndex == 589)
            {
                EventFile evtVmFile = evtArchive.GetFileByIndex(fileIndex);
                VoiceMapFile vmFile = evtVmFile.CastTo<VoiceMapFile>();
                vmFile.FontReplacementMap = evtVmFile.FontReplacementMap;
                vmFile.ImportResxFile(file, spellcheck, warningLog);
                evtArchive.Files[evtArchive.Files.IndexOf(evtVmFile)] = vmFile;
            }
            else
            {
                evtArchive.GetFileByIndex(fileIndex).ImportResxFile(file, spellcheck, warningLog);
            }
        }
        await File.WriteAllBytesAsync(_outputArchive, evtArchive.GetBytes());
        CommandSet.Out.WriteLine("Done.");

        if (warningLog is StreamWriter writer)
        {
            await writer.FlushAsync();
            writer.Close();
        }

        return 0;
    }
}