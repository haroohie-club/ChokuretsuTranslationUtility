using Mono.Options;

namespace HaruhiChokuretsuCLI;

public class Program
{
    public static int Main(string[] args)
    {
        CommandSet commands = new("HaruhiChokuretsuCLI")
        {
            "Usage: HaruhiChokuretsuCLI COMMAND [OPTIONS]",
            "",
            "Available commands:",
            new JsonExportCommand(),
            new JsonImportCommand(),
            new UnpackCommand(),
            new ExtractCommand(),
            new ReplaceCommand(),
            new ImportResxCommand(),
            new LocalizeSourceFilesCommand(),
            new HexSearchCommand(),
            new VersionScreenCommand(),
            new FontAppendCommand(),
            new ConvertAudioCommand(),
            new ExportCharacterSpriteCommand(),
            new ExportChibiCommand(),
            new ExportMapCommand(),
            new ExportLayoutCommand(),
            new ExportIncludeCommand(),
            new ScriptCommandSearchCommand(),
            new RecalculateSaveFileChecksumsCommand(),
            new CompareSavesCommand(),
            new ExportScriptToWikiTableCommand(),
            new DumpGraphicsCommand(),
        };

        return commands.Run(args);
    }
}