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
        private int _id = -1, _section = -1;
        private string[] _parameters;

        private static readonly List<string> SECTIONS = new()
        {
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
        };
        public ScriptCommandSearchCommand() : base("command-search", "Search script commands or sections throughout evt.bin")
        {
            Options = new()
            {
                { "e|evt=", "Input evt.bin", e => _evt = e },
                { "s|section=", "Section to search within", s => _section = SECTIONS.IndexOf(s) },
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
                if (_id > 0)
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
                                            split[0] = split[0][0..^1];
                                        }
                                        (int param, int value) = (int.Parse(split[0]), int.Parse(split[1]));
                                        if ((!not && invocation.Parameters[param] != value) || (not && invocation.Parameters[param] == value))
                                        {
                                            match = false;
                                            break;
                                        }
                                    }
                                }
                                if (match)
                                {
                                    CommandSet.Out.WriteLine($"{eventFile.Name} ({eventFile.Index}) has command {invocation.Command.Mnemonic} (0x{_id:X2}) " +
                                        $"@ section {eventFile.ScriptSections.IndexOf(scriptSection)} of {eventFile.ScriptSections.Count - 1} line {scriptSection.Objects.IndexOf(invocation)} of {scriptSection.Objects.Count - 1} with parameters: " +
                                        $"{string.Join(' ', invocation.Parameters.Select(b => $"{b}"))}");
                                }
                            }
                        }
                    }
                }
                else
                {
                    switch (_section)
                    {
                        case 0:
                            var genericSection01 = eventFile.SectionPointersAndCounts.FirstOrDefault(s => (s.Section?.Name ?? "") == "UNKNOWNSECTION01");
                            if (genericSection01 is not null)
                            {
                                var unknown01Section = (IntegerSection)Convert.ChangeType(genericSection01.Section, typeof(IntegerSection));
                                foreach (var unknown01 in unknown01Section.Objects)
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
                                                split[0] = split[0][0..^1];
                                            }
                                            (int param, int value) = (int.Parse(split[0]), int.Parse(split[1]));
                                            if ((!not && unknown01 != value) || (not && unknown01 == value))
                                            {
                                                match = false;
                                                break;
                                            }
                                        }
                                    }
                                    if (match)
                                    {
                                        CommandSet.Out.WriteLine($"{eventFile.Name} ({eventFile.Index}) has Unknown Section 01: {unknown01}");
                                    }
                                }
                            }
                            break;
                        case 1:
                            var genericInteractableObjectsSection = eventFile.SectionPointersAndCounts.FirstOrDefault(s => (s.Section?.Name ?? "") == "INTERACTABLEOBJECTS");
                            if (genericInteractableObjectsSection is not null)
                            {
                                var interactableObjectsSection = (InteractableObjectsSection)Convert.ChangeType(genericInteractableObjectsSection.Section, typeof(InteractableObjectsSection));
                                foreach (var interactableObject in interactableObjectsSection.Objects)
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
                                                split[0] = split[0][0..^1];
                                            }
                                            (int param, int value) = (int.Parse(split[0]), int.Parse(split[1]));
                                            switch (param)
                                            {
                                                case 1:
                                                    if ((!not && interactableObject.ObjectId != value) || (not && interactableObject.ObjectId == value))
                                                    {
                                                        match = false;
                                                    }
                                                    break;
                                                case 2:
                                                    if ((!not && interactableObject.ScriptBlock != value) || (not && interactableObject.ScriptBlock == value))
                                                    {
                                                        match = false;
                                                    }
                                                    break;
                                                case 3:
                                                    if ((!not && interactableObject.Padding != value) || (not && interactableObject.Padding == value))
                                                    {
                                                        match = false;
                                                    }
                                                    break;
                                            }
                                            if (!match)
                                            {
                                                break;
                                            }
                                        }
                                    }
                                    if (match)
                                    {
                                        CommandSet.Out.WriteLine($"{eventFile.Name} ({eventFile.Index}) has Unknown Section 02: {interactableObject.ObjectId} {interactableObject.ScriptBlock} {interactableObject.Padding}");
                                    }
                                }
                            }
                            break;

                        case 2:
                            var genericSection3 = eventFile.SectionPointersAndCounts.FirstOrDefault(s => (s.Section?.Name ?? "") == "UNKNOWNSECTION03");
                            if (genericSection3 is not null)
                            {
                                var unknown03Section = (Unknown03Section)Convert.ChangeType(genericSection3.Section, typeof(Unknown03Section));
                                foreach (var unknown03 in unknown03Section.Objects)
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
                                                split[0] = split[0][0..^1];
                                            }
                                            (int param, int value) = (int.Parse(split[0]), int.Parse(split[1]));
                                            switch (param)
                                            {
                                                case 1:
                                                    if ((!not && unknown03.UnknownInt1 != value) || (not && unknown03.UnknownInt1 == value))
                                                    {
                                                        match = false;
                                                    }
                                                    break;
                                                case 2:
                                                    if ((!not && unknown03.UnknownInt2 != value) || (not && unknown03.UnknownInt2 == value))
                                                    {
                                                        match = false;
                                                    }
                                                    break;
                                                case 3:
                                                    if ((!not && unknown03.UnknownInt3 != value) || (not && unknown03.UnknownInt3 == value))
                                                    {
                                                        match = false;
                                                    }
                                                    break;
                                            }
                                            if (!match)
                                            {
                                                break;
                                            }
                                        }
                                    }
                                    if (match)
                                    {
                                        CommandSet.Out.WriteLine($"{eventFile.Name} ({eventFile.Index}) has Unknown Section 03: {unknown03.UnknownInt1} {unknown03.UnknownInt2} {unknown03.UnknownInt3}");
                                    }
                                }
                            }
                            break;

                        case 3:
                            var genericSection06 = eventFile.SectionPointersAndCounts.FirstOrDefault(s => (s.Section?.Name ?? "") == "UNKNOWNSECTION06");
                            if (genericSection06 is not null)
                            {
                                var unknown06Section = (IntegerSection)Convert.ChangeType(genericSection06.Section, typeof(IntegerSection));
                                foreach (var unknown06 in unknown06Section.Objects)
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
                                                split[0] = split[0][0..^1];
                                            }
                                            (int param, int value) = (int.Parse(split[0]), int.Parse(split[1]));
                                            if ((!not && unknown06 != value) || (not && unknown06 == value))
                                            {
                                                match = false;
                                                break;
                                            }
                                        }
                                    }
                                    if (match)
                                    {
                                        CommandSet.Out.WriteLine($"{eventFile.Name} ({eventFile.Index}) has Unknown Section 06: {unknown06}");
                                    }
                                }
                            }
                            break;

                        case 4:
                            var genericSection07 = eventFile.SectionPointersAndCounts.FirstOrDefault(s => (s.Section?.Name ?? "") == "UNKNOWNSECTION07");
                            if (genericSection07 is not null)
                            {
                                var unknown07Section = (Unknown07Section)Convert.ChangeType(genericSection07.Section, typeof(Unknown07Section));
                                foreach (var unknown07 in unknown07Section.Objects)
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
                                                split[0] = split[0][0..^1];
                                            }
                                            (int param, int value) = (int.Parse(split[0]), int.Parse(split[1]));

                                            switch (param)
                                            {
                                                case 1:
                                                    if ((!not && unknown07.UnknownShort1 != value) || (not && unknown07.UnknownShort1 == value))
                                                    {
                                                        match = false;
                                                    }
                                                    break;
                                                case 2:
                                                    if ((!not && unknown07.UnknownShort2 != value) || (not && unknown07.UnknownShort2 == value))
                                                    {
                                                        match = false;
                                                    }
                                                    break;
                                            }
                                        }
                                    }
                                    if (match)
                                    {
                                        CommandSet.Out.WriteLine($"{eventFile.Name} ({eventFile.Index}) has Unknown Section 07: {unknown07.UnknownShort1} {unknown07.UnknownShort2}");
                                    }
                                }
                            }
                            break;

                        case 5:
                            var genericSection08 = eventFile.SectionPointersAndCounts.FirstOrDefault(s => (s.Section?.Name ?? "") == "UNKNOWNSECTION08");
                            if (genericSection08 is not null)
                            {
                                var unknown08Section = (Unknown08Section)Convert.ChangeType(genericSection08.Section, typeof(Unknown08Section));
                                foreach (var unknown08 in unknown08Section.Objects)
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
                                                split[0] = split[0][0..^1];
                                            }
                                            (int param, int value) = (int.Parse(split[0]), int.Parse(split[1]));

                                            switch (param)
                                            {
                                                case 1:
                                                    if ((!not && unknown08.UnknownInt1 != value) || (not && unknown08.UnknownInt1 == value))
                                                    {
                                                        match = false;
                                                    }
                                                    break;
                                                case 2:
                                                    if ((!not && unknown08.UnknownInt2 != value) || (not && unknown08.UnknownInt2 == value))
                                                    {
                                                        match = false;
                                                    }
                                                    break;
                                                case 3:
                                                    if ((!not && unknown08.UnknownInt3 != value) || (not && unknown08.UnknownInt3 == value))
                                                    {
                                                        match = false;
                                                    }
                                                    break;
                                                case 4:
                                                    if ((!not && unknown08.UnknownInt4 != value) || (not && unknown08.UnknownInt4 == value))
                                                    {
                                                        match = false;
                                                    }
                                                    break;
                                            }
                                        }
                                    }
                                    if (match)
                                    {
                                        CommandSet.Out.WriteLine($"{eventFile.Name} ({eventFile.Index}) has Unknown Section 08: {unknown08.UnknownInt1} {unknown08.UnknownInt2} {unknown08.UnknownInt3} {unknown08.UnknownInt4}");
                                    }
                                }
                            }
                            break;

                        case 6:
                            var genericSection09 = eventFile.SectionPointersAndCounts.FirstOrDefault(s => (s.Section?.Name ?? "") == "UNKNOWNSECTION09");
                            if (genericSection09 is not null)
                            {
                                var unknown09Section = (Unknown09Section)Convert.ChangeType(genericSection09.Section, typeof(Unknown09Section));
                                foreach (var unknown09 in unknown09Section.Objects)
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
                                                split[0] = split[0][0..^1];
                                            }
                                            (int param, int value) = (int.Parse(split[0]), int.Parse(split[1]));

                                            switch (param)
                                            {
                                                case 1:
                                                    if ((!not && unknown09.UnknownInt1 != value) || (not && unknown09.UnknownInt1 == value))
                                                    {
                                                        match = false;
                                                    }
                                                    break;
                                                case 2:
                                                    if ((!not && unknown09.UnknownInt2 != value) || (not && unknown09.UnknownInt2 == value))
                                                    {
                                                        match = false;
                                                    }
                                                    break;
                                            }
                                        }
                                    }
                                    if (match)
                                    {
                                        CommandSet.Out.WriteLine($"{eventFile.Name} ({eventFile.Index}) has Unknown Section 09: {unknown09.UnknownInt1} {unknown09.UnknownInt2}");
                                    }
                                }
                            }
                            break;

                        case 7:
                            var genericSection10 = eventFile.SectionPointersAndCounts.FirstOrDefault(s => (s.Section?.Name ?? "") == "UNKNOWNSECTION10");
                            if (genericSection10 is not null)
                            {
                                var unknown10Section = (Unknown10Section)Convert.ChangeType(genericSection10.Section, typeof(Unknown10Section));
                                foreach (var unknown10 in unknown10Section.Objects)
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
                                                split[0] = split[0][0..^1];
                                            }
                                            (int param, int value) = (int.Parse(split[0]), int.Parse(split[1]));

                                            switch (param)
                                            {
                                                case 1:
                                                    if ((!not && unknown10.UnknownInt1 != value) || (not && unknown10.UnknownInt1 == value))
                                                    {
                                                        match = false;
                                                    }
                                                    break;
                                                case 2:
                                                    if ((!not && unknown10.UnknownInt2 != value) || (not && unknown10.UnknownInt2 == value))
                                                    {
                                                        match = false;
                                                    }
                                                    break;
                                            }
                                        }
                                    }
                                    if (match)
                                    {
                                        CommandSet.Out.WriteLine($"{eventFile.Name} ({eventFile.Index}) has Unknown Section 10: {unknown10.UnknownInt1} {unknown10.UnknownInt2}");
                                    }
                                }
                            }
                            break;

                        case 8:
                            var startingChibisGeneric = eventFile.SectionPointersAndCounts.FirstOrDefault(s => (s.Section?.Name ?? "") == "STARTINGCHIBIS");
                            if (startingChibisGeneric is not null)
                            {
                                var startingChibisSection = (StartingChibisSection)Convert.ChangeType(startingChibisGeneric.Section, typeof(StartingChibisSection));
                                foreach (var startingChibi in startingChibisSection.Objects)
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
                                                split[0] = split[0][0..^1];
                                            }
                                            (int param, int value) = (int.Parse(split[0]), int.Parse(split[1]));

                                            switch (param)
                                            {
                                                case 1:
                                                    if ((!not && startingChibi.ChibiIndex != value) || (not && startingChibi.ChibiIndex == value))
                                                    {
                                                        match = false;
                                                    }
                                                    break;
                                                case 2:
                                                    if ((!not && startingChibi.UnknownShort2 != value) || (not && startingChibi.UnknownShort2 == value))
                                                    {
                                                        match = false;
                                                    }
                                                    break;
                                                case 3:
                                                    if ((!not && startingChibi.UnknownShort3 != value) || (not && startingChibi.UnknownShort3 == value))
                                                    {
                                                        match = false;
                                                    }
                                                    break;
                                                case 4:
                                                    if ((!not && startingChibi.UnknownShort4 != value) || (not && startingChibi.UnknownShort4 == value))
                                                    {
                                                        match = false;
                                                    }
                                                    break;
                                                case 5:
                                                    if ((!not && startingChibi.UnknownShort5 != value) || (not && startingChibi.UnknownShort5 == value))
                                                    {
                                                        match = false;
                                                    }
                                                    break;
                                                case 6:
                                                    if ((!not && startingChibi.UnknownShort6 != value) || (not && startingChibi.UnknownShort6 == value))
                                                    {
                                                        match = false;
                                                    }
                                                    break;
                                            }
                                        }
                                    }
                                    if (match)
                                    {
                                        CommandSet.Out.WriteLine($"{eventFile.Name} ({eventFile.Index}) has Starting Chibis Section: {startingChibi.ChibiIndex} {startingChibi.UnknownShort2} {startingChibi.UnknownShort3} {startingChibi.UnknownShort4} {startingChibi.UnknownShort5} {startingChibi.UnknownShort6}");
                                    }
                                }
                            }
                            break;

                        case 9:
                            var mapCharactersGeneric = eventFile.SectionPointersAndCounts.FirstOrDefault(s => (s.Section?.Name ?? "") == "MAPCHARACTERS");
                            if (mapCharactersGeneric is not null)
                            {
                                var mapCharactersSection = (MapCharactersSection)Convert.ChangeType(mapCharactersGeneric.Section, typeof(MapCharactersSection));
                                foreach (var mapCharacter in mapCharactersSection.Objects)
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
                                                split[0] = split[0][0..^1];
                                            }
                                            (int param, int value) = (int.Parse(split[0]), int.Parse(split[1]));

                                            switch (param)
                                            {
                                                case 1:
                                                    if ((!not && mapCharacter.CharacterIndex != value) || (not && mapCharacter.CharacterIndex == value))
                                                    {
                                                        match = false;
                                                    }
                                                    break;
                                                case 2:
                                                    if ((!not && mapCharacter.FacingDirection != value) || (not && mapCharacter.FacingDirection == value))
                                                    {
                                                        match = false;
                                                    }
                                                    break;
                                                case 3:
                                                    if ((!not && mapCharacter.X != value) || (not && mapCharacter.X == value))
                                                    {
                                                        match = false;
                                                    }
                                                    break;
                                                case 4:
                                                    if ((!not && mapCharacter.Y != value) || (not && mapCharacter.Y == value))
                                                    {
                                                        match = false;
                                                    }
                                                    break;
                                                case 5:
                                                    if ((!not && mapCharacter.TalkScriptBlock != value) || (not && mapCharacter.TalkScriptBlock == value))
                                                    {
                                                        match = false;
                                                    }
                                                    break;
                                                case 6:
                                                    if ((!not && mapCharacter.Padding != value) || (not && mapCharacter.Padding == value))
                                                    {
                                                        match = false;
                                                    }
                                                    break;
                                            }
                                        }
                                    }
                                    if (match)
                                    {
                                        CommandSet.Out.WriteLine($"{eventFile.Name} ({eventFile.Index}) has Map Characters Section: {mapCharacter.CharacterIndex} {mapCharacter.FacingDirection} {mapCharacter.X} {mapCharacter.Y} {mapCharacter.TalkScriptBlock} {mapCharacter.Padding}");
                                    }
                                }
                            }
                            break;

                        case 10:
                            var choicesGeneric = eventFile.SectionPointersAndCounts.FirstOrDefault(s => (s.Section?.Name ?? "") == "CHOICES");
                            if (choicesGeneric is not null)
                            {
                                var choicesSection = (ChoicesSection)Convert.ChangeType(choicesGeneric.Section, typeof(ChoicesSection));
                                foreach (var choice in choicesSection.Objects)
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
                                                split[0] = split[0][0..^1];
                                            }
                                            (int param, int value) = (int.Parse(split[0]), int.Parse(split[1]));

                                            switch (param)
                                            {
                                                case 1:
                                                    if ((!not && choice.Id != value) || (not && choice.Id == value))
                                                    {
                                                        match = false;
                                                    }
                                                    break;
                                                case 2:
                                                    if ((!not && choice.Padding1 != value) || (not && choice.Padding1 == value))
                                                    {
                                                        match = false;
                                                    }
                                                    break;
                                                case 3:
                                                    if ((!not && choice.Padding2 != value) || (not && choice.Padding2 == value))
                                                    {
                                                        match = false;
                                                    }
                                                    break;
                                                case 4:
                                                    if ((!not && choice.Padding3 != value) || (not && choice.Padding3 == value))
                                                    {
                                                        match = false;
                                                    }
                                                    break;
                                            }
                                        }
                                    }
                                    if (match)
                                    {
                                        CommandSet.Out.WriteLine($"{eventFile.Name} ({eventFile.Index}) has Map Characters Section: {choice.Id} {choice.Padding1} {choice.Padding2} {choice.Padding3}");
                                    }
                                }
                            }
                            break;
                    }
                }
            }

            return 0;
        }
    }
}
