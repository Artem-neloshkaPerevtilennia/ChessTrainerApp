using ChessTrainerApp.Models;

namespace ChessTrainerApp.Services;

public class GameState
{
    public PieceModel[] BoardSnapshot { get; set; }
    public PieceColor Turn { get; set; }
    public SquareModel EnPassantTarget { get; set; }
    
    public string PgnText { get; set; }
    public List<string> MoveHistory { get; set; }

    public int HalfMoveClock { get; set; } // Для 50 ходів
    public string PositionHash { get; set; } // Для повторень

    public int LastFromIndex { get; set; } // Для підсвітки
    public int LastToIndex { get; set; }
}