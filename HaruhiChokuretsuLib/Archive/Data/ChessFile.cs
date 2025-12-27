using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using HaruhiChokuretsuLib.Util;
// ReSharper disable InconsistentNaming

namespace HaruhiChokuretsuLib.Archive.Data;

/// <summary>
/// Represents chess data files
/// </summary>
public class ChessFile : DataFile
{
    /// <summary>
    /// The number of moves the player is given to solve the puzzle
    /// </summary>
    public int NumMoves { get; set; }
    /// <summary>
    /// The time limit the player has to solve the puzzle (zero is unlimited)
    /// </summary>
    public int TimeLimit { get; set; }
    /// <summary>
    /// Unknown
    /// </summary>
    public int Unknown08 { get; set; }
    /// <summary>
    /// A 64-byte array representing each space on the chess board from the top left to the bottom right
    /// </summary>
    public ChessPiece[] Chessboard { get; set; }

    /// <summary>
    /// Constructs a blank chess file
    /// </summary>
    public ChessFile()
    {
    }

    /// <inheritdoc/>
    public override void Initialize(byte[] decompressedData, int offset, ILogger log)
    {
        if (IO.ReadInt(decompressedData, 0x00) != 1)
        {
            throw new DataException("Invalid chess file format.");
        }
            
        NumMoves = IO.ReadInt(decompressedData, 0x14);
        TimeLimit = IO.ReadInt(decompressedData, 0x18);
        Unknown08 = IO.ReadInt(decompressedData, 0x1C);
        Chessboard = [.. decompressedData.Skip(0x20).Take(0x40).Select(b => (ChessPiece)b)];
    }

    /// <inheritdoc/>
    public override string GetSource(Dictionary<string, IncludeEntry[]> includes)
    {
        StringBuilder sb = new();

        sb.AppendLine(".word 1");
        sb.AppendLine(".word ENDPOINTERS");
        sb.AppendLine(".word FILESTART");
        sb.AppendLine(".word CHESS");
        sb.AppendLine(".word 1");
        sb.AppendLine();

        sb.AppendLine("FILESTART:");
        sb.AppendLine("CHESS:");
        sb.AppendLine($".word {NumMoves}");
        sb.AppendLine($".word {TimeLimit}");
        sb.AppendLine($".word {Unknown08}");

        for (int i = 0; i < Chessboard.Length; i++)
        {
            sb.AppendLine($".byte 0x{(byte)Chessboard[i]:X2}");
        }

        sb.AppendLine("ENDPOINTERS:");
        sb.AppendLine(".word 0");
            
        return sb.ToString();
    }

    /// <summary>
    /// An enum representing chess pieces as used in the dat.bin chess files
    /// </summary>
    public enum ChessPiece : byte
    {
        /// <summary>
        /// Empty space
        /// </summary>
        Empty = 0,
        /// <summary>
        /// White's king
        /// </summary>
        WhiteKing = 0x01,
        /// <summary>
        /// White's queen
        /// </summary>
        WhiteQueen = 0x02,
        /// <summary>
        /// White's left rook
        /// </summary>
        WhiteRookLeft = 0x03,
        /// <summary>
        /// White's right rook
        /// </summary>
        WhiteRookRight = 0x04,
        /// <summary>
        /// White's left bishop
        /// </summary>
        WhiteBishopLeft = 0x05,
        /// <summary>
        /// White's right bishop
        /// </summary>
        WhiteBishopRight = 0x06,
        /// <summary>
        /// White's left knight
        /// </summary>
        WhiteKnightLeft = 0x07,
        /// <summary>
        /// White's right knight
        /// </summary>
        WhiteKnightRight = 0x08,
        /// <summary>
        /// White's pawn in rank A
        /// </summary>
        WhitePawnA = 0x09,
        /// <summary>
        /// White's pawn in rank B
        /// </summary>
        WhitePawnB = 0x0A,
        /// <summary>
        /// White's pawn in rank C
        /// </summary>
        WhitePawnC = 0x0B,
        /// <summary>
        /// White's pawn in rank D
        /// </summary>
        WhitePawnD = 0x0C,
        /// <summary>
        /// White's pawn in rank E
        /// </summary>
        WhitePawnE = 0x0D,
        /// <summary>
        /// White's pawn in rank F
        /// </summary>
        WhitePawnF = 0x0E,
        /// <summary>
        /// White's pawn in rank G
        /// </summary>
        WhitePawnG = 0x0F,
        /// <summary>
        /// White's pawn in rank H
        /// </summary>
        WhitePawnH = 0x10,
        /// <summary>
        /// Black's king
        /// </summary>
        BlackKing = 0x81,
        /// <summary>
        /// Black's queen
        /// </summary>
        BlackQueen = 0x82,
        /// <summary>
        /// Black's left rook
        /// </summary>
        BlackRookLeft = 0x83,
        /// <summary>
        /// Black's right rook
        /// </summary>
        BlackRookRight = 0x84,
        /// <summary>
        /// Black's left bishop
        /// </summary>
        BlackBishopLeft = 0x85,
        /// <summary>
        /// Black's right bishop
        /// </summary>
        BlackBishopRight = 0x86,
        /// <summary>
        /// Black's left knight
        /// </summary>
        BlackKnightLeft = 0x87,
        /// <summary>
        /// Black's right knight
        /// </summary>
        BlackKnightRight = 0x88,
        /// <summary>
        /// Black's pawn in rank A
        /// </summary>
        BlackPawnA = 0x89,
        /// <summary>
        /// Black's pawn in rank B
        /// </summary>
        BlackPawnB = 0x8A,
        /// <summary>
        /// Black's pawn in rank C
        /// </summary>
        BlackPawnC = 0x8B,
        /// <summary>
        /// Black's pawn in rank D
        /// </summary>
        BlackPawnD = 0x8C,
        /// <summary>
        /// Black's pawn in rank E
        /// </summary>
        BlackPawnE = 0x8D,
        /// <summary>
        /// Black's pawn in rank F
        /// </summary>
        BlackPawnF = 0x8E,
        /// <summary>
        /// Black's pawn in rank G
        /// </summary>
        BlackPawnG = 0x8F,
        /// <summary>
        /// Black's pawn in rank H
        /// </summary>
        BlackPawnH = 0x90,
    }
}