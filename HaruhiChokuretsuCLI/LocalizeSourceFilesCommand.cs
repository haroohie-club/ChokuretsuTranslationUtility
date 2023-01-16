using HaruhiChokuretsuLib.Font;
using Mono.Options;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Resources.NetStandard;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace HaruhiChokuretsuCLI
{
    public class LocalizeSourceFilesCommand : Command
    {
        private string _sourceDir, _resxPath, _fontReplacementJson, _tempOut;
        public LocalizeSourceFilesCommand() : base("localize-sources", "Localize source files given a particular RESX")
        {
            Options = new()
            {
                { "s|sources=", "Sources directory", s => _sourceDir = s },
                { "r|resx=", "RESX containing keys", r => _resxPath = r },
                { "f|font-replacement=", "Font replacement map JSON", f => _fontReplacementJson = f },
                { "t|o|temp-output=", "Temporary holding place for the files", t => _tempOut = t },
            };
        }

        public override int Invoke(IEnumerable<string> arguments)
        {
            Options.Parse(arguments);

            if (string.IsNullOrEmpty(_sourceDir) || string.IsNullOrEmpty(_resxPath) || string.IsNullOrEmpty(_fontReplacementJson) || string.IsNullOrEmpty(_tempOut))
            {
                CommandSet.Out.WriteLine("ERROR: Must provide all parameters.");
                return 1;
            }

            if (!Directory.Exists(_tempOut))
            {
                Directory.CreateDirectory(_tempOut);
            }

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            Dictionary<string, string> placeholders = new();
            FontReplacementDictionary fontReplacementDictionary = new();
            fontReplacementDictionary.AddRange(JsonSerializer.Deserialize<List<FontReplacement>>(File.ReadAllText(_fontReplacementJson)));

            string resxContents = File.ReadAllText(_resxPath);
            resxContents = resxContents.Replace("System.Resources.ResXResourceWriter, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
                "System.Resources.NetStandard.ResXResourceWriter, System.Resources.NetStandard, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");
            resxContents = resxContents.Replace("System.Resources.ResXResourceReader, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
                "System.Resources.NetStandard.ResXResourceReader, System.Resources.NetStandard, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");
            TextReader textReader = new StringReader(resxContents);
            using ResXResourceReader resxReader = new(textReader);

            foreach (DictionaryEntry d in resxReader)
            {
                string final = string.Empty;
                foreach (char c in (string)d.Value)
                {
                    if (fontReplacementDictionary.ContainsKey(c))
                    {
                        char newCharacter = fontReplacementDictionary[c].OriginalCharacter;
                        if (c == '"' && (c == ' ' || c == '!' || c == '?' || c == '.' || c == '…' || c == '\n' || c == '#'))
                        {
                            newCharacter = '”';
                        }
                        final += newCharacter;
                    }
                }
                placeholders.Add((string)d.Key, final);
            }

            string[] files = Directory.GetFiles(_sourceDir, "*.s", SearchOption.AllDirectories);
            List<PlaceholderFile> filesWithPlaceholders = new();

            foreach (string file in files)
            {
                string content = File.ReadAllText(file);
                MatchCollection matches = Regex.Matches(content, @"\{\{(?<id>[\w\d_]+)\}\}");
                if (matches.Count > 0)
                {
                    PlaceholderFile placeholderFile = new() { OriginalLocation = file };
                    foreach (Match match in matches)
                    {
                        string id = match.Groups["id"].Value;
                        if (placeholders.ContainsKey(id))
                        {
                            content = content.Replace($"{{{{{id}}}}}", string.Join("", Encoding.GetEncoding("Shift-JIS").GetBytes(placeholders[id])
                                .Select(b => $"\\x{b:X2}")));
                        }
                    }

                    placeholderFile.Name = $"{Path.GetFileNameWithoutExtension(file)}-{Guid.NewGuid()}{Path.GetExtension(file)}";

                    filesWithPlaceholders.Add(placeholderFile);
                    File.Copy(file, Path.Combine(_tempOut, placeholderFile.Name));
                    File.WriteAllText(file, content);
                }
            }

            File.WriteAllText(Path.Combine(_tempOut, "map.json"), JsonSerializer.Serialize(filesWithPlaceholders));

            return 0;
        }

        [Serializable]
        private struct PlaceholderFile
        {
            public string OriginalLocation { get; set; }
            public string Name { get; set; }
        }
    }
}
