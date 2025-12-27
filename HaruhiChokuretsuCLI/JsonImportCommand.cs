using HaruhiChokuretsuLib.Archive;
using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Font;
using HaruhiChokuretsuLib.Util;
using Mono.Options;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace HaruhiChokuretsuCLI;

public partial class JsonImportCommand : Command
{
    private string _inputArchive, _inputFolder, _charmapFile, _outputArchive;
    private bool _showHelp, _isDat;

    public JsonImportCommand() : base("json-import", "Import messages from json files")
    {
        Options = new()
        {
            "Extracts json files from an archive (evt.bin or dat.bin)",
            "Usage: HaruhiChokuretsuCLI json-import -i [inputArchive] -o [outputFolder] [OPTIONS]",
            "",
            { "i|input-archive=", "Archive to extract file from", i => _inputArchive = i },
            { "f|input-folder=", "Folder path of json files to replace", f => _inputFolder = f},
            { "d|is-dat", "Whether input archive is dat.bin", _ => _isDat = true },
            { "c|charmap=", "Charset mapping file", c => _charmapFile = c },
            { "o|output-archive=", "Output file to save modified archive to", o => _outputArchive = o },
            { "h|help", "Shows this help screen", _ => _showHelp = true },
        };
    }

    public override int Invoke(IEnumerable<string> arguments)
    {
        Options.Parse(arguments);
        ConsoleLogger log = new();

        if (_showHelp || string.IsNullOrEmpty(_inputArchive) || string.IsNullOrEmpty(_inputFolder) || string.IsNullOrEmpty(_outputArchive))
        {
            int returnValue = 0;
            if (string.IsNullOrEmpty(_inputArchive))
            {
                CommandSet.Error.WriteLine("Input archive not provided, please supply -i or --input-archive");
                returnValue = 1;
            }
            if (string.IsNullOrEmpty(_inputFolder))
            {
                CommandSet.Error.WriteLine("Input folder not provided, please supply -f or --input-folder");
                returnValue = 1;
            }
            if (string.IsNullOrEmpty(_outputArchive))
            {
                CommandSet.Error.WriteLine("Output archive not provided, please supply -o or --output-archive");
                returnValue = 1;
            }
            Options.WriteOptionDescriptions(CommandSet.Out);
            return returnValue;
        }

        var char2ShiftJis = new Dictionary<char, char>();

        if (!string.IsNullOrEmpty(_charmapFile))
        {
            var jsonText = File.ReadAllText(_charmapFile);
            var content = JsonSerializer.Deserialize<List<FontReplacement>>(jsonText)!;
            foreach (var obj in content)
            {
                char2ShiftJis.Add(obj.ReplacedCharacter, obj.OriginalCharacter);
            }
        }
        var evtArchive = ArchiveFile<EventFile>.FromFile(_inputArchive, log);
        foreach (var file in evtArchive.Files)
        {
            if (_isDat) { file.InitializeDialogueForSpecialFiles(); }
            string path = Path.Combine(_inputFolder, $"{(_isDat ? "dat" : "evt")}_{file.Name}.json");
            if (!File.Exists(path)) { continue; }
            var input = JsonSerializer.Deserialize<List<List<string>>>(File.ReadAllText(path))!;
            if (file.DialogueLines.Count == 0)
            {
                if (!_isDat) { file.InitializeDialogueForSpecialFiles(); }
                if (file.DialogueLines.Count == 0)
                {
                    continue;
                }
            }
            CommandSet.Out.Write($"Importing: {(_isDat ? "dat" : "evt")}_{file.Name}.json...");
            for (int i = 0; i < file.DialogueLines.Count; i++)
            {
                var newTextOriginal = input[i][1];
                if (string.IsNullOrEmpty(newTextOriginal) || TrashPattern().IsMatch(newTextOriginal)) { continue; }
                var newText = "";
                foreach (char c in input[i][1])
                {
                    newText += char2ShiftJis.GetValueOrDefault(c, c);
                }
                if (file.DialogueLines[i].Text != newText)
                {
                    file.EditDialogueLine(i, newText);
                }
            }
            CommandSet.Out.WriteLine(file.Edited ? "OK" : "No change");
        }
        File.WriteAllBytes(_outputArchive, evtArchive.GetBytes());
        CommandSet.Out.WriteLine("Done.");

        return 0;
    }

    [GeneratedRegex(@"^[0-9a-zA-Z_ \.,!\?<>=\""\'\/\+\|\{\}&]+$|[\x00-\x09\x0b-\x1f\x7fｬｿﾜ]|^(?:＿々ー―～…|‘’“”（）《》「」『』－＝％☆♪|―+)$")]
    private static partial Regex TrashPattern();
}