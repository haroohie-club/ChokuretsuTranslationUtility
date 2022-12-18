using HaruhiChokuretsuLib.Archive;
using Mono.Options;
using System.Collections.Generic;
using System.IO;

namespace HaruhiChokuretsuCLI
{
    public class ExportArchiveIncludeeCommand : Command
    {
        private string _inputArchive, _outputSourceFile;
        public ExportArchiveIncludeeCommand() : base("export-archive-inc", "Export an archive include file")
        {
            Options = new()
            {
                { "i|input=", "Input bin archive file", i => _inputArchive = i },
                { "o|output=", "Output source file", o => _outputSourceFile = o },
            };
        }

        public override int Invoke(IEnumerable<string> arguments)
        {
            Options.Parse(arguments);

            if (string.IsNullOrEmpty(_inputArchive))
            {
                CommandSet.Error.WriteLine("ERROR: Must provide input archive.");
            }
            string outputSourceFile = _outputSourceFile;
            if (string.IsNullOrEmpty(outputSourceFile))
            {
                outputSourceFile = $"{Path.GetFileNameWithoutExtension(_inputArchive).ToUpper()}BIN.INC";
            }

            ArchiveFile<FileInArchive> arc = ArchiveFile<FileInArchive>.FromFile(_inputArchive);

            File.WriteAllText(outputSourceFile, arc.GetSourceInclude());

            return 0;
        }
    }
}
