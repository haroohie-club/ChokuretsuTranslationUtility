using HaruhiChokuretsuLib.Archive;
using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Font;
using HaruhiChokuretsuLib.Util;
using Mono.Options;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace HaruhiChokuretsuCLI
{
    public class JsonExportCommand : Command
    {
        private string _inputArchive, _outputFolder, _charmapFile;
        private bool _showHelp, _isDat;

        public JsonExportCommand() : base("json-export", "Export messages into json files")
        {
            Options = new()
            {
                "Extracts json files from an archive (evt.bin or dat.bin)",
                "Usage: HaruhiChokuretsuCLI json-export -i [inputArchive] -o [outputFolder] [OPTIONS]",
                "",
                { "i|input-archive=", "Archive to extract file from", i => _inputArchive = i },
                { "o|output-folder=", "Folder path of extracted file", o => _outputFolder = o},
                { "d|is-dat", "Whether input archive is dat.bin", d => _isDat = true },
                { "c|charmap=", "Charset mapping file", c => _charmapFile = c },
                { "h|help", "Shows this help screen", h => _showHelp = true },
            };
        }

        public override int Invoke(IEnumerable<string> arguments)
        {
            Options.Parse(arguments);
            ConsoleLogger log = new();

            if (_showHelp || string.IsNullOrEmpty(_inputArchive) || string.IsNullOrEmpty(_outputFolder))
            {
                int returnValue = 0;
                if (string.IsNullOrEmpty(_inputArchive))
                {
                    CommandSet.Error.WriteLine("Input archive not provided, please supply -i or --input-archive");
                    returnValue = 1;
                }
                if (string.IsNullOrEmpty(_outputFolder))
                {
                    CommandSet.Error.WriteLine("Output folder not provided, please supply -o or --output-folder");
                    returnValue = 1;
                }
                Options.WriteOptionDescriptions(CommandSet.Out);
                return returnValue;
            }

            var shiftJis2Char = new Dictionary<char, char>();
            if (!string.IsNullOrEmpty(_charmapFile))
            {
                var json_text = File.ReadAllText(_charmapFile);
                var content = JsonSerializer.Deserialize<List<FontReplacement>>(json_text)!;
                foreach (var obj in content)
                {
                    shiftJis2Char.Add(obj.OriginalCharacter, obj.ReplacedCharacter);
                }
            }

            Directory.CreateDirectory(_outputFolder);
            var evtArchive = ArchiveFile<EventFile>.FromFile(_inputArchive, log);
            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            };

            foreach (var file in evtArchive.Files)
            {
                if (_isDat) { file.InitializeDialogueForSpecialFiles(); }
                if (file.DialogueLines.Count == 0)
                {
                    if (!_isDat) { file.InitializeDialogueForSpecialFiles(); }
                    if (file.DialogueLines.Count == 0)
                    {
                        continue;
                    }
                }
                var output = new List<List<string>>();
                foreach (var line in file.DialogueLines)
                {
                    var text = "";
                    foreach (char c in line.Text)
                    {
                        if (shiftJis2Char.TryGetValue(c, out char new_c))
                        {
                            text += new_c;
                        }
                        else
                        {
                            text += c;
                        }
                    }
                    output.Add(new List<string>() { line.SpeakerName, text });
                }
                var outputJson = Encoding.UTF8.GetString(JsonSerializer.SerializeToUtf8Bytes(output, jsonOptions));
                outputJson = outputJson.Replace(@"\u3000", "\u3000");
                File.WriteAllText(Path.Combine(_outputFolder, $"{(_isDat ? "dat" : "evt")}_{file.Name}.json"), outputJson);
            }

            return 0;
        }
    }
}
