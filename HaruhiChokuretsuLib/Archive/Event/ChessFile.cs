using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using HaruhiChokuretsuLib.Util;

namespace HaruhiChokuretsuLib.Archive.Event;

public partial class EventFile
{
    /// <summary>
    /// If this file is CHESS.S, contains the chess descriptor data
    /// </summary>
    public ChessFileDescriptor ChessFile { get; set; }
}

/// <summary>
/// Representation of the various chess files described in CHESS.S
/// </summary>
public class ChessFileDescriptor
{
    /// <summary>
    /// The list of chess event files
    /// </summary>
    public List<short> EventFileIndices { get; set; } = [];
    /// <summary>
    /// Structure defining Koizumi's chess puzzles as seen in the extras mode
    /// </summary>
    public ChessCharacterDefinition KoizumiChess { get; set; }
    /// <summary>
    /// Structure defining Mikuru's chess puzzles as seen in the extras mode
    /// </summary>
    public ChessCharacterDefinition MikuruChess { get; set; }
    /// <summary>
    /// Structure defining Tsuruya's chess puzzles as seen in the extras mode
    /// </summary>
    public ChessCharacterDefinition TsuruyaChess { get; set; }
    /// <summary>
    /// Structure defining Nagato's chess puzzles as seen in the extras mode
    /// </summary>
    public ChessCharacterDefinition NagatoChess { get; set; }
    /// <summary>
    /// Structure defining Haruhi's chess puzzles as seen in the extras mode
    /// </summary>
    public ChessCharacterDefinition HaruhiChess { get; set; }

    /// <summary>
    /// Parameterless constructor for serialization
    /// </summary>
    public ChessFileDescriptor()
    {
    }
    
    /// <summary>
    /// Constructs a chess file given CHESS.S data
    /// </summary>
    /// <param name="data">Binary representation of CHESS.S data</param>
    /// <exception cref="DataException">Throws if not provided valid CHESS.S data</exception>
    public ChessFileDescriptor(byte[] data)
    {
        if (IO.ReadInt(data, 0x00) != 7)
        {
            throw new DataException("File is not a valid chess file!");
        }

        int indicesOffset = IO.ReadInt(data, 0x0C);
        int numIndices = IO.ReadInt(data, 0x10);
        for (int i = 0; i < numIndices; i++)
        {
            EventFileIndices.Add(IO.ReadShort(data, indicesOffset + (i * 0x02)));
        }

        KoizumiChess = new(data, IO.ReadInt(data, 0x14));
        MikuruChess = new(data, IO.ReadInt(data, 0x1C));
        TsuruyaChess = new(data, IO.ReadInt(data, 0x24));
        NagatoChess = new(data, IO.ReadInt(data, 0x2C));
        HaruhiChess = new(data, IO.ReadInt(data, 0x34));
    }

    /// <summary>
    /// Gets an ASM source representation of CHESS.S
    /// </summary>
    /// <returns>An ASM source representation of CHESS.S</returns>
    public string GetSource()
    {
        StringBuilder sb = new();

        sb.AppendLine(".word 7");
        sb.AppendLine(".word ENDPOINTERS");
        sb.AppendLine(".word FILESTART");
        sb.AppendLine(".word EVENTFILEINDICES");
        sb.AppendLine($".word {EventFileIndices.Count}");
        sb.AppendLine(".word KOIZUMICHESS");
        sb.AppendLine(".word 1");
        sb.AppendLine(".word MIKURUCHESS");
        sb.AppendLine(".word 1");
        sb.AppendLine(".word TSURUYACHESS");
        sb.AppendLine(".word 1");
        sb.AppendLine(".word NAGATOCHESS");
        sb.AppendLine(".word 1");
        sb.AppendLine(".word HARUHICHESS");
        sb.AppendLine(".word 1");
        sb.AppendLine(".word DEFINITIONSMAP");
        sb.AppendLine(".word 1");
        sb.AppendLine();

        sb.AppendLine("FILESTART:");
        sb.AppendLine("EVENTFILEINDICES:");
        foreach (short eventFileIndex in EventFileIndices)
        {
            sb.AppendLine($".short {eventFileIndex}");
        }
        if (EventFileIndices.Count % 2 == 1)
        {
            sb.AppendLine(".skip 2");
        }
        sb.AppendLine();

        int currentEndPointer = 0;
        sb.AppendLine("KOIZUMICHESS:");
        sb.AppendLine(KoizumiChess.GetSource(ref currentEndPointer));
        sb.AppendLine();
            
        sb.AppendLine("MIKURUCHESS:");
        sb.AppendLine(MikuruChess.GetSource(ref currentEndPointer));
        sb.AppendLine();
            
        sb.AppendLine("TSURUYACHESS:");
        sb.AppendLine(TsuruyaChess.GetSource(ref currentEndPointer));
        sb.AppendLine();
            
        sb.AppendLine("NAGATOCHESS:");
        sb.AppendLine(NagatoChess.GetSource(ref currentEndPointer));
        sb.AppendLine();
            
        sb.AppendLine("HARUHICHESS:");
        sb.AppendLine(HaruhiChess.GetSource(ref currentEndPointer));
        sb.AppendLine();
            
        sb.AppendLine("DEFINITIONSMAP:");
        sb.AppendLine($"ENDPOINTER{currentEndPointer++:D3}: .word KOIZUMICHESS");
        sb.AppendLine($"ENDPOINTER{currentEndPointer++:D3}: .word MIKURUCHESS");
        sb.AppendLine($"ENDPOINTER{currentEndPointer++:D3}: .word TSURUYACHESS");
        sb.AppendLine($"ENDPOINTER{currentEndPointer++:D3}: .word NAGATOCHESS");
        sb.AppendLine($"ENDPOINTER{currentEndPointer++:D3}: .word HARUHICHESS");
        sb.AppendLine();

        sb.AppendLine("ENDPOINTERS:");
        sb.AppendLine($".word {currentEndPointer}");
        for (int i = 0; i < currentEndPointer; i++)
        {
            sb.AppendLine($".word ENDPOINTER{i:D3}");
        }
            
        return sb.ToString();
    }
}

/// <summary>
/// Representation of a chess character definition structure as found in CHESS.S
/// </summary>
public class ChessCharacterDefinition
{
    /// <summary>
    /// The character (speaker) associated with this chess mode
    /// </summary>
    public Speaker Character { get; set; }
    /// <summary>
    /// Boolean indicating whether this character's chess puzzles must be unlocked
    /// </summary>
    public bool InitiallyLocked { get; set; }
    /// <summary>
    /// Number of puzzles this character has (not counting the final celebration event)
    /// </summary>
    public byte NumPuzzles { get; set; }
    /// <summary>
    /// Title displayed on the top screen when viewing this character's puzzles
    /// </summary>
    public string ChessTitle { get; set; }
    /// <summary>
    /// String displayed when this character's puzzles must still be unlocked
    /// </summary>
    public string UnlockString { get; set; }
    /// <summary>
    /// List of chess puzzle objects
    /// </summary>
    public List<ChessPuzzleDefinition> Puzzles { get; set; } = [];

    /// <summary>
    /// Parameterless constructor for serialization
    /// </summary>
    public ChessCharacterDefinition()
    {
    }
    
    /// <summary>
    /// Creates a new chess character definition given the CHESS.S data and a starting offset
    /// </summary>
    /// <param name="data">Binary representation of CHESS.S data</param>
    /// <param name="offset">The offset at which this chess character definition starts</param>
    public ChessCharacterDefinition(byte[] data, int offset)
    {
        Character = (Speaker)IO.ReadShort(data, offset + 0x00);
        InitiallyLocked = data.ElementAt(offset + 0x02) == 0x01;
        NumPuzzles = data.ElementAt(offset + 0x03);
        ChessTitle = IO.ReadShiftJisString(data, IO.ReadInt(data, offset + 0x04));
        UnlockString = IO.ReadShiftJisString(data, IO.ReadInt(data, offset + 0x08));
        for (int i = 0; i < NumPuzzles + 1; i++)
        {
            Puzzles.Add(new(data, offset + 0x0C * (i + 1)));
        }
    }

    /// <summary>
    /// Gets an ASM representation of this character definition and all its associated strings
    /// </summary>
    /// <param name="currentEndPointer">The current end pointer while iterating through the file</param>
    /// <returns>An ASM source representation of this character definition</returns>
    public string GetSource(ref int currentEndPointer)
    {
        StringBuilder sb = new();

        sb.AppendLine($".short {(short)Character}");
        sb.AppendLine($".byte {(InitiallyLocked ? 1 : 0)}");
        sb.AppendLine($".byte {NumPuzzles}");
        sb.AppendLine($"ENDPOINTER{currentEndPointer++:D3}: .word {Character.ToString()}TITLE");
        sb.AppendLine($"ENDPOINTER{currentEndPointer++:D3}: .word {Character.ToString()}UNLOCK");
        for (int i = 0; i < Puzzles.Count; i++)
        {
            sb.AppendLine(Puzzles[i].GetSource(Character, i, ref currentEndPointer));
        }

        if (Puzzles.Count < 16)
        {
            sb.AppendLine($".skip {(16 - Puzzles.Count) * 0x0C}");
        }

        sb.AppendLine($"{Character.ToString()}TITLE: .string \"{ChessTitle.EscapeShiftJIS()}\"");
        sb.AsmPadString(ChessTitle, Encoding.GetEncoding("Shift-JIS"));
        sb.AppendLine($"{Character.ToString()}UNLOCK: .string \"{UnlockString.EscapeShiftJIS()}\"");
        sb.AsmPadString(UnlockString, Encoding.GetEncoding("Shift-JIS"));

        for (int i = 0; i < Puzzles.Count; i++)
        {
            sb.AppendLine($"{Character.ToString()}PUZZLE{i:D2}: .string \"{Puzzles[i].PuzzleTitle.EscapeShiftJIS()}\"");
            sb.AsmPadString(Puzzles[i].PuzzleTitle, Encoding.GetEncoding("Shift-JIS"));
        }
            
        return sb.ToString();
    }
}

/// <summary>
/// A representation of a chess puzzle definition as found in CHESS.S
/// </summary>
public class ChessPuzzleDefinition
{
    /// <summary>
    /// The flag that is set when this puzzle is completed
    /// </summary>
    public short CompletionFlag { get; set; }
    /// <summary>
    /// The evt.bin index of the event file associated with this puzzle
    /// </summary>
    public short EventFileIndex { get; set; }
    /// <summary>
    /// A boolean indicating whether this puzzle is timed (and thus whether to display the clock icon in the menu)
    /// </summary>
    public bool IsTimed { get; set; }
    /// <summary>
    /// The title of the puzzle as seen in the menu
    /// </summary>
    public string PuzzleTitle { get; set; }

    /// <summary>
    /// Parameterless constructor for serialization
    /// </summary>
    public ChessPuzzleDefinition()
    {
    }

    /// <summary>
    /// Constructs a chess puzzle definition given CHESS.S data and an offset into the file
    /// </summary>
    /// <param name="data">Binary representation of CHESS.S data</param>
    /// <param name="offset">The offset at which this puzzle is defined in the file</param>
    public ChessPuzzleDefinition(byte[] data, int offset)
    {
        CompletionFlag = IO.ReadShort(data, offset + 0x00);
        EventFileIndex = IO.ReadShort(data, offset + 0x02);
        IsTimed = IO.ReadInt(data, offset + 0x04) == 1;
        PuzzleTitle = IO.ReadShiftJisString(data, IO.ReadInt(data, offset + 0x08));
    }

    /// <summary>
    /// Gets an ASM source representation of this puzzle definition (not including the title string)
    /// </summary>
    /// <param name="character">The character whose puzzle this is</param>
    /// <param name="currentDefinition">The current definition number while iterating through the file</param>
    /// <param name="currentEndPointer">The current end pointer while iterating through the file</param>
    /// <returns>An ASM source representation of this puzzle definition</returns>
    public string GetSource(Speaker character, int currentDefinition, ref int currentEndPointer)
    {
        StringBuilder sb = new();

        sb.AppendLine($".short {CompletionFlag}");
        sb.AppendLine($".short {EventFileIndex}");
        sb.AppendLine($".word {(IsTimed ? 1 : 0)}");
        sb.AppendLine($"ENDPOINTER{currentEndPointer++:D3}: .word {character.ToString()}PUZZLE{currentDefinition:D2}");
                
        return sb.ToString();
    }
}