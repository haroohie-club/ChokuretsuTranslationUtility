using HaruhiChokuretsuLib;
using HaruhiChokuretsuLib.Archive;
using HaruhiChokuretsuLib.Font;
using HaruhiChokuretsuLib.Overlay;
using Mono.Options;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace HaruhiChokuretsuCLI
{
    public class Program
    {
        public static int Main(string[] args)
        {
            CommandSet commands = new("HaruhiChokuretsuCLI")
            {
                "Usage: HaruhiChokuretsuCLI COMMAND [OPTIONS]",
                "",
                "Available commands:",
                new UnpackCommand(),
                new ExtractCommand(),
                new ReplaceCommand(),
                new ImportResxCommand(),
                new PatchOverlaysCommand(),
                new HexSearchCommand(),
                new VersionScreenCommand(),
            };

            return commands.Run(args);
        }
    }
}
