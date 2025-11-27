using System.Collections.ObjectModel;

namespace ChessTrainerApp.Models;

public static class ChessRules
{
    // перевірка чи можливий даний хід геометрично
    public static bool IsBasicMoveValid(SquareModel from, SquareModel to, IList<SquareModel> board, SquareModel enPassantTarget = null)
    {
        // не можна ходити на клітинку, зайняту своєю фігурою
        if (to.Piece.Type != PieceType.None && to.Piece.Color == from.Piece.Color) return false;

        int dRow = to.Row - from.Row;
        int dCol = to.Column - from.Column;

        return from.Piece.Type switch
        {
            PieceType.Pawn => ValidatePawn(from, to, dRow, dCol, board, enPassantTarget),
            PieceType.Knight => ValidateKnight(dRow, dCol),
            PieceType.Rook => ValidateRook(from, to, board),
            PieceType.Bishop => ValidateBishop(from, to, board),
            PieceType.Queen => ValidateRook(from, to, board) || ValidateBishop(from, to, board),
            PieceType.King => ValidateKing(from, to, board),
            
            _ => false
        };
    }

    // Кінь
    private static bool ValidateKnight(int dRow, int dCol)
    {
        // кінь ходить на 2 клітинки в один бік і 1 в інший
        return (Math.Abs(dRow) == 2 && Math.Abs(dCol) == 1) || 
               (Math.Abs(dRow) == 1 && Math.Abs(dCol) == 2);
    }

    // Король
    // перевірка рокировки
    private static bool CanCastle(SquareModel kingSquare, SquareModel destSquare, IList<SquareModel> board)
    {
        // якщо король вже ходив, то рокировка не є можливою
        if (kingSquare.Piece.HasMoved) return false;

        // коротка чи довга рокировка
        int direction = destSquare.Column - kingSquare.Column > 0 ? 1 : -1; // 1 = Short (Kingside), -1 = Long (Queenside)
        
        // Шукаємо туру
        int rookCol = direction == 1 ? 7 : 0;
        var rookSquare = GetSquare(board, kingSquare.Row, rookCol);
        
        // якщо тура ходила, то рокировка не є можливою
        if (rookSquare.Piece.Type != PieceType.Rook || rookSquare.Piece.HasMoved) return false;

        // Перевіряємо, чи пустий простір між королем і турою
        int col = kingSquare.Column + direction;
        while (col != rookCol)
        {
            if (GetSquare(board, kingSquare.Row, col).Piece.Type != PieceType.None) return false;
            col += direction;
        }

        // під шахом рокируватись не можна
        var enemyColor = kingSquare.Piece.Color == PieceColor.White ? PieceColor.Black : PieceColor.White;
        if (IsSquareUnderAttack(kingSquare, enemyColor, board)) return false;

        // після рокировки тура не може бути під боєм
        var middleSquare = GetSquare(board, kingSquare.Row, kingSquare.Column + direction);
        if (IsSquareUnderAttack(middleSquare, enemyColor, board)) return false;

        // Кінцева клітинка короля перевіряється в IsMoveValid

        return true;
    }

    private static bool ValidateKing(SquareModel from, SquareModel to, IList<SquareModel> board)
    {
        int dRow = to.Row - from.Row;
        int dCol = to.Column - from.Column;

        // звичайний хід на 1 клітинку
        if (Math.Abs(dRow) <= 1 && Math.Abs(dCol) <= 1)
            return true;

        // рокировка
        if (dRow == 0 && Math.Abs(dCol) == 2)
            return CanCastle(from, to, board);

        return false;
    }

    // пішак
    private static bool ValidatePawn(SquareModel from, SquareModel to, int dRow, int dCol, IList<SquareModel> board, SquareModel enPassantTarget)
    {
        var direction = from.Piece.Color == PieceColor.White ? -1 : 1;

        // звичайний хід на 1 клітинку
        if (dCol == 0 && dRow == direction)
            return to.Piece.Type == PieceType.None;

        // хід на 2 клітинки
        bool isStartRow = (from.Piece.Color == PieceColor.White && from.Row == 6) ||
                        (from.Piece.Color == PieceColor.Black && from.Row == 1);

        if (isStartRow && dCol == 0 && dRow == 2 * direction)
        {
            // перевіряємо наявність фігур посеред шляху
            var intermediateSquare = GetSquare(board, from.Row + direction, from.Column);
            return to.Piece.Type == PieceType.None && intermediateSquare.Piece.Type == PieceType.None;
        }

        // взяття іншої фігури пішаком
        if (Math.Abs(dCol) == 1 && dRow == direction)
        {
            // звичайне взяття
            if (to.Piece.Type != PieceType.None && to.Piece.Color != from.Piece.Color)
                return true;

            // взяття на проході
            if (to == enPassantTarget)
                return true;
        }

        return false;
    }

