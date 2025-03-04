using HaruhiChokuretsuLib.Archive;
using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Util;
using Mono.Options;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace HaruhiChokuretsuCLI
{
    public class ScriptCommandSearchCommand : Command
    {
        private string _evt;
        private int _id = -1;
        private string[] _parameters;

        private static readonly List<string> SECTIONS =
        [
            "Unknown01Section", // 0
            nameof(InteractableObjectsSection), // 1
            nameof(Unknown03Section), // 2
            "Unknown06Section", // 3
            nameof(Unknown07Section), // 4
            nameof(Unknown08Section), // 5
            nameof(Unknown09Section), // 6
            nameof(Unknown10Section), // 7
            nameof(StartingChibisSection), // 8
            nameof(MapCharactersSection), // 9
            nameof(ChoicesSection), // 10
        ];
        public ScriptCommandSearchCommand() : base("command-search", "Search script commands throughout evt.bin")
        {
            Options = new()
            {
                { "e|evt=", "Input evt.bin", e => _evt = e },
                { "i|id=", "Command mnemonic or ID (as hex number) to search for", i =>
                    {
                        if (!int.TryParse(i, NumberStyles.HexNumber, new CultureInfo("en-US"), out _id))
                        {
                            _id = EventFile.CommandsAvailable.First(c => c.Mnemonic.Equals(i, StringComparison.OrdinalIgnoreCase)).CommandId;
                        }
                    }
                },
                { "p|params|parameters=", "Comma-delimited set of param arguments (of the form 0=20,2=30,3!=4 etc.)", p => _parameters = p.Split(',') },
            };
        }

        public override int Invoke(IEnumerable<string> arguments)
        {
            Options.Parse(arguments);
            ConsoleLogger log = new();

            ArchiveFile<EventFile> evt = ArchiveFile<EventFile>.FromFile(_evt, log);

            foreach (EventFile eventFile in evt.Files)
            {
                foreach (ScriptSection scriptSection in eventFile.ScriptSections)
                {
                    foreach (ScriptCommandInvocation invocation in scriptSection.Objects)
                    {
                        if (invocation.Command.CommandId == _id)
                        {
                            bool match = true;
                            if ((_parameters?.Length ?? 0) > 0)
                            {
                                foreach (string p in _parameters)
                                {
                                    string[] split = p.Split('=');
                                    if (split[1].StartsWith("0x"))
                                    {
                                        split[1] = $"{int.Parse(split[1][2..], NumberStyles.HexNumber)}";
                                    }

                                    bool not = false;
                                    if (split[0].EndsWith("!"))
                                    {
                                        not = true;
                                        split[0] = split[0][..^1];
                                    }

                                    (int param, int value) = (int.Parse(split[0]), int.Parse(split[1]));
                                    if ((!not && invocation.Parameters[param] != value) ||
                                        (not && invocation.Parameters[param] == value))
                                    {
                                        match = false;
                                        break;
                                    }
                                }
                            }

                            if (match)
                            {
                                CommandSet.Out.WriteLine(
                                    $"{eventFile.Name} ({eventFile.Index}) has command {invocation.Command.Mnemonic} (0x{_id:X2}) " +
                                    $"@ section {eventFile.ScriptSections.IndexOf(scriptSection)} of {eventFile.ScriptSections.Count - 1} line {scriptSection.Objects.IndexOf(invocation)} of {scriptSection.Objects.Count - 1} with parameters: " +
                                    $"{string.Join(' ', invocation.Parameters.Select(b => $"{b}"))}");
                            }
                        }
                    }
                }
            }

            return 0;
        }
    }
}
