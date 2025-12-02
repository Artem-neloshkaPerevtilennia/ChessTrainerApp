using System.Collections.ObjectModel;
using ChessTrainerApp.Services;

namespace ChessTrainerApp.Models;

public static class ChessAI
{
    private const int MATE_SCORE = 100000;

    // Вартість фігур
    private static int GetPieceValue(PieceType type)
    {
        return type switch
        {
            PieceType.Pawn => 100,
            PieceType.Knight => 320,
            PieceType.Bishop => 330,
            PieceType.Rook => 500,
            PieceType.Queen => 900,
            PieceType.King => 20000,
            _ => 0
        };
    }

    public static int EvaluateBoard(IList<SquareModel> board, PieceColor botColor)
    {
        int score = 0;
        foreach (var square in board)
        {
            if (square.Piece.Type == PieceType.None) continue;

            int value = GetPieceValue(square.Piece.Type);

            // Позиційні таблиці (з попереднього уроку)
            int index = square.Row * 8 + square.Column;
            int tableIndex = square.Piece.Color == PieceColor.White ? (7 - square.Row) * 8 + square.Column : index;
            int positionBonus = 0;

            switch (square.Piece.Type)
            {
                case PieceType.Pawn: positionBonus = EvaluationTables.PawnTable[tableIndex]; break;
                case PieceType.Knight: positionBonus = EvaluationTables.KnightTable[tableIndex]; break;
                case PieceType.Bishop: positionBonus = EvaluationTables.BishopTable[tableIndex]; break;
                case PieceType.Rook: positionBonus = EvaluationTables.RookTable[tableIndex]; break;
                case PieceType.Queen: positionBonus = EvaluationTables.QueenTable[tableIndex]; break;
                case PieceType.King: positionBonus = EvaluationTables.KingMiddleGameTable[tableIndex]; break;
            }
            value += positionBonus;

            if (square.Piece.Color == botColor) score += value;
            else score -= value;
        }
        return score;
    }

    public static Move? GetBestMove(IList<SquareModel> originalBoard, PieceColor botColor, int maxDepth, SquareModel enPassantTarget)
    {
        int pieceCount = originalBoard.Count(s => s.Piece.Type != PieceType.None);
        if (pieceCount >= 32) maxDepth = 2; // Прискорення дебюту

        Move? bestMove = null;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        long timeLimitMs = 2500; // Даємо 2.5 сек на роздуми

        for (int currentDepth = 1; currentDepth <= maxDepth; currentDepth++)
        {
            if (stopwatch.ElapsedMilliseconds > timeLimitMs && bestMove != null) break;

            Move? currentDepthBestMove = null;
            int globalBestValue = int.MinValue;
            object lockObj = new object();

            var possibleMoves = ChessRules.GetAllLegalMoves(botColor, new List<SquareModel>(originalBoard), enPassantTarget);
            
            // Сортуємо ходи перед розподілом потоків
            possibleMoves = OrderMoves(possibleMoves, originalBoard, botColor); 

            if (possibleMoves.Count == 0) break;

            Parallel.ForEach(possibleMoves, (move, state) =>
            {
                if (stopwatch.ElapsedMilliseconds > timeLimitMs) state.Stop();

                var localBoard = DeepCloneBoard(originalBoard);
                var localMove = MapMoveToBoard(move, localBoard);
                
                SquareModel localEnPassant = null;
                if (enPassantTarget != null)
                    localEnPassant = localBoard[enPassantTarget.Row * 8 + enPassantTarget.Column];

                var capturedPiece = SimulateMove(localMove);

                int boardValue = Minimax(localBoard, currentDepth - 1, int.MinValue, int.MaxValue, false, botColor, localEnPassant);

                UndoMove(localMove, capturedPiece);

                lock (lockObj)
                {
                    if (boardValue > globalBestValue)
                    {
                        globalBestValue = boardValue;
                        currentDepthBestMove = move;
                    }
                }
            });

            if (stopwatch.ElapsedMilliseconds <= timeLimitMs || bestMove == null)
            {
                bestMove = currentDepthBestMove;
            }
        }
        return bestMove;
    }

    private static int Minimax(IList<SquareModel> board, int depth, int alpha, int beta, bool isMaximizingPlayer, PieceColor botColor, SquareModel enPassantTarget)
    {
        PieceColor currentPlayer = isMaximizingPlayer ? botColor : (botColor == PieceColor.White ? PieceColor.Black : PieceColor.White);
        var possibleMoves = ChessRules.GetAllLegalMoves(currentPlayer, board, enPassantTarget);

        if (possibleMoves.Count == 0)
        {
            if (IsKingInCheck(currentPlayer, board))
                return isMaximizingPlayer ? -MATE_SCORE - depth : MATE_SCORE + depth;
            return 0;
        }

        if (depth == 0) return EvaluateBoard(board, botColor);

        // 🧠 СОРТУВАННЯ ХОДІВ (Твій запит)
        // Ми сортуємо і тут, всередині рекурсії, щоб прискорити відсікання
        possibleMoves = OrderMoves(possibleMoves, board, currentPlayer);

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
                if (beta <= alpha) break;
            }
            return minEval;
        }
    }

    // 🔥 ГОЛОВНА ФІШКА: ПРІОРИТЕТНІСТЬ ХОДІВ 🔥
    private static List<Move> OrderMoves(List<Move> moves, IList<SquareModel> board, PieceColor attackerColor)
    {
        // Ми присвоюємо кожному ходу бал "цікавості". Чим більше - тим раніше перевіряємо.
        return moves.OrderByDescending(move => 
        {
            int score = 0;

            // 1. ВЗЯТТЯ (Captures) - MVV-LVA
            // Найцінніша жертва найменшою ціною.
            if (move.To.Piece.Type != PieceType.None)
            {
                score += 10 * GetPieceValue(move.To.Piece.Type) - GetPieceValue(move.From.Piece.Type);
            }

            // 2. ПЕРЕТВОРЕННЯ (Promotion)
            if (move.From.Piece.Type == PieceType.Pawn && (move.To.Row == 0 || move.To.Row == 7))
            {
                score += 900; // Це майже як отримати ферзя
            }

            // 3. ШАХ (Checks) - Це те, що ти просив!
            // Нам треба симулювати хід, щоб дізнатися, чи це шах.
            // Це трохи дорого, тому робимо це швидко.
            var captured = SimulateMove(move);
            var enemyColor = attackerColor == PieceColor.White ? PieceColor.Black : PieceColor.White;
            if (IsKingInCheck(enemyColor, board))
            {
                score += 5000; // Шах має ВЕЛИЧЕЗНИЙ пріоритет (вище за звичайні взяття)
            }
            UndoMove(move, captured);

            // 4. НАПАДИ (Attacks) - Спрощено
            // Якщо ми ставимо фігуру на позицію, яка вважається сильною (за EvaluationTables)
            // (Це частково покриває "напади", бо таблиці заохочують активні поля)
            
            return score;
        }).ToList();
    }

    private static bool IsKingInCheck(PieceColor kingColor, IList<SquareModel> board)
    {
        var kingSquare = board.FirstOrDefault(s => s.Piece.Type == PieceType.King && s.Piece.Color == kingColor);
        if (kingSquare == null) return true;
        
        var enemyColor = kingColor == PieceColor.White ? PieceColor.Black : PieceColor.White;
        return ChessRules.IsSquareUnderAttack(kingSquare, enemyColor, board);
    }

    private static List<SquareModel> DeepCloneBoard(IList<SquareModel> original)
    {
        var clone = new List<SquareModel>(64);
        foreach (var sq in original)
        {
            clone.Add(new SquareModel
            {
                Row = sq.Row, Column = sq.Column, SquareColor = sq.SquareColor,
                Piece = new PieceModel { Type = sq.Piece.Type, Color = sq.Piece.Color, HasMoved = sq.Piece.HasMoved }
            });
        }
        return clone;
    }

    private static Move MapMoveToBoard(Move originalMove, IList<SquareModel> targetBoard)
    {
        return new Move 
        { 
            From = targetBoard[originalMove.From.Row * 8 + originalMove.From.Column],
            To = targetBoard[originalMove.To.Row * 8 + originalMove.To.Column]
        };
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
