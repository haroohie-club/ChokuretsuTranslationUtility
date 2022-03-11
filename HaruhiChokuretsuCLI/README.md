# HaruhiChokuretsuCLI
The HaruhiChokuretsuCLI is a command-line interface that can be used to directly interact with an unpacked Suzumiya Haruhi no Chokuretsu ROM. To unpack the ROM,
you will need to use a utility such as NDS Lazy or NitroPacker.

All commands are self-documented with `-h` or `--help`, e.g. `HaruhiChokuretsuCLI unpack --help` will print help for the `unpack` command.

## _Unpack_ an archive
The `unpack` command unpacks all files in a specified archive to a specified directory. Files will be named by hexadecimal index. Its arguments are:

* `-i` or `--input-archive` &ndash; The archive to unpack files from.
* `-o` or `--output-directory` &ndash; The directory to unpack files to.
* `-c` or `--compressed` &ndash; Don't decompress the unpacked files.

| Example | Function |
|---------|----------|
| `HaruhiChokuretsuCLI unpack -i "rom/grp.bin" -o "graphics/"` | Unpacks all of the files in `grp.bin` to the `graphics/` directory. |
| `HaruhiChokuretsuCLI unpack -i "rom/scn.bin" -o "scene/" -c` | Unpacks all the files in `scn.bin` to `scene/`, but leaves them compressed. |

## _Extract_ a file from an archive
The `extract` command will extract an individual file from an archive either as raw binary data (a targeted `unpack`) or as a PNG image for graphics or a .NET resource
file (RESX) for string files. Its arguments are:

* `-i` or `--input-archive` &ndash; The archive to extract a file from.
* `-n` or `--index` &ndash; The index of the file to extract; this doesn't need to be specified if the output file is a hex integer.
* `-o` or `--output-file` &ndash; Filename of the extracted file. If the file extension is `.png` or `.resx`, the utility will attempt to extract the file in that format.
* `-w` or `--image-width` &ndash; If extracting an image file, this specifies the width of the image. Defaults to the encoded width of the image.

| Example | Function |
|---------|----------|
| `HaruhiChokuretsuCLI extract -i "rom/dat.bin" -o "data/076.bin` | Extracts the file at index 0x076 from `dat.bin`. |
| `HaruhiChokuretsuCLI extract -i "rom/evt.bin" -n 361 -o "event/EV1_001.ja.resx"` | Extracts the file at index 361 from `evt.bin` as a .NET resource file. |
| `HaruhiChokuretsuCLI extract -i "rom/grp.bin" -o "graphics/E50.png"` | Extracts the file at index 0xE50 from `grp.bin` as a PNG. |
| `HaruhiChokuretsuCLI extract -i "rom/grp.bin" -n 0xC1A -o "graphics/title.png" -w 64` | Extracts the file at index 0xC1A from `grp.bin` as a 64-pixel wide PNG. |

## _Replace_ a file in an archive
The `replace` command will replace either a single file or a set of files in an archive depending on whether you pass it a file or a directory. It has the following arguments:

* `-i` or `--input-archive` &ndash; The archive to replace file(s) in.
* `-o` or `--output-archive` &ndash; The location to save the modified archive to.
* `-r` or `--replacement` &ndash; A file or directory to replace with/from. Images must be `.png` files and all others must be `.bin` files.

The files referenced by `--replacement` have a naming convention of `({hex}|new)[_newpal|_sharedpal{num}[_tidx{num}]][_{comments}].{ext}`. These components have the following
effects:

* `{hex}` &ndash; The bare minimum a file needs for replacement is a hex number as its name. For example, `C45.bin` will replace the file at index 0xC45 in the archive specified.
* `new` &ndash; Instead of providing `{hex}`, a file named `new` will be added to the specified archive. This is currently implemented only for graphics files, where you must
    also specify whether the image is 4bpp or 8bpp and whether they are tiles or textures (e.g., `new_4bpp_tile` or `new_8bpp_tex`). New graphics files do not need `_newpal` specified
    as they must automatically generate a palette for themselves. They can, however, use a `_sharedpal`.
* `_newpal` &ndash; When in a PNG file's name, this will generate a new palette for the graphics file from the colors used in the PNG.
* `_sharedpal{num}` &ndash; This can be used as an alternative to `_newpal` for when multiple files need to share the same palette. This will generate a single palette based on the colors
    used in all images that have the same `{num}` and apply that palette to each of the graphics files that use it.
* `_tidx{num}` &ndash; Can be used with `_sharedpal` or `_newpal` to specify a transparent index that shouldn't be used as part of the palette generation (this is almost always 0).
* `_{comments}` &ndash; Any comments can go at the end of the file name and will be ignored.
* `{ext}` &ndash; As stated above, the file extension must be `.png` for image files and `.bin` for all other files.
* Finally, a file containing `ignore` anywhere in the file name will be ignored by the replacement process.

| Example | Function |
|---------|----------|
| `HaruhiChokuretsuCLI replace -i "rom/grp.bin" -o "out/grp.bin" -r "graphics/C1A.png"` | Will replace the file at 0xC1A in `grp.bin` with a graphics file representing the contents of `C1A.png` and then save the modified archive to `out/grp.bin`. |
| `HaruhiChokuretsuCLI replace -i "rom/dat.bin" -o "out/dat.bin" -r "data/"` | Will replace files in `dat.bin` with files in `data/` and then save the modified archive to `out/dat.bin`. |

