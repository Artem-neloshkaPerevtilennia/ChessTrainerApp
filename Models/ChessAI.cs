using System.Collections.ObjectModel;
using ChessTrainerApp.Models;

namespace ChessTrainerApp.Models;

public static class ChessAI
{
    // Вартість фігур
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

    // Оцінка всієї дошки: (Мої бали) - (Бали ворога)
    public static int EvaluateBoard(IList<SquareModel> board, PieceColor botColor)
    {
        int score = 0;
        foreach (var square in board)
        {
            if (square.Piece.Type == PieceType.None) continue;

            int value = GetPieceValue(square.Piece.Type);

            // Додаткові бали за позицію (центр)
            if (square.Piece.Type == PieceType.Pawn || square.Piece.Type == PieceType.Knight)
            {
                // Якщо фігура в центрі (квадрат d4-e5), даємо трохи бонусів
                if (square.Row > 2 && square.Row < 5 && square.Column > 2 && square.Column < 5)
                    value += 1; // Заохочуємо контроль центру
            }

            if (square.Piece.Color == botColor)
                score += value;
            else
                score -= value;
        }
        return score;
    }

    // Головний метод
    public static Move? GetBestMove(IList<SquareModel> board, PieceColor botColor, int depth, SquareModel enPassantTarget)
    {
        Move? bestMove = null;
        int alpha = int.MinValue;
        int beta = int.MaxValue;
        
        // Отримуємо всі ходи
        var possibleMoves = ChessRules.GetAllLegalMoves(botColor, new ObservableCollection<SquareModel>(board), enPassantTarget);
        
        // ОПТИМІЗАЦІЯ 1: Сортування ходів
        // Спочатку перевіряємо взяття фігур - це збільшує шанс швидкого відсікання
        possibleMoves = OrderMoves(possibleMoves);

        // Якщо ходів немає - повертаємо null (пат/мат)
        if (possibleMoves.Count == 0) return null;

        foreach (var move in possibleMoves)
        {
            var capturedPiece = SimulateMove(move);

            // Викликаємо Minimax з Alpha-Beta
            // false = хід мінімізатора (ворога)
            int boardValue = Minimax(board, depth - 1, alpha, beta, false, botColor, enPassantTarget);

            UndoMove(move, capturedPiece);

            if (boardValue > alpha)
            {
                alpha = boardValue;
                bestMove = move;
            }
        }

        return bestMove;
    }

    // Рекурсивна функція з Alpha-Beta
    private static int Minimax(IList<SquareModel> board, int depth, int alpha, int beta, bool isMaximizingPlayer, PieceColor botColor, SquareModel enPassantTarget)
    {
        if (depth == 0)
        {
            return EvaluateBoard(board, botColor);
        }

        PieceColor currentPlayer = isMaximizingPlayer ? botColor : (botColor == PieceColor.White ? PieceColor.Black : PieceColor.White);
        
        // Тут потрібен cast до ObservableCollection для ChessRules, або зміни ChessRules на IList
        var possibleMoves = ChessRules.GetAllLegalMoves(currentPlayer, new ObservableCollection<SquareModel>(board), enPassantTarget);

        if (possibleMoves.Count == 0) return EvaluateBoard(board, botColor);

        // Сортуємо ходи для ефективності
        possibleMoves = OrderMoves(possibleMoves);

        if (isMaximizingPlayer)
        {
            int maxEval = int.MinValue;
            foreach (var move in possibleMoves)
            {
                var captured = SimulateMove(move);
                int eval = Minimax(board, depth - 1, alpha, beta, false, botColor, enPassantTarget);
                UndoMove(move, captured);

                maxEval = Math.Max(maxEval, eval);
                alpha = Math.Max(alpha, eval);

                // ВІДСІКАННЯ (Pruning)
                if (beta <= alpha) break; 
            }
            return maxEval;
        }
        else
        {
            int minEval = int.MaxValue;
            foreach (var move in possibleMoves)
            {
                var captured = SimulateMove(move);
                int eval = Minimax(board, depth - 1, alpha, beta, true, botColor, enPassantTarget);
                UndoMove(move, captured);

                minEval = Math.Min(minEval, eval);
                beta = Math.Min(beta, eval);

                // ВІДСІКАННЯ (Pruning)
                if (beta <= alpha) break;
            }
            return minEval;
        }
    }

    // ОПТИМІЗАЦІЯ: Спочатку перевіряємо ходи, де ми когось їмо
    private static List<Move> OrderMoves(List<Move> moves)
    {
        return moves.OrderByDescending(m => m.To.Piece.Type != PieceType.None ? 10 : 0).ToList();
    }

    private static PieceModel SimulateMove(Move move)
    {
        var capturedPiece = move.To.Piece;
        move.To.Piece = move.From.Piece;
        move.From.Piece = new PieceModel { Type = PieceType.None, Color = PieceColor.None };
        return capturedPiece;
    }

    private static void UndoMove(Move move, PieceModel capturedPiece)
    {
        move.From.Piece = move.To.Piece;
        move.To.Piece = capturedPiece;
    }
}