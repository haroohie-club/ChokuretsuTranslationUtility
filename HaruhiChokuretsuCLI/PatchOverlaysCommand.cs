using HaruhiChokuretsuLib.Overlay;
using Mono.Options;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace HaruhiChokuretsuCLI
{
    public class PatchOverlaysCommand : Command
    {
        private string _inputOverlaysDirectory, _outputOverlaysDirectory, _overlayPatch, _romInfoPath;
        private bool _showHelp;

        public PatchOverlaysCommand() : base("patch-overlays", "Patches the game's overlays")
        {
            Options = new()
            {
                "Patches the game's overlays given a Riivolution-style XML file and a rominfo.xml",
                "Usage: HaruhiChokuretsuCLI patch-overlays -i [inputOverlaysDirectory] -o [outputOverlaysDirectory] -p [overlayPatch] -r [romInfo]",
                "",
                { "i|input-overlays=", "Directory containing unpatched overlays", i => _inputOverlaysDirectory = i },
                { "o|output-overlays=", "Directory where patched overlays will be written", o => _outputOverlaysDirectory = o },
                { "p|patch=", "Riivolution-style XML patch containing patch information for overlays", p => _overlayPatch = p },
                { "r|rom-info=", "rominfo.xml file containing the overlay table", r => _romInfoPath = r },
                { "h|help", "Shows this help screen", h => _showHelp = true },
            };
        }

        public override int Invoke(IEnumerable<string> arguments)
        {
            Options.Parse(arguments);

            if (_showHelp || string.IsNullOrEmpty(_inputOverlaysDirectory) || string.IsNullOrEmpty(_outputOverlaysDirectory) || string.IsNullOrEmpty(_overlayPatch) || string.IsNullOrEmpty(_romInfoPath))
            {
                int returnValue = 0;
                if (string.IsNullOrEmpty(_inputOverlaysDirectory))
                {
                    CommandSet.Out.WriteLine("Input overlays directory not provided, please supply -i or --input-overlays");
                    returnValue = 1;
                }
                if (string.IsNullOrEmpty(_outputOverlaysDirectory))
                {
                    CommandSet.Out.WriteLine("Output overlays directory not provided, please supply -o or --output-overlays");
                    returnValue = 1;
                }
                if (string.IsNullOrEmpty(_overlayPatch))
                {
                    CommandSet.Out.WriteLine("Overlay patch not provided, please supply -p or --patch");
                    returnValue = 1;
                }
                if (string.IsNullOrEmpty(_romInfoPath))
                {
                    CommandSet.Out.WriteLine("rominfo.xml not provided, please supply -r or --rom-info");
                    returnValue = 1;
                }
                Options.WriteOptionDescriptions(CommandSet.Out);
                return returnValue;
            }

            if (!Directory.Exists(_outputOverlaysDirectory))
            {
                Directory.CreateDirectory(_outputOverlaysDirectory);
            }

            List<Overlay> overlays = new();
            foreach (string file in Directory.GetFiles(_inputOverlaysDirectory))
            {
                overlays.Add(new(file));
            }

            XmlSerializer serializer = new(typeof(OverlayPatchDocument));
            OverlayPatchDocument patchDoc = (OverlayPatchDocument)serializer.Deserialize(File.OpenRead(_overlayPatch));
            foreach (OverlayXml overlay in patchDoc.Overlays)
            {
                Overlay overlayToModify = overlays.First(o => o.Name == overlay.Name);
                CommandSet.Out.WriteLine($"Patching overlay '{overlay.Name}'...");
                foreach (OverlayPatchXml patch in overlay.Patches)
                {
                    overlayToModify.Patch((int)(patch.Location - overlay.Start), patch.Value);
                }
                if (overlay.appendFunction is not null)
                {
                    overlayToModify.Append(overlay.AppendFunction, _romInfoPath);
                }
            }

            foreach (Overlay overlay in overlays)
            {
                overlay.Save(Path.Combine(_outputOverlaysDirectory, $"{overlay.Name}.bin"));
            }

            return 0;
        }
    }
}