### File naming examples
| Example | Function |
|---------|----------|
| `C45_puzzle_layouts.bin` | A binary file that will replace the file at 0xC45 in the specified archive. |
| `new001_8bpp_tile_tidx0_credits.png` | A PNG that will be turned into an 8bpp tile graphics file with transparent index 0 and appended to the archive. |
| `8b7_newpal_tidx0_splash_screen.png` | A PNG that will replace the file at 0x8B7 with a new graphics file with a new palette and transparent index 0. |
| `141_sharedpal0.png` | A PNG that will replace the file at 0x141 and shares its new palette with all other files tagged `_sharedpal0`. |
| `e50_original_ignore.png` | A PNG file that will be ignored for replacement. |

## _Import RESXs_ into an archive
The `import-resx` command will import a set of .NET resource (RESX) files with a given language code into an archive with string files (such as `evt.bin` or `dat.bin`). Unlike the `replace` command,
`import-resx` expects the files to be named with decimal indices (e.g., `360.en.resx`). Its arguments are:

* `-i` or `--input-archive` &ndash; The archive to replace strings in.
* `-o` or `--output-archive` &ndash; The location to save the modified archive to.
* `-r` or `--resx-directory` &ndash; The location of the directory with RESX files to import.
* `-l` or `--lang-code` &ndash; The language code of the desired target language (e.g., "en" for English; used to filter RESX files).
* `-f` or `--font-map` &ndash; The font offset mapping JSON file.

| Example | Function |
|---------|----------|
| `HaruhiChokuretsuCLI import-resx -i "rom/evt.bin" -o "out/evt.bin" -r "strings/" -l "en" -f "charset.json"` | Replaces strings in `evt.bin` with strings from all the English-language RESX files in `strings/` using `charset.json` as a font map. |

An example `strings/` directory might look like:

* 001.en.resx
* 001.it.resx
* 001.ja.resx
* 001.ru.resx
* ...
* 360.en.resx
* 360.it.resx
* 360.ja.resx
* 360.ru.resx

etc.

## _Patch Overlays_
The `patch-overlays` command does essentially exactly what it says on the tin: it patches the game's overlays using a Riivolution-style XML file and a rominfo.xml file containing the overlay table.
Its arguments are:

* `-i` or `--input-overlays` &ndash; Directory containing the unpatched overlays.
* `-o` or `--output-overlays` &ndash; Directory where patched overlays will be written to.
* `-p` or `--patch` &ndash; The Riivolution-style XML file containing patch information for the overlays.
* `-r` or `--rom-info` &ndash; A rominfo.xml file containing the overlay table.

| Example | Function |
|---------|----------|
| `HaruhiChokuretsuCLI patch-overlays -i "rom/overlays/" -o "out/overlays/" -p "overlay_patch.xml" -r "rominfo.xml"` | Patches the overlays contained in `rom/overlays` with `overlay_patch.xml` and places the patched overlays in `overlay_patch.xml`. Modifies `rominfo.xml` in place to change the overlay table if necessary. |

An example `overlay.xml` file might look something like:

```xml
<?xml version="1.0"?>
<OverlayPatchDocument xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
    <overlays>
        <overlay name="main_0001">
            <start>020C7660</start>
            <patches>
                <replace location="020C76D4" value="0410A0E3" />
            </patches>
            <append>04002DE5000090E5040090E5030050E3010000AA1E2DA0E3000000EA1E2CA0E304009DE41EFF2FE1</append>
        </overlay>
        <overlay name="main_0004">
            <start>020C7660</start>
            <patches>
                <replace location="020C76D4" value="0410A0E3" />
            </patches>
            <append>04002DE5000090E5040090E5030050E3010000AA1E2DA0E3000000EA1E2CA0E304009DE41EFF2FE1</append>
        </overlay>
    </overlays>
</OverlayPatchDocument>
```

The root element is an `<OverlayPatchDocument>` followed by an array called `<overlays>` containing a series of `<overlay>` objects. The overlay object contains the following elements:

* `<start>` &ndash; The location where the overlay is loaded in memory. This location can be found in the `rominfo.xml`.
* `<patches>` &ndash; An array of `<replace>` elements. `<replace>` elements have the following attributes:
    - `location` &ndash; The location at which the replacement should take place.
    - `value` &ndash; The little-endian encoded binary instruction to replace at that location.
* `<append>` &ndash; An element containing all code that should be appended at the end of the overlay.

## _Search_ an archive for a particular _hex string_
The `hex-search` command searches an archive for a given hex string. Its arguments are:

* `-a` or `--archive` &ndash; Archive to search.
* `-s` or `--search` &ndash; Hex string to search for.

| Example | Function |
|---------|----------|
| `HaruhiChokuretsuCLI hex-search -a "rom/grp.bin" -s "0102030405060708090A0B0C0D0E0F"` | Searches `grp.bin` for the sequence `0102030405060708090A0B0C0D0E0F` and returns and matches.

## Create a _versioned splash screen_
The `version-screen` command accepts an unversioned splash screen image and writes a given version number to it. Its arguments are:

* `-v` or `--version` &ndash; The version to be written on the splash screen. When accepting a version with four components (e.g. 0.2.20220403.1), splits the version into three lines.
* `-s` or `--splash-screen-path` &ndash; The path to the unversioned splash screen image.
* `-f` or `--font-file` &ndash; The font file that the version should be written using. Expects the font face to have the same name as the file name with hyphens replaced with spaces. (e.g. `Nunito-Black.ttf` becomes `Nunito Black`).
* `-o` or `--output-path` &ndash; The path the versioned splash screen should be written to.

| Example | Function |
|---------|----------|
| `HaruhiChokuretsuCLI version-screen -v 0.2 -s "path/to/splash-screen.png" -f "path/to/font.ttf" -o "out/8b7_newpal_tidx0_splash_screen.png"` | Writes version `0.2` using `font.ttf` to `splash-screen.png` and outputs the result to `out/8b7_newpal_tidx0_splash_screen.png`. |