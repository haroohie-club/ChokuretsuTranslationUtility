using HaruhiChokuretsuLib.Archive;
using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Font;
using HaruhiChokuretsuLib.Util;
using Mono.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace HaruhiChokuretsuCLI
{
    public class ExportScriptToWikiTableCommand : Command
    {
        private string _jaEvtPath, _enEvtPath, _scriptName, _charmapFile, _outputFile;
        private int _scriptIndex;
        private FontReplacementDictionary _fontReplacement;

        public ExportScriptToWikiTableCommand() : base("export-script-to-wiki-table", "Exports a script file to a wiki table")
        {
            Options = new()
            {
                { "j|ja-evt=", "Path to Japanese evt.bin file", j => _jaEvtPath = j },
                { "e|en-evt=", "Path to English evt.bin file", e => _enEvtPath = e },
                { "n|name=", "Name of script to export", s => _scriptName = s },
                { "i|index=", "Index of script to export", i => _scriptIndex = int.Parse(i) },
                { "c|charmap=", "Charmap file", c => _charmapFile = c },
                { "o|output=", "Output file to write to", o => _outputFile = o },
            };
        }

        public override int Invoke(IEnumerable<string> arguments)
        {
            Options.Parse(arguments);

            ConsoleLogger log = new();
            ArchiveFile<EventFile> jaEvt = ArchiveFile<EventFile>.FromFile(_jaEvtPath, log);
            ArchiveFile<EventFile> enEvt = ArchiveFile<EventFile>.FromFile(_enEvtPath, log);
            _fontReplacement = new();
            _fontReplacement.AddRange(JsonSerializer.Deserialize<List<FontReplacement>>(File.ReadAllText(_charmapFile)));

            EventFile jaScript, enScript;
            if (!string.IsNullOrEmpty(_scriptName))
            {
                jaScript = jaEvt.GetFileByName(_scriptName);
                enScript = enEvt.GetFileByName(_scriptName);
            }
            else
            {
                jaScript = jaEvt.GetFileByIndex(_scriptIndex);
                enScript = enEvt.GetFileByIndex(_scriptIndex);
            }

            EventFile jaTopics = jaEvt.GetFileByName("TOPICS");
            jaTopics.InitializeTopicFile();
            EventFile enTopics = enEvt.GetFileByName("TOPICS");
            enTopics.InitializeTopicFile();

            List<WikiTableEntry> entries = [];
            for (int i = 0; i < jaScript.ScriptSections.Count; i++)
            {
                entries.Add(new() { SectionName = jaScript.ScriptSections[i].Name });
                for (int j = 0; j < jaScript.ScriptSections[i].Objects.Count; j++)
                {
                    switch (Enum.Parse<EventFile.CommandVerb>(jaScript.ScriptSections[i].Objects[j].Command.Mnemonic))
                    {
                        case EventFile.CommandVerb.DIALOGUE:
                            DialogueLine jaLine = jaScript.DialogueSection.Objects[jaScript.ScriptSections[i].Objects[j].Parameters[0]];
                            DialogueLine enLine = enScript.DialogueSection.Objects[jaScript.ScriptSections[i].Objects[j].Parameters[0]];
                            if (string.IsNullOrEmpty(jaLine.Text) || string.IsNullOrEmpty(enLine.Text))
                            {
                                continue;
                            }
                            entries.Add(new() { Character = jaLine.Speaker, JapaneseLine = jaLine.Text, EnglishLine = ReplaceString(enLine.Text) });
                            break;
                        case EventFile.CommandVerb.SELECT:
                            if (entries.Count > 0)
                            {
                                StringBuilder selectSb = new();
                                int numChoices = jaScript.ScriptSections[i].Objects[j].Parameters.Take(4).Count(p => p > 0);
                                selectSb.Append($"After this line, the player is presented with {numChoices} choices: ");
                                for (int k = 0; k < numChoices; k++)
                                {
                                    if (k == numChoices - 1)
                                    {
                                        selectSb.Append("or ");
                                    }
                                    selectSb.Append($"{jaScript.ChoicesSection.Objects[jaScript.ScriptSections[i].Objects[j].Parameters[k]].Text} (\"{ReplaceString(enScript.ChoicesSection.Objects[jaScript.ScriptSections[i].Objects[j].Parameters[k]].Text)}\") " +
                                        $"which jumps to {jaScript.LabelsSection.Objects.FirstOrDefault(l => l.Id == jaScript.ChoicesSection.Objects[jaScript.ScriptSections[i].Objects[j].Parameters[k]].Id)?.Name ?? "an undefined section"}");
                                    if (numChoices > 2 && k != numChoices - 1)
                                    {
                                        selectSb.Append(", ");
                                    }
                                    else if (numChoices == 2)
                                    {
                                        selectSb.Append(" ");
                                    }
                                }
                                if (!string.IsNullOrEmpty(entries.Last().Notes))
                                {
                                    entries.Last().Notes += "<br/>";
                                }
                                entries.Last().Notes += selectSb.ToString();
                            }
                            break;
                        case EventFile.CommandVerb.GOTO:
                            if (entries.Count > 0)
                            {
                                if (!string.IsNullOrEmpty(entries.Last().Notes))
                                {
                                    entries.Last().Notes += "<br/>";
                                }
                                entries.Last().Notes += $"After this line, the script jumps to {jaScript.LabelsSection.Objects.FirstOrDefault(l => l.Id == jaScript.ScriptSections[i].Objects[j].Parameters[0])?.Name ?? "an undefined section"}";
                            }
                            break;
                        case EventFile.CommandVerb.TOPIC_GET:

                            if (entries.Count > 0)
                            {
                                if (!string.IsNullOrEmpty(entries.Last().Notes))
                                {
                                    entries.Last().Notes += "<br/>";
                                }
                                short topicId = jaScript.ScriptSections[i].Objects[j].Parameters[0];
                                entries.Last().Notes += $"After this line, the player will receive topic #{topicId}";
                                Topic jaTopic = jaTopics.Topics.FirstOrDefault(t => t.Id == topicId);
                                if (jaTopic is not null)
                                {
                                    Topic enTopic = enTopics.Topics.FirstOrDefault(t => t.Id == topicId);
                                    entries.Last().Notes += $" ({jaTopic.Title}, \"{enTopic.Title}\")";
                                }
                                else
                                {
                                    entries.Last().Notes += $"; however, no such topic exists in the game.";
                                }
                            }
                            break;

                    }
                }
            }

            StringBuilder sb = new();
            sb.AppendLine("{| class=\"mw-collapsible mw-collapsed wikitable\" style=\"text-align: center; margin-left: auto; margin-right: auto\"");
            sb.AppendLine("|-");
            sb.AppendLine($"! colspan=4 | {jaScript.Name[..^1]}");
            sb.AppendLine("|-");
            sb.AppendLine("! Speaker !! Original Japanese !! English !! style=\"width: 200pt;\" | Notes");
            sb.AppendLine("|-");
            foreach (WikiTableEntry entry in entries)
            {
                sb.AppendLine(entry.GetWikiTableMarkup());
                sb.AppendLine("|-");
            }
            sb.AppendLine("|}");

            File.WriteAllText(_outputFile, sb.ToString());

            return 0;
        }

        private string ReplaceString(string s)
        {
            for (int i = 0; i < s.Length; i++)
            {
                FontReplacement replacement = _fontReplacement.ReverseLookup(s[i]);
                if (replacement is not null)
                {
                    s = s.Remove(i, 1);
                    s = s.Insert(i, $"{replacement.ReplacedCharacter}");
                }
            }
            return s;
        }

        private class WikiTableEntry
        {
            public string SectionName { get; set; }
            public Speaker Character { get; set; }
            public string JapaneseLine { get; set; }
            public string EnglishLine { get; set; }
            public string Notes { get; set; } = string.Empty;

            public string GetWikiTableMarkup()
            {
                if (!string.IsNullOrEmpty(SectionName))
                {
                    return $"| colspan=4 | '''{SectionName}'''";
                }
                return $"| {SpeakerToName(Character)} || {JapaneseLine.Replace("\n", "<br/>")} || {EnglishLine.Replace("\n", "<br/>")} || {Notes}";
            }

            private static string SpeakerToName(Speaker speaker)
            {
                return speaker switch
                {
                    Speaker.MIKURU => "Asahina",
                    Speaker.KYON_SIS => "Kyon's Sister",
                    Speaker.CLUB_PRES => "President",
                    Speaker.CLUB_MEM_A => "Member A",
                    Speaker.CLUB_MEM_B => "Member B",
                    Speaker.CLUB_MEM_C => "Member C",
                    Speaker.CLUB_MEM_D => "Member D",
                    Speaker.OLD_LADY => "Old Lady",
                    Speaker.STRAY_CAT => "Cat",
                    Speaker.BASEBALL_CAPTAIN => "Baseball Captain",
                    Speaker.FAKE_HARUHI => "Fake Haruhi",
                    Speaker.UNKNOWN => "???",
                    Speaker.MAIL => "Email",
                    _ => $"{speaker.ToString()[0]}{speaker.ToString().ToLower()[1..]}",
                };
            }
        }
    }
}
