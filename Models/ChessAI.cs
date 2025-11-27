using System.Collections.ObjectModel;
using ChessTrainerApp.Models;

namespace ChessTrainerApp.Models;

public static class ChessAI
{
    // вартість фігур
    private static int GetPieceValue(PieceType type)
    {
        return type switch
        {
            PieceType.Pawn => 10,
            PieceType.Knight => 30,
            PieceType.Bishop => 30,
            PieceType.Rook => 50,
            PieceType.Queen => 90,
            PieceType.King => 900,
            _ => 0
        };
    }

    // оцінка всієї позиції: (Мої бали) - (Бали ворога)
    public static int EvaluateBoard(IList<SquareModel> board, PieceColor botColor)
    {
        int score = 0;
        foreach (var square in board)
        {
            if (square.Piece.Type == PieceType.None) continue;

            int value = GetPieceValue(square.Piece.Type);

            // якщо фігура належить боту, то він покращує свою оцінку
            if (square.Piece.Color == botColor)
                score += value;
            else
                score -= value;
        }
        return score;
    }

    // метод отримання найкращого ходу 
    public static Move? GetBestMove(IList<SquareModel> board, PieceColor botColor, int depth, SquareModel enPassantTarget)
    {
        Move? bestMove = null;
        int bestValue = int.MinValue;
        
        // всі можливі ходи
        var possibleMoves = ChessRules.GetAllLegalMoves(botColor, board, enPassantTarget);
        
        // перемішуємо ходи, щоб бот не був передбачуваним при однакових оцінках
        Random rng = new Random();
        possibleMoves = possibleMoves.OrderBy(a => rng.Next()).ToList();

        foreach (var move in possibleMoves)
        {
            // симуляція ходу
            var capturedPiece = SimulateMove(move);

            // рекурсивний виклик ходу за суперника
            // false означає, що зараз хід мінімізатора
            int boardValue = Minimax(board, depth - 1, false, botColor, enPassantTarget);

            // відкат змін
            UndoMove(move, capturedPiece);

            // чи є розглянений хід найкращим
            if (boardValue > bestValue)
            {
                bestValue = boardValue;
                bestMove = move;
            }
        }

        return bestMove;
    }

    // minimax алгоритм
    private static int Minimax(IList<SquareModel> board, int depth, bool isMaximizingPlayer, PieceColor botColor, SquareModel enPassantTarget)
    {
        // базовий випадок - глибина 0
        if (depth == 0)
            return EvaluateBoard(board, botColor);

        PieceColor currentPlayer = isMaximizingPlayer ? botColor : (botColor == PieceColor.White ? PieceColor.Black : PieceColor.White);
        var possibleMoves = ChessRules.GetAllLegalMoves(currentPlayer, board, enPassantTarget);

        // якщо ходів нема, то маємо мат або пат
        if (possibleMoves.Count == 0) return EvaluateBoard(board, botColor);

        if (isMaximizingPlayer) // хід бота-максимізатора
        {
            int bestVal = int.MinValue;
            foreach (var move in possibleMoves)
            {
                var captured = SimulateMove(move);
                int value = Minimax(board, depth - 1, false, botColor, enPassantTarget);
                UndoMove(move, captured);
                bestVal = Math.Max(bestVal, value);
            }
            return bestVal;
        }
        else // хід гравця-мінімізатора
        {
            int bestVal = int.MaxValue;
            foreach (var move in possibleMoves)
            {
                var captured = SimulateMove(move);
                int value = Minimax(board, depth - 1, true, botColor, enPassantTarget);
                UndoMove(move, captured);
                bestVal = Math.Min(bestVal, value);
            }
            return bestVal;
        }
    }

    // симуляція ходу
    private static PieceModel SimulateMove(Move move)
    {
        var capturedPiece = move.To.Piece; // запам'ятовуємо, що з'їли
        move.To.Piece = move.From.Piece;
        move.From.Piece = new PieceModel { Type = PieceType.None, Color = PieceColor.None };
        return capturedPiece;
    }

    // відкат ходу назад
    private static void UndoMove(Move move, PieceModel capturedPiece)
    {
        move.From.Piece = move.To.Piece;
        move.To.Piece = capturedPiece; // Повертаємо з'їдену фігуру
    }
}