using HaruhiChokuretsuLib.Archive;
using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Util;
using Mono.Options;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace HaruhiChokuretsuCLI
{
    public class ExportIncludeCommand : Command
    {
        private string _inputArchive, _outputSourceFile;
        private bool _commands;
        public ExportIncludeCommand() : base("export-inc", "Export an archive include file")
        {
            Options = new()
            {
                { "i|input=", "Input bin archive file", i => _inputArchive = i },
                { "o|output=", "Output source file", o => _outputSourceFile = o },
                { "c|commands", "When used with evt.bin, exports the command list as an include file", c => _commands = true },
            };
        }

        public override int Invoke(IEnumerable<string> arguments)
        {
            Options.Parse(arguments);
            ConsoleLogger log = new();

            if (string.IsNullOrEmpty(_inputArchive) && !_commands)
            {
                CommandSet.Error.WriteLine("ERROR: Must provide input archive.");
            }
            string outputSourceFile = _outputSourceFile;
            if (string.IsNullOrEmpty(outputSourceFile))
            {
                if (_commands)
                {
                    outputSourceFile = "COMMANDS.INC";
                }
                else
                {
                    outputSourceFile = $"{Path.GetFileNameWithoutExtension(_inputArchive).ToUpper()}BIN.INC";
                }
            }

            if (_commands)
            {
                StringBuilder sb = new();
                foreach (ScriptCommand command in EventFile.CommandsAvailable)
                {
                    sb.AppendLine(command.GetMacro());
                }
                File.WriteAllText(outputSourceFile, sb.ToString());
            }
            else
            {
                ArchiveFile<FileInArchive> arc = ArchiveFile<FileInArchive>.FromFile(_inputArchive, log);
                File.WriteAllText(outputSourceFile, arc.GetSourceInclude());
            }

            return 0;
        }
    }
}
