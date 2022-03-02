# HaruhiChokuretsuLib
The HaruhiChokuretsuLib project is the primary library which the rest of the solution depends on. It contains a Helpers
class with various helper methods as well as three primary parts.

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
    newGraphicsFile.Initialize(File.ReadAllBytes(decompressedFilePath, currentFile.Offset));
    newGraphicsFile.Edited = true;
    grpArchive.Files[grpArchive.Files.IndexOf(currentFile)] = newGraphicsFile;
    ```
* To add a file to a repo, simply instatiate a new file and use `archive.AddFile()`.

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
    
The standard way of instantiating a `FileInArchive` type from compressed data is using `FileManager<T>.FromCompressedData()`.

### `DataFile` 
This is a basic implementation of archive files whose types are not fully understood. It simply is a container for their binary data.

### `EventFile` 
This is an implementation of the files found in `evt.bin` (and a few in `dat.bin`). These file are mostly composed of pointers and Shift-JIS encoded strings.
