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
The _Preview_ tab displays the currently selected Shade Texture file 