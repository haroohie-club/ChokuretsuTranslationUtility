# Haruhi Chokuretsu Editor
The Haruhi Chokuretsu Editor is a WPF application that can be used to interface with Suzumiya Haruhi no Chokuretsu's archive files. This document
will walk you through how to use the program.

The application has four tabs at the top, each of which contain different functions for interacting with particular archives.

## Events
The **Events** tab contains functions for interacting with event files (and string files more generally) in `evt.bin` and `dat.bin`. On the left of the
screen is a list box that, upon opening an archive, will contain a list of all the files in that archive. On the right is the editor pane, which
is itself composed of four tabs: _Dialogue_, _Topics_, _Front Pointers_, and _End Pointers_.

### Tabs

#### Dialogue
The _Dialogue_ tab in the editor pane is contains all of the strings in the file selected in the list box. They are prepended by the speaker's name
and encoded in Shift-JIS encoding. This means that when editing them directly you will need to use an IME in order to type in full-width alphanumeric characters
(assuming you want Latin characters).

#### Topics
The _Topics_ tab contains information on the Topics present in the selected file. In mainline event files, these represent the Topics you can obtain during
that episode. In the Topics file (0x245 / 581), this represents all the topics available in the game. The values displayed are (in order):

* The Topic's unique identifier (hexadecimal)
* The Topic's dialogue index (relevant in the Topics file)
* The Topic's text/name
* The event triggered by selecting the Topic during the Puzzle Phase (decimal followed by hex)

#### Front Pointers
At the top of this tab are the uncompressed lengths, followed by the hex compressed lengths (real and calculated) on the following line.
After this, all of the front pointers are listed and can be modified.

#### End Pointers
All of the end pointers are listed and can be modified.

### Buttons
| Button | Function |
|:------:|----------|
| Open | Opens `evt.bin`. |
| Open DAT | Opens `dat.bin`. |
| Save | Saves the currently open archive. |
| Export | Exports the selected file as uncompressed binary data. |
| Import | Imports an uncompressed binary file from disk and replaces the selected file with it. |
| Export Strings | Exports the strings in the currently selected file as a .NET resource (RESX) file. |
| Import Strings | Imports the a .NET resource (RESX) file and replaces the strings in the currently selected file with those in the RESX. This is the preferred method of modifying a file's strings as it does font replacement work for you. |
| Export All Strings | Exports all strings in from all files in the currently open archive to provided directory. |
| Import All Strings | Imports all strings of a given language code from a folder of .NET resource (RESX) files.
| Export Topics | Exports the topics in a series of files (defined from the currently selected file to a file you specify) to a CSV. Useful for collating which topics are available from in a given episode. |
| Search/Next | Searches through the open archive for a given string. |

## Graphics
The **Graphics** tab allows you to interact with the files in `grp.bin`. On the left of the screen is a list box that, upon opening an archive, will contain a list of all files
in that archive. On the right is the editor pane, which itself is composed of three tabs: _Preview_, _Palette_, and _Statistics_.

### Tabs

#### Tiles/Texture
The _Preview_ tab displays an image representing the currently selected file. For Shade Texture files, it will display the texture or tiles represented by the file. For Layouts, it will display several
controls allowing you to create a layout preview and then edit that layout. The controls are as follows:

* _Start_ &ndash; The starting index of the layout (which layout entry will be the first to be drawn)
* _Length_ &ndash; The number of layout entries to draw after that starting entry.
* _Preview Layout_ &ndash; Pressing this button will render the layout.
* _Dark Mode_ &ndash; Ticking this box before rendering will cause the background to be black, allowing for easy viewing of white/transparent tiles.
* _Total Entries_ &ndash; The total number of layout entries.

After this, the layout preview will be displayed. Below it will be a series of controls for the layout entries. They are as follows:

1. (Non-editable) The layout entry number
2. The relative SHTX index
3. The texture X
4. The texture Y
5. The texture width
6. The texture height
7. The screen X
8. The screen Y
9. The screen width
10. The screen height
11. The tint color

Editing these values will change the values in the layout. These can then be saved or exported to preserve these changes.

#### Palette
The _Palette_ tab allows you to view the palette of a selected Shade Texture file. The palette data is show left to right, top to bottom.

#### Statistics
The _Statistics_ tab is independent of the listbox selection and shows stats on the presence of certain byte combinations in the headers of Shade Texture files.

### Buttons
| Button | Function |
|:------:|----------|
| Open | Opens `grp.bin`. |
| Save | Saves the currently open archive. |
| Export | Exports the selected file as uncompressed binary data. |
| Import | Imports an uncompressed binary file from disk and replaces the selected file with it. |
| Export Image | Exports currently selected image as a PNG file. |
| Import Image | Imports a PNG file and sets the contents of the currently selected file to match it. |
| Import Image with Palette | Same as Import Image but additionally generates a palette from the PNG file and replaces the palette data with it. |
| Add Image | Adds an image to the archive.

## Data
The **Data** tab is intended for viewing content files in `dat.bin`. Currently, it can only open and save `dat.bin` and import and export data files.
The editor view only displays the file length.

## Compression
The **Compression** tab simply has two buttons which will compress or decompress files on disk using the Shade compression algorithm.