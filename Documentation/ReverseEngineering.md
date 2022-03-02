# Reverse Engineering Documentation
Suzumiya Haruhi no Chokuretsu was developed by Shade and published by SEGA. Thus, it uses common Shade formats as well as some proprietary Sega formats.

## ROM Structure
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
    font file. It has no header and is simply a standard DS 4BPP, 16-pixel wide tile file. There is also one file at -x
* `scn.bin` &ndash; The contents of this file archive are unknown and have not been reverse engineered.
* `snd.bin` &ndash; A standard SDAT file containing the sound effects used in the game.

All files are little-endian (as expected of ARM architecture).

## Archive Files
The Shade archive files are arcane and honestly very ugly; however, they are fairly well-understood. The structure is as follows:

* **0x00-0x03** &ndash; The number of items contained within the archive.
* **0x04-0x07** &ndash; The "magic integer most-significant bits multiplier" (MMSB), used in the calculation of the "magic integer."
* **0x08-0x0B** &ndash; The "magic integer least-significant bits multiplier" (MLSB), used in the calculation of the "magic integer."
* **0x0C-0x0F** &ndash; The "magic integer most-significant bits shift" (S), used in the calculation of the "magic integer."
* **0x10-0x13** &ndash; The "magic integer least-significant bits bitwise-and" (A), used in the calculation of the "magic integer."
* **Magic Integer Section** &ndash; Starting at 0x1C and ending at (0x1C + 4 * NumItems - 1), this section comprises of the "magic integers" which contain
    the offsets and compressed lengths of the files in the archive. The offset is calculated from this number by the following formula:
    ```csharp
        Offset = (MagicInteger >> S) * MMSB;
    ```
    The compressed length, meanwhile starts with:
    ```csharp
        MagicLengthInteger = 0x7FF + (MagicInteger & A) * MLSB;
    ```
    and then uses that MagicLengthInteger in an absolutely unhinged routine to calculate the file length. An implementation of the routin can be found
    in the `GetFileLength()` method in [`ArchiveFile.cs`](../HaruhiChokuretsuLib/Archive/ArchiveFile.cs).

    Encoded file lengths and offsets are all multiples of 0x800.
* **Secondary Intger Section** &ndash; Starting at (0x1C + 4 * NumItems) and ending at (0x1C + 8 * NumItems - 1), this section's function remains unknown.
* **Final Header Section** &ndash; Starting at (0x1C + 8 * NumItems), this section is not understood at all. It does not seem to be used in-game.
* **Files** &ndash; The files in the archives are compressed using a custom run-length encoding algorithm (referred to as Shade compression). 

## Event Files
Event files control all of the scenes ("events") in the game and are all contained with in `evt.bin`. The structure of the event files is as follows:

* **0x00-0x03**: The number of pointers that will be resolved at the beginning of the file. The pointers start at **0x0C** and are spaced out every
    0x08 bytes (every other integer is a pointer). The front pointers contain references
    to other things in the file, most of which are currently not understood. Some important things that are known include:
    - Dramatis Personae &ndash; The set of characters who appear in the event.
    - Dialogue Section Pointer &ndash; This is a pointer that immediately follows the Dramatis Personae and directly points to section where dialogue is defined.
* **0x04-0x07**: The pointer to the End Pointers section.
* **0x08-0x0B**: For files that have a title (e.g., EV1_001), this is the pointer to that title.
* **Dialogue Section**: Location defined by the Dialogue Section Pointer. The dialogue section is composed of an array of structs containing three integers:
    - **0x00-0x03** &ndash; An integer representing the character speaking the dialogue line. The full set of these references can be found in `enum Speaker` in [`EventFile.cs`](../HaruhiChokuretsuLib/Archive/EventFile.cs).
    - **0x04-0x07** &ndash; A pointer to the Dramatis Personae section containing the name of the character speaking the dialogue line. (This does not seem to be used in-game.)
    - **0x08-0x0B** &ndash; A pointer to a Shift-JIS encoded string of the dialogue line being spoken. This string is always zero-padded to four-byte alignment.
* **End Pointers Section**: Location determined by the End Pointers Section pointer. This contains an array of integer pointers to other pointers throughout the file.
    The `EndPointerPointers` include all of the dialogue section pointers. Additionally, they contain:
    - **Choices**: The dialogue tree choices appear prior to the Dramatis Personae section and are Shift-JIS encoded strings referenced by some of the `EndPointerPointers`. The way their position is
        determined as of now is to search look for the first byte to be a Shift-JIS control character which seems to work reliably enough.

## SHTX Shade Texture Files
Shade texture files (SHTX) are standard graphics files that are all contained within `grp.bin`. The structure of these files is as follows:

* **0x00-0x05** &ndash; The file identifier. Usually is `SHTXDS` (likely, "Shade Texture DS"), but there are three instances of `SHTXD5` occurring. It is unknown if these
    second files have different properties than the `SHTXDS` files.
* **0x06-0x07** &ndash; A short representing the color-space of the image. `0x10` corresponds to 16-color images (4bpp) and `0x100` corresponds to 256-color images (8bpp).
* **0x0E-0x0F** &ndash; The width (0x0E) and height (0x0F) of the image, each encoded as a single byte. The dimensions are obtained by raising two to the power of the given byte.
* **Palette Section** &ndash; Starting at 0x14 and spanning 0x60 bytes for 4bpp images and 0x200 bytes for 8bpp images, this section represents the color palette of the image.
    Each color is encoded in a short and uses 15-bits (five bits for each RGB component) with the most-significant bit going unused.
* **Pixel Data Section** &ndash; The remainder of the file after the palette is used to encode the pixel data. The pixel data is encoded in one of two ways:
    - Tile: Standard DS tile format where pixels are encoded in 8x8 blocks. Additionally, these files are all "tiled" in blocks of varying sizes.
    - Texture: Pixels are encoded from left to right, top to bottom. Very straightforward and simple format.
    In both cases, pixels are encoded as byte-long (or 4-bit long in the case of 4bpp images) indices into the palette.

## Graphical Layout Files
Graphical layout files define the way graphics are drawn to the screen, carving out quads from SHTX files and then placing them in 2D screen space.
The structure of these files is as follows:
* **0x00-0x03** &ndash; Appears to be a standard identifier for these files; can either be `0x0001C000` or `0x10048802`.
* **Layout Entries Section** &ndash; Starting at 0x08 and continuing through the end of the file, layout entries are 0x1C-byte long structs that contain
    data about a particular quad to be drawn. Their structure is a series of shorts as follows:
    - **0x00-0x01** &ndash; Unknown short #1.
    - **0x02-0x03** &ndash; Relative SHTX index. This is the index (counting forward and only counting SHTX files) of the texture the quad is pulled from.
    - **0x04-0x05** &ndash; Unknown short #2.
    - **0x06-0x07** &ndash; Screen X position; the X position that the quad will be drawn to on the screen.
    - **0x08-0x09** &ndash; Screen Y position; the Y position that the quad will be drawn to on the screen.
    - **0x0A-0x0B** &ndash; Texture width; the width of the quad bounding box within the reference texture.
    - **0x0C-0x0D** &ndash; Texture height; the height of the quad bounding box within the reference texture.
    - **0x0E-0x0F** &ndash; Texture X position; the X position of the quad bounding box on the reference texture.
    - **0x10-0x11** &ndash; Texture Y position; the Y position of the quad bounding box on the reference texture.
    - **0x12-0x13** &ndash; Screen width; the width of the quad as drawn on the screen. If negative, the texture will be flipped.
    - **0x14-0x15** &ndash; Screen height; the height of the quad as drawn on the screen. If negative, the texture will be flipped.
    - **0x16-0x17** &ndash; Unknown short #3.
    - **0x18-0x1B** &ndash; Tint; a 32-bit integer representing an ARBG color to tint the texture.
