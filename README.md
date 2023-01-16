# Suzumiya Haruhi no Chokuretsu Translation Utility
This repo contains a utility for translating Suzumiya Haruhi no Chokuretsu (The Series of Haruhi Suzumiya).
It's composed of a library containing the methods and classes necessary for interacting with the game files
and a command-line interface and graphical editor that use that library.

## Dependencies & Building
The utility is built on top of the .NET 6.0 runtime and that's a requirement to use it. To build the solution,
you will need to install the .NET 6.0 runtime. The CLI and library are fully cross-platform, working on Windows,
macOS, and Linux. However, currently the editor only runs on Windows. There are no plans to make it cross-platform
in the future as it will be superseded by a level editor.

There are two ways of building the solution:
1. Open the solution file in Visual Studio 2022 (or later, probably) and build it from there.
2. Run `dotnet build` from the command line in the repo root.

## Usage and Reverse Engineering Documentation
* For documentation on how to use the graphical editor, go [here](HaruhiChokuretsuEditor/README.md).
* For documentation on how to use the command-line interface, go [here](HaruhiChokuretsuCLI/README.md).
* For documentation on how to use HaruhiChokuretsuLib in your own code, go [here](HaruhiChokuretsuLib/README.md).
* For documentation on the structure and composition of Suzumiya Haruhi no Chokuretsu, see the [Wiki](https://github.com/haroohie-club/ChokuretsuTranslationUtility/wiki).