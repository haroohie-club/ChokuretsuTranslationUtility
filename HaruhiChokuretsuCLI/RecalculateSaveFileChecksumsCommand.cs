using HaruhiChokuretsuLib.Save;
using Mono.Options;
using System.Collections.Generic;
using System.IO;

namespace HaruhiChokuretsuCLI
{
    public class RecalculateSaveFileChecksumsCommand : Command
    {
        private string _saveFile;

        public RecalculateSaveFileChecksumsCommand() : base("recalculate-save-checksums", "Recalculates a save file's checksums (useful for modifying it with a hex editor)")
        {
            Options = new()
            {
                { "s|i|save|input=", "Input save file", s => _saveFile = s }
            };
        }

        public override int Invoke(IEnumerable<string> arguments)
        {
            Options.Parse(arguments);

            SaveFile save = new(File.ReadAllBytes(_saveFile));
            File.WriteAllBytes(_saveFile, save.GetBytes());

            return 0;
        }
    }
}
