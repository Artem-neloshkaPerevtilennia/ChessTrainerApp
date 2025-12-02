using ChessTrainerApp.Models;

namespace ChessTrainerApp.Services;

public class GameState
{
    public PieceModel[] BoardSnapshot { get; set; } // Масив фігур
    public PieceColor Turn { get; set; }            // Чий хід
    public SquareModel EnPassantTarget { get; set; } // Ціль для взяття
    public int LastFromIndex { get; set; } = -1;
    public int LastToIndex { get; set; } = -1;
}