namespace ChessTrainerApp.ViewModels;

public enum GameStatus
{
    InProgress,
    Checkmate,
    Stalemate,
    Draw // (Для правила 50 ходів або нестачі матеріалу - якщо встигнемо)
}