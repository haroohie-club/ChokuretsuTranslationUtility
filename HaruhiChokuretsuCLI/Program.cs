using Mono.Options;

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
