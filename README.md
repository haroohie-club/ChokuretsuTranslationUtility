# Suzumiya Haruhi no Chokuretsu Translation Utility
This repo contains a utility for tranlsating Suzumiya Haruhi no Chokuretsu (The Series of Haruhi Suzumiya).
It's composed of a library containing the methods and classes necessary for interacting with the game files
and a command-line interface and graphical editor that use that library.

## Dependencies & Building
The utility is built ontop of the .NET 5.0 runtime and that's a requirement to use it. To build the solution,
you will need to install the .NET 5.0 SDK. Currently, the utility only runs on Windows, but there are plans to make
it cross-platform in the future (likely with the advent of .NET 7.0 and MAUI).

There are two ways of building the solution:
1. Open the solution file in Visual Studio 2019 (or later, probably) and build it from there.
2. Run `dotnet build` from the command line in the repo root.

## Reverse Engineering Documentation
Suzumiya Haruhi no Chokuretsu was developed by Shade and published by SEGA. Thus, it uses common Shade formats as well as some proprietary Sega formats.

The Chokuretsu ROM contains the following files:
* `bgm/` &ndash; A directory containing background music
    - `BGM001.bin` through `BGM034.bin` &ndash; Music files (unknown format)
* `movie/` &ndash; A directory containing movie files using MODS format (Mobiclip DS encoding; partially implemented decoder [here](https://github.com/Gericom/MobiclipDecoder))
    - `MOVIE00.mods` &ndash; The OP video
    - `MOVIE01.mods` &ndash; The ED/credits video
* `vce/` &ndash; A directory containing voice files
    - Various `.bin` voice files &ndash; All stored in Sega's AHX format (used as far back as the Dreamcast). Tool to decode is ahx2wav, found [here](https://github.com/LemonHaze420/ahx2wav).
* `dat.bin` &ndash; Data file container. Files of note:
    - String Files: #0x001, #0x005, #0x06F, #0x075, #0x077, #0x09E. #0x075 is of particular note, as it contains the bulk of the UI text.
    - #0x070 &ndash; Contains references to the Sparkle (SPKL) files in grp.bin. The function of the Sparkle files is unknown at this time.
    - #0x071 &ndash; The font map file (seems to contain a mapping between Shift-JIS text and the font graphics file).
    - #0x09A &ndash; The first file loaded into the game. Contains file mappings for the BGM and voice files. Function unknown at this time.
    - #0x09B &ndash; This file contains references to several graphics files used throughout the game. The file is structured as follows:
        - **0x04-0x07**: Integer file length
        - **0x0C-0x0F**: Pointer to start of structs section
        - **0x10-0x13**: Integer number of struct entries
        - **0x14-0x17**: Pointer to start of end pointers section
        - **Structs Section**: 0x2C length structs containing the following components (most components unknown):
            - **0x04-0x07**: Integer reference to archive file index
        - **End Pointers Section**: At the end of the file, there are a set of short (16-bit integer) pointers to the struct entries. These pointers have hardcoded references to them
            in game code. As an example, overlay_0001 loops over the pointers from 0x2A64 to 0x2A69 to display the logo splash screens on startup (we actually modified this code and
            #0x09B in order to insert our own splash screens).
* `evt.bin` &ndash; Event file container. Most of these are standard event files containing scene dialogue. A couple special files include:
    - #0x219 &ndash; Seems to contain a list of file names for the other EVT files. The mapping between these and the indices is unknown at this time.
    - #0x244 &ndash; The companion selection text file. Does not have the typical file format of an event file (initialized manually using end pointers only).
    - #0x245 &ndash; The Topics file. Contains the names of all topics and mappings to the event files that selecting that topic during the Puzzle Phase triggers.
        The Topics file contains an 0x18 byte header followed by an array of 0x24 byte topic structs and then by the end pointers. The topic structs are structured as follows:
        - **0x00-0x01**: A short (16-bit integer) index that is the magical "topic index" of this topic.
        - **0x02-0x03**: A short (16-bit integer) index to the event file triggered by selecting this topic in the Puzzle Phase.
        - **0x18-0x19**: A short (16-bit integer) pointer to the Shift-JIS encoded text of the topic.
        - **0x1A-0x1B**: A short (16-bit integer) pointer to the Shift-JIS encoded text of the ticker tape text that appears when selecting the topic.
* `grp.bin` &ndash; Graphics file container. Contains texture/tile, layout, and animation data. A special file of note is the very last file in the archive, #0xE50, which is the
    font file. It has no header and is simply a standard DS 4BPP, 16-pixel wide tile file.
* `scn.bin` &ndash; The contents of this file archive are unknown and have not been reverse engineered.
* `snd.bin` &ndash; A standard SDAT file containing the sound effects used in the game.

### Archive Files
The Shade archive files are arcane and honestly very ugly.

### Event Files
The structure of the event files (as they are currently understood) is as follows:
* **0x00-0x03**: The number of pointers that will be resolved at the beginning of the file. The pointers start at **0x0C** and are spaced out every
    0x08 bytes (every other integer is a pointer). The front pointers contain references
    to other things in the file, most of which are currently not understood. Some important things that are known include:
    - Dramatis Personae &ndash; The set of characters who appear in the event.
    - Dialogue Section Pointer &ndash; This is a pointer that immediately follows the Dramatis Personae and directly points to section where dialogue is defined.
* **0x04-0x07**: The pointer to the End Pointers section.
* **0x08-0x0B**: For files that have a title (e.g., EV1_001), this is the pointer to that title.
* **Dialogue Section**: Location defined by the Dialogue Section Pointer. The dialogue section is composed of an array of structs containing three integers:
    - `int character` &ndash; An integer representing the character speaking the dialogue line. The full set of these references can be found in `enum Speaker` in `EventFile.cs`.
    - `int speakerPointer` &ndash; A pointer to the Dramatis Personae section containing the name of the character speaking the dialogue line. (This does not seem to be used in-game.)
    - `int dialoguePointer` &ndash; A pointer to a Shift-JIS encoded string of the dialogue line being spoken. This string is always zero-padded to four-byte alignment.
* **End Pointers Section**: Location determined by the End Pointers Section pointer. This contains an array of integer pointers to other pointers throughout the file.
    The `EndPointerPointers` include all of the dialogue section pointers. Additionally, they contain:
    - **Choices**: The dialogue tree choices appear prior to the Dramatis Personae section and are Shift-JIS encoded strings referenced by some of the `EndPointerPointers`. The way their position is
        determined as of now is to search look for the first byte to be a Shift-JIS control character which seems to work reliably enough.

### SHTX Shade Texture Files


### Graphical Layout Files

## HaruhiChokuretsuLib
The HaruhiChokuretsuLib project is the primary library which the rest of the solution depends on. It contains a Helpers
class with various helper methods as well as three primary parts.

### Helpers
The following helper methods are available:
* `DecompressData()` and `CompressData()` &ndash; Implementations of the Shade compresssion algorithm that accept and return byte arrays.
* `GetPaletteFromImage()` &ndash; A simplified implementation of [this](https://github.com/antigones/palette_extraction) palette extraction
    routine. Creates an arbitarily sized palette of colors used in an image. Used for changing palette data in Shade texture files.
* `ByteArrayFromString()` and `StringFromByteArray()` &ndash; This pair of methods converts to and from hexadecimal strings and byte arrays.
* `BytesInARowLessThan()` &ndash; Returns true if a specific byte is repeated less than a certain number of times in a row in a given sequence.
* `AddWillCauseCarry()` &ndash; Returns true if an addition operation will cause a carry (used in the unhinged file length routine).
* `ClosestColorIndex()` &ndash; Returns the closest match of a color in a given palette.

### HaruhiChokuretsuLib.Archive
This namespace contains the logic for interacting with the `.bin` archives in the game (`dat.bin`, `evt.bin`, `grp.bin`, and `scn.bin`).
The classes it contains are:

#### `ArchiveFile`
This is the top level class which abstracts the `.bin` file itself. It's a generic class which can be instantiated to contain a list of files of
any of the other classes in this namespace. It contains methods for instantiating all of the files in a given archive and keeping track of their
offsets and file sizes.
* The standard way to instantiate an `ArchiveFile` is with the `ArchiveFile<T>.FromFile()` method.
* Since file indices do not necessarily match up with the position in `Archive.Files`, the proper way to access a file by index is:
    ```csharp
    archiveFile.Files.FirstOrDefault(f => f.Index == index);
    ``` 
* To replace a file in a repo, you should create a new file of the archive's type, initialize it, set the `Edited` flag on the file,
    and then set the file in `archive.Files` at the replacement index to the new file. Example:
    ```csharp
    ArchiveFile<GraphicsFile> grpArchive = ArchiveFile<GraphicsFile>.FromFile("path/to/grp.bin");
    int replacementIndex = 0xC1A;
    string decompressedFilePath = "path/to/decompressed_file";

    GraphicsFile currentFile = grpArchive.Files.FirstOrDefault(f => f.Index == replacementIndex);
    GraphicsFile newGraphicsFile = new();
    newGraphicsFile.Initialize(File.ReadAllBytes(decompressedFilePath, currentFile.Offset));
    newGraphicsFile.Edited = true;
    grpArchive.Files[grpArchive.Files.IndexOf(currentFile)] = newGraphicsFile;
    ```
* To add a file to a repo, simply instatiate a new file and use `archive.AddFile()`.

#### `FileInArchive`
This is the base class for all other file types. All `FileInArchive` types have the following properties:
* `uint MagicInteger` &ndash; The "magic integer" in the archive header that contains the file's offset and compressed length.
* `int Index` &ndash; The index of the file in the archive (not the same as its absolute position).
* `int Offset` &ndash; The offset of the file in the archive. Contained in the MagicInteger.
* `int Length` &ndash; The compressed length of the file. Contained in the MagicInteger.
* `List<byte> Data` &ndash; The decompressed binary data of the file.
* `byte[] CompressedData` &ndash; The compressed binary data of the file (what's found in the archive).
* `bool Edited` &ndash; A flag indicating that this file has been edited. This is used to determine if the file's data should be recompressed
    or if the compressed data should be used on reinsertion.
    
Additionally, `FileInArchive` types have the following methods:
* `Initialize()` &ndash; Initializes the file given decompressed data and an offset.
* `GetBytes()` &ndash; Constructs the decompressed binary data of the file.
* `NewFile()` &ndash; Creates a new file from scratch.
    
The standard way of instantiating a `FileInArchive` type from compressed data is using `FileManager<T>.FromCompressedData()`.

#### `DataFile` 
This is a basic implementation of archive files whose types are not fully understood. It simply is a container for their binary data.

#### `EventFile` 
This is an implementation of the files found in `evt.bin` (and a few in `dat.bin`). These file are mostly composed of pointers and Shift-JIS encoded strings.
