using ChessTrainerApp.Models;

namespace ChessTrainerApp.Services;

public class GameState
{
    public PieceModel[]? BoardSnapshot { get; set; }
    public PieceColor Turn { get; set; }
    public SquareModel? EnPassantTarget { get; set; }
    
    public string? PgnText { get; set; }
    public List<string>? MoveHistory { get; set; }

    public int HalfMoveClock { get; set; }
    public string? PositionHash { get; set; }

    public int LastFromIndex { get; set; }
    public int LastToIndex { get; set; }
}