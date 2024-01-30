using HaruhiChokuretsuLib.Save;
using Mono.Options;
using System.Collections.Generic;
using System.IO;

namespace HaruhiChokuretsuCLI
{
    public class CompareSavesCommand : Command
    {
        private string _firstSave, _secondSave;
        private int _saveToCompare;

        public CompareSavesCommand() : base("compare-saves", "Compares two save files and shows which flags they differ on")
        {
            Options = new()
            {
                { "a|save1|first-save=", "First save file to compare", a => _firstSave = a },
                { "b|save2|second-save=", "Second save file to compare", b => _secondSave = b },
                { "c|compare=", "Save to compare (0 = common, 1-2 = checkpoint saves, 3 = quicksave", c => _saveToCompare = int.Parse(c) },
            };
        }

        public override int Invoke(IEnumerable<string> arguments)
        {
            Options.Parse(arguments);

            SaveFile firstSave = new(File.ReadAllBytes(_firstSave));
            SaveFile secondSave = new(File.ReadAllBytes(_secondSave));

            SaveSection firstSection = _saveToCompare switch
            {
                1 => firstSave.CheckpointSaveSlots[0],
                2 => firstSave.CheckpointSaveSlots[1],
                3 => firstSave.QuickSaveSlot,
                _ => firstSave.CommonData,
            };
            SaveSection secondSection = _saveToCompare switch
            {
                1 => secondSave.CheckpointSaveSlots[0],
                2 => secondSave.CheckpointSaveSlots[1],
                3 => secondSave.QuickSaveSlot,
                _ => secondSave.CommonData,
            };

            for (int i = 0; i < firstSection.Flags.Length * 8; i++)
            {
                if (firstSection.IsFlagSet(i) != secondSection.IsFlagSet(i))
                {
                    CommandSet.Out.WriteLine($"Differ on flag {i}: Save 1 has flag {(firstSection.IsFlagSet(i) ? "set" : "not set")} while Save 2 has flag {(secondSection.IsFlagSet(i) ? "set" : "not set")}");
                }
            }

            return 0;
        }
    }
}
