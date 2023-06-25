using HaruhiChokuretsuLib.Archive;
using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Audio.SDAT;
using HaruhiChokuretsuLib.Util;
using Mono.Options;
using System.Collections.Generic;
using System.Linq;

namespace HaruhiChokuretsuCLI
{
    public class ExportSfxCommand : Command
    {
        private string _snd, _dat, _outputDirectory;
        private int[] _indices;
        private bool _all;

        public ExportSfxCommand() : base("export-sfx", "Export sound effects")
        {
            Options = new()
            {
                { "i|input|s|snd|input-archive=", "The input snd.bin archive", i => _snd = i },
                { "d|dat=", "The input dat.bin archive", d => _dat = d },
                { "o|output|output-directory=", "The output directory where the file(s) will be saved", o => _outputDirectory = o },
                { "n|indices=", "A comma-separated list of SFX indices to export", n => _indices = n.Split(',').Select(n => int.Parse(n.Trim())).ToArray() },
                { "a|all", "Export all SFX", a => _all = true },
            };
        }

        public override int Invoke(IEnumerable<string> arguments)
        {
            Options.Parse(arguments);
            ConsoleLogger log = new();

            SoundArchive snd = new(_snd);
            ArchiveFile<DataFile> dat = ArchiveFile<DataFile>.FromFile(_dat, log);
            SoundDSFile sndDsFile = dat.Files.First(f => f.Name == "SND_DSS").CastTo<SoundDSFile>();

            List<SequenceArchiveSequence> sequences;

            if (_all)
            {
                sequences = sndDsFile.SfxSection.Select(s => snd.SequenceArchives[s.SequenceArchive].File.Sequences[s.Index]).ToList();
            }
            else
            {
                sequences = _indices.Select(i => snd.SequenceArchives[sndDsFile.SfxSection[i].SequenceArchive].File.Sequences[sndDsFile.SfxSection[i].Index]).ToList();
            }

            return 0;
        }
    }
}