    // далекобійні фігури
    // чи чистий шлях між точками
    private static bool IsPathClear(SquareModel from, SquareModel to, IList<SquareModel> board)
    {
        int dRow = Math.Sign(to.Row - from.Row); // -1, 0 або 1
        int dCol = Math.Sign(to.Column - from.Column);

        int currentRow = from.Row + dRow;
        int currentCol = from.Column + dCol;

        while (currentRow != to.Row || currentCol != to.Column)
        {
            var square = GetSquare(board, currentRow, currentCol);
            if (square.Piece.Type != PieceType.None) return false; // Перешкода!

            currentRow += dRow;
            currentCol += dCol;
        }
        return true;
    }

    // тура
    private static bool ValidateRook(SquareModel from, SquareModel to, IList<SquareModel> board)
    {
        // Тура ходить тільки прямо (змінюється або рядок, або стовпець, але не обидва)
        if (from.Row != to.Row && from.Column != to.Column) return false;
        return IsPathClear(from, to, board);
    }

    // слон
    private static bool ValidateBishop(SquareModel from, SquareModel to, IList<SquareModel> board)
    {
        // Слон ходить по діагоналі (зміна рядка дорівнює зміні стовпця)
        if (Math.Abs(to.Row - from.Row) != Math.Abs(to.Column - from.Column)) return false;
        return IsPathClear(from, to, board);
    }

    // для валідації ферзя можна використати методи для тури та слона разом

    // допоміжний метод для пошуку клітинки в масиві
    private static SquareModel GetSquare(IList<SquareModel> board, int row, int col)
    {
        // Оскільки це одновимірний масив 8x8, індекс = row * 8 + col
        return board[row * 8 + col];
    }

    // чи атакована клітина іншою фігурою
    public static bool IsSquareUnderAttack(SquareModel targetSquare, PieceColor attackerColor, IList<SquareModel> board)
    {
        // Проходимо по всіх клітинках дошки
        foreach (var square in board)
        {
            // Якщо це фігура ворога (атакуючого)
            if (square.Piece.Type != PieceType.None && square.Piece.Color == attackerColor)
            {
                // Перевіряємо, чи може ця фігура "з'їсти" фігуру на цільовій клітинці
                if (IsBasicMoveValid(square, targetSquare, board)) 
                {
                    return true;
                }
            }
        }
        return false;
    }

    // остаточна перевірка валідності ходу
    public static bool IsMoveValid(SquareModel from, SquareModel to, IList<SquareModel> board, SquareModel enPassantTarget = null)
    {
        // перевірка геометрії
        if (!IsBasicMoveValid(from, to, board, enPassantTarget)) return false;

        // симуляцію ходу для перевірки на шах
        // Зберігаємо старі дані, щоб потім відкотити
        var originalPieceOnTo = to.Piece;
        var movingPiece = from.Piece;
        var myColor = movingPiece.Color;

        // Тимчасово робимо хід
        to.Piece = movingPiece;
        from.Piece = new PieceModel { Type = PieceType.None, Color = PieceColor.None };

        // знаходимо, де тепер король гравця, що походив
        var myKingSquare = FindKing(myColor, board);

        // перевірка чи він під атакою ворога
        var enemyColor = myColor == PieceColor.White ? PieceColor.Black : PieceColor.White;
        bool isKingInCheck = IsSquareUnderAttack(myKingSquare, enemyColor, board);

        // відкочуємо зміни назад
        from.Piece = movingPiece;
        to.Piece = originalPieceOnTo;

        // Якщо король під шахом після цього ходу — хід нелегальний
        return !isKingInCheck;
    }

    // пошук розташування короля
    private static SquareModel FindKing(PieceColor color, IList<SquareModel> board)
    {
        return board.FirstOrDefault(s => s.Piece.Type == PieceType.King && s.Piece.Color == color);
    }

    // чи є у гравця взагалі ходи
    public static bool HasAnyLegalMove(PieceColor color, IList<SquareModel> board, SquareModel enPassantTarget)
    {
        // всі фігури гравця
        var myPiecesSquares = board.Where(s => s.Piece.Type != PieceType.None && s.Piece.Color == color).ToList();

        // перебираємо всі фігури
        foreach (var fromSquare in myPiecesSquares)
        {
            // пошук потенційних клітинок для кожної фігури
            foreach (var toSquare in board)
            {
                // якщо хід легальний
                if (IsMoveValid(fromSquare, toSquare, board, enPassantTarget))
                {
                    return true;
                }
            }
        }

        return false; // якщо ходів нема, то мат або пат
    }

    // всі можливі ходи (для ШІ)
    public static List<Move> GetAllLegalMoves(PieceColor color, IList<SquareModel> board, SquareModel enPassantTarget)
    {
        var moves = new List<Move>();
        var myPieces = board.Where(s => s.Piece.Type != PieceType.None && s.Piece.Color == color);

        foreach (var fromSq in myPieces)
        {
            foreach (var toSq in board)
            {
                if (IsMoveValid(fromSq, toSq, board, enPassantTarget))
                {
                    moves.Add(new Move { From = fromSq, To = toSq });
                }
            }
        }
        return moves;
    }
}