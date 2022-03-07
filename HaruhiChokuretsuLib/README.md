# HaruhiChokuretsuLib
The HaruhiChokuretsuLib project is the primary library which the rest of the solution depends on. It contains a Helpers
class with various helper methods as well as three namespaces.

Before reading this documentation, it is recommended you familiarize yourself with the [reverse engineering documentation](./ReverseEngineering.md)
as it will provide context for what is being described here.

## Helpers
The following helper methods are available:

* `DecompressData()` and `CompressData()` &ndash; Implementations of the Shade compresssion algorithm that accept and return byte arrays.
* `GetPaletteFromImage()` &ndash; A simplified implementation of [this](https://github.com/antigones/palette_extraction) palette extraction
    routine. Creates an arbitarily sized palette of colors used in an image. Used for changing palette data in Shade texture files.
* `ByteArrayFromString()` and `StringFromByteArray()` &ndash; This pair of methods converts to and from hexadecimal strings and byte arrays.
* `BytesInARowLessThan()` &ndash; Returns true if a specific byte is repeated less than a certain number of times in a row in a given sequence.
* `AddWillCauseCarry()` &ndash; Returns true if an addition operation will cause a carry (used in the unhinged file length routine).
* `ClosestColorIndex()` &ndash; Returns the closest match of a color in a given palette.

## HaruhiChokuretsuLib.Archive
This namespace contains the logic for interacting with the `.bin` archives in the game (`dat.bin`, `evt.bin`, `grp.bin`, and `scn.bin`).
The classes it contains are:

### `ArchiveFile`
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
    newGraphicsFile.Initialize(File.ReadAllBytes(decompressedFilePath), currentFile.Offset);
    newGraphicsFile.Edited = true;
    grpArchive.Files[grpArchive.Files.IndexOf(currentFile)] = newGraphicsFile;
    ```
* To add a file to an archive, simply instatiate a new file and use `archive.AddFile()`.

### `FileInArchive`
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

### `DataFile` 
This is a basic implementation of archive files whose types are not fully understood. It simply is a container for their binary data.

### `EventFile` 
This is an implementation of the event files found in `evt.bin`. These file are mostly composed of pointers and Shift-JIS encoded strings.
While it's designed to represent event files, the `EventFile` class can also be used to represent special string files.
In addition to the properties it inherits from `FileInArchive`, `EventFile`s contain the following properties:

* `List<int> FrontPointers` &ndash; A list containing all of the pointers that appear at the beginning of the file.
* `int PointerToEndPointerSection` &ndash; The pointer to the end pointers section.
* `List<int> EndPointers` &ndash; A list containing all of the pointers that appear at the end of the file.
* `List<int> EndPointerPointers` &ndash; A list containing all of the pointers that the end pointers point to.
* `string Title` &ndash; If one exists, the title of the event file (e.g., EV1_001).
* `Dictionary<int, string> DramatisPersonae` &ndash; A dictionary containing the offsets and names of the characters denoted in the dramatis personae section.
* `int DialogueSectionPointer` &ndash; A pointer to the dialogue section.
* `List<DialogueLine> DialogueLines` &ndash; The dialogue lines contained in this event.
* `List<TopicStruct> TopicStructs` &ndash; In #0x245 (the Topic file), this represents all of the topic structs in the file. In main event files,
    this represents the obtainable topics in that event.
* `FontReplacementDictionary FontReplacementMap` &ndash; The font replacement (charset) map.

It also has the following methods:
* `InitializeDialogueForSpecialFiles()` &ndash; This initializes files that are not standard event files but still have strings (the string files in `dat.bin`
    and files #0x244 and #0x245 in `evt.bin` are the primary examples).
* `InitializeTopicFile()` &ndash; An initialization routine that is called specifically for #0x245 in `evt.bin` after `InitializeDialougeForSpecialFiles` is called.
* `IdentifyEventFileTopics()` &ndash; Identify the topics from the Control Section of main event files.
* `ShiftPointers()` &ndash; Shifts all pointers in the file based on a location in the file and an amount to shift. Every pointer that points to something after the specified
    location has its value shifted by the specified amount.
* `EditDialogueLine()` &ndash; Edits a line of dialogue, including calling `ShiftPointers()` after editing.
* `WriteResxFile()` &ndash; Writes all dialogue lines to a .NET Resource (RESX) file.
* `ImportResxFile()` &ndash; Reads all dialogue lines from a .NET Resource (RESX) file and replaces the dialogue in the file with those lines. The following changes are made
    to strings in the file:
    - Three dots (`...`) are replaced by an ellipsis character (`…`)
    - Two hyphens (`--`) are replaced by an em-dash character (`—`)
    - Replaces Windows linebreaks (`\r\n`) with Unix-style ones (`\n`)
    - Replaces text according to the `FontReplacementMap`.
    - Automatically introduces line breaks past to prevent text from going off the screen (in non-dat files)

`NewFile()` is not implemented for `EventFile`s.

The following sub-classes are used by the `EventFile` class:

#### `DialogueLine`
The `DialogueLine` class abstracts the structs in the Dialogue Section of the event file. It has the following properties:

* `int Pointer` &ndash; The pointer to the dialogue text; corresponds to the third int in the struct.
* `byte[] Data` &ndash; The binary data of the dialogue text string.
* `int NumPaddingZeroes` &ndash; The number of zeroes added to pad the string to four-byte alignment.
* `string Text` &ndash; An string abstraction of the `Data` property.
* `int Length` &ndash; The length of the `Data` property.
* `int SpeakerPointer` &ndash; The pointer to the Dramatis Personae section speaker name, corresponds to the second int of the struct.
* `Speaker Speaker` &ndash; An enum value representing the speaker of the dialogue line. Corresponds to the first int of the struct.
* `string SpeakerName` &ndash; The string value found at the `SpeakerPointer` position.

#### `TopicStruct`
The `TopicStruct` class abstracts the structs found in the Topics file #0x245. It has the following properties:

* `int ToipcDialogueIndex` &ndash; An index representing which `DialogueLine` this Topic corresponds to.
* `string Title` &ndash; The title of the Topic (equivalent to the text of the dialogue line).
* `short Id` &ndash; The ID of the topic, corresponding to the first short of the struct.
* `short EventIndex` &ndash; The event this topic triggers during the puzzle phase, corresponding to the second short of the struct.
* `short[] UnknownShorts` &ndash; The following 16 unknown shorts in the struct (0x20 bytes).

### `GraphicsFile`
The `GraphicsFile` class is designed to implement the files found in `grp.bin`; however, while most of the files are understood and can be parsed, there are still
some that remain unknown.

The property that determines what time of graphic a give file is is the `FileFunction` property which uses the `Function` enum. The options are `SHTX`, `LAYOUT`, and `UNKNOWN`.

#### `SHTX`
When `FileFunction` is set to `Function.SHTX` (which happens if the first four bytes of the file are `SHTX`), the class implements a Shade Texture file. The following properties
become relevant:

* `List<byte> PaletteData` &ndash; Contains the binary palette data of the image.
* `List<Color> Palette` &ndash; Contains the colors of the image's palette.
* `List<byte> PixelData` &ndash; Contains the image's binary pixel data.
* `int Width` &ndash; The image's width.
* `int Height` &ndash; The image's height.
* `TileForm ImageTileForm` &ndash; Whether the image is a 16-color/4BPP image or a 256-color/8BPP image.
* `Form ImageForm` &ndash; Whether the image is a texture, a tile image, or unknown.
* `public string Determinant` &ndash; The two bits following `SHTX` (either `DS` or `D5`).

Furthermore, the following methods are relevant:

* `NewFile()` &ndash; Only implemented for SHTX files.
* `InitializeFontFile()` &ndash; Initializes the font file (#0xE50) which is a special file only containing pixel data.
* `IsTexture()` &ndash; Manually determines whether a file is of `Form` `TILE` or `TEXTURE`. Since there isn't a known way to determine this from file data, this is manually constructed
    based on position within the archive.
* `GetImage()` &ndash; Returns a bitmap image of the SHTX file. Optionally allows specifying a transparent index, an index into the palette that will be made transparent (in reality almost always 0).
* `SetImage()` &ndash; Sets the pixel data to that of a bitmap in memory or bitmap file provided. Optionally, can set a flag to create a new palette from the image, and can set a transparent index.
* `GetPalette()` &ndash; Gets a bitmap image representing the palette data of the image.
* `SetPalette()` &ndash; Accepts a `List<Color>` to set as the palette for the image.

#### Layout
When `FileFunction` is set to `Function.LAYOUT` (which happens if the first four bytes are either 0x0001C000 or 0x10048802), the class implements a layout file. The only
relevant property for layouts is `List<LayoutEntry> LayoutEntries`, which abstracts the layout entry struct.

The only relevant method is `GetLayout()`, which returns a bitmap representation of the given layout entries as well as those layout entries.

The `LayoutEntry` class, however, is immediately relevant. Most of its properties are self-explanatory and line up exactly with those described in the [reverse engineering documentation](ReverseEngineering.md).
However, a few properties are more abstracted:

* `Color Tint` &ndash; The color associated with the ARGB tint value in the struct
* `bool FlipX` &ndash; If the `ScreenX` value in the struct is negative, `FlipX` is set to true and the absolute value of `ScreenX` is used. This indicates that the layout entry should be flipped horizontally
    when displayed.
* `bool FlipY` &ndash; If the `ScreenY` value in the struct is negative, `FlipY` is set to true and the absolute value of `ScreenY` is used. This indicates that the layout entry should be flipped vertically
    when displayed.

## HaruhiChokuretsuLib.Font
The Font namespace is composed of two classes and simply provides the logic for interacting with the font-width ASM hack we have implemented. The first class, the `FontReplacement` class,
is an abstraction of the font replacement JSON and contains information on the `OriginalCharacter` in the script, the character we replace it with (`ReplacedCharacter`), the Shift-JIS
`CodePoint` where the replacement occurs, and the amount the character is `Offset`. This then feeds into a `FontReplacementDictionary`, a class which implements `IDictionary` and allows for
easy lookup into a `List<FontReplacement>`.

## HaruhiChokuretsuLib.Overlay
This namespace contains the methods to aid in interacting with the game's overlays.

### `Overlay`
The `Overlay` class is an abstraction of an overlay. It contains the following properties:

* `string Name` &ndash; The name of the overlay (e.g., main_0001).
* `int Id` &ndash; The overlay's ID (e.g., 1). This is determined by parsing the last four characters of the overlay's name as a hex number.
* `List<byte> Data` &ndash; The binary assembled instructions of the overlay.

It also contains the following methods:

* `Overlay(string file)` &ndash; The constructor takes a file and uses its name as the `Name` and its contents as the `Data`.
* `Save()` &ndash; Writes the contents of `Data` to a given file.
* `Patch()` &ndash; Replaces binary data at a particular point with provided patch adata.
* `Append()` &ndash; Takes data to append and the path to an NDS project file that contains an XML representation of the overlay table. This method appends the data to the end of the array and then
    modifies the overlay table to reflect the updated size of the overlay.

### `OverlayPatchDocument`
This class simply serializes the Riivolution-style overlay patch documents.