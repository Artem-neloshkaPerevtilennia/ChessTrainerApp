using System.Collections.ObjectModel;

namespace ChessTrainerApp.Models; // або Services

public static class ChessRules
{
    // Додали необов'язковий параметр enPassantTarget
    public static bool IsBasicMoveValid(SquareModel from, SquareModel to, ObservableCollection<SquareModel> board, SquareModel enPassantTarget = null)
    {
        // 1. Не можна ходити на клітинку, зайняту своєю фігурою
        if (to.Piece.Type != PieceType.None && to.Piece.Color == from.Piece.Color) return false;

        int dRow = to.Row - from.Row;
        int dCol = to.Column - from.Column;

        return from.Piece.Type switch
        {
            // Передаємо enPassantTarget
            PieceType.Pawn => ValidatePawn(from, to, dRow, dCol, board, enPassantTarget),
            
            // Кінь залишається простим
            PieceType.Knight => ValidateKnight(dRow, dCol),
            
            // Тура, Слон, Ферзь залишаються як були
            PieceType.Rook => ValidateRook(from, to, board),
            PieceType.Bishop => ValidateBishop(from, to, board),
            PieceType.Queen => ValidateRook(from, to, board) || ValidateBishop(from, to, board),
            
            // Королю тепер потрібна дошка та координати для перевірки Рокировки
            PieceType.King => ValidateKing(from, to, board),
            
            _ => false
        };
    }

    // 🐴 КІНЬ (Найпростіший)
    private static bool ValidateKnight(int dRow, int dCol)
    {
        // Кінь ходить літерою "Г": 2 клітинки в один бік і 1 в інший
        return (Math.Abs(dRow) == 2 && Math.Abs(dCol) == 1) || 
               (Math.Abs(dRow) == 1 && Math.Abs(dCol) == 2);
    }

    // 👑 КОРОЛЬ
    // метод для перевірки шляху рокировки
    private static bool CanCastle(SquareModel kingSquare, SquareModel destSquare, ObservableCollection<SquareModel> board)
    {
        if (kingSquare.Piece.HasMoved) return false;

        // Визначаємо напрямок (вліво чи вправо)
        int direction = destSquare.Column - kingSquare.Column > 0 ? 1 : -1; // 1 = Short (Kingside), -1 = Long (Queenside)
        
        // Шукаємо туру
        int rookCol = direction == 1 ? 7 : 0;
        var rookSquare = GetSquare(board, kingSquare.Row, rookCol);
        
        if (rookSquare.Piece.Type != PieceType.Rook || rookSquare.Piece.HasMoved) return false;

        // Перевіряємо, чи пустий простір між королем і турою
        // Для короткої рокировки: клітинки col 5 і 6. Для довгої: 1, 2, 3.
        int col = kingSquare.Column + direction;
        while (col != rookCol)
        {
            if (GetSquare(board, kingSquare.Row, col).Piece.Type != PieceType.None) return false;
            col += direction;
        }

        // ВАЖЛИВО: Не можна робити рокировку з-під шаху, через шах, або під шах.
        // 1. Чи зараз шах?
        var enemyColor = kingSquare.Piece.Color == PieceColor.White ? PieceColor.Black : PieceColor.White;
        if (IsSquareUnderAttack(kingSquare, enemyColor, board)) return false;

        // 2. Чи бите поле, через яке перестрибуємо?
        var middleSquare = GetSquare(board, kingSquare.Row, kingSquare.Column + direction);
        if (IsSquareUnderAttack(middleSquare, enemyColor, board)) return false;

        // (Кінцева клітинка перевіряється в IsMoveValid)

        return true;
    }

    private static bool ValidateKing(SquareModel from, SquareModel to, ObservableCollection<SquareModel> board)
    {
        int dRow = to.Row - from.Row;
        int dCol = to.Column - from.Column;

        // 1. Стандартний хід (на 1 клітинку в будь-який бік)
        if (Math.Abs(dRow) <= 1 && Math.Abs(dCol) <= 1)
        {
            return true; 
        }

        // 2. Рокировка (Тільки по горизонталі, рівно на 2 клітинки)
        if (dRow == 0 && Math.Abs(dCol) == 2)
        {
            return CanCastle(from, to, board);
        }

        return false;
    }

    // ♟️ ПІШАК (Трохи складніше)
    private static bool ValidatePawn(SquareModel from, SquareModel to, int dRow, int dCol, ObservableCollection<SquareModel> board, SquareModel enPassantTarget)
    {
        var direction = from.Piece.Color == PieceColor.White ? -1 : 1;

        // 1. Звичайний хід вперед (на 1 клітинку)
        if (dCol == 0 && dRow == direction)
        {
            return to.Piece.Type == PieceType.None;
        }

        // 2. Подвійний хід зі старту
        bool isStartRow = (from.Piece.Color == PieceColor.White && from.Row == 6) ||
                        (from.Piece.Color == PieceColor.Black && from.Row == 1);

        if (isStartRow && dCol == 0 && dRow == 2 * direction)
        {
            // Перевіряємо проміжну клітинку
            var intermediateSquare = GetSquare(board, from.Row + direction, from.Column);
            return to.Piece.Type == PieceType.None && intermediateSquare.Piece.Type == PieceType.None;
        }

        // 3. Взяття (по діагоналі)
        if (Math.Abs(dCol) == 1 && dRow == direction)
        {
            // Або там стоїть ворог...
            if (to.Piece.Type != PieceType.None && to.Piece.Color != from.Piece.Color)
                return true;

            // ...АБО це клітинка En Passant (вона пуста, але ми можемо туди піти)
            if (to == enPassantTarget)
                return true;
        }

        return false;
    }

    // Допоміжний метод для Важких фігур (Тура, Слон, Ферзь)
    // Перевіряє, чи чистий шлях між точками
    private static bool IsPathClear(SquareModel from, SquareModel to, ObservableCollection<SquareModel> board)
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

    private static bool ValidateRook(SquareModel from, SquareModel to, ObservableCollection<SquareModel> board)
    {
        // Тура ходить тільки прямо (змінюється або рядок, або стовпець, але не обидва)
        if (from.Row != to.Row && from.Column != to.Column) return false;
        return IsPathClear(from, to, board);
    }

    private static bool ValidateBishop(SquareModel from, SquareModel to, ObservableCollection<SquareModel> board)
    {
        // Слон ходить по діагоналі (зміна рядка дорівнює зміні стовпця)
        if (Math.Abs(to.Row - from.Row) != Math.Abs(to.Column - from.Column)) return false;
        return IsPathClear(from, to, board);
    }

    // Допоміжний метод для пошуку клітинки в масиві
    private static SquareModel GetSquare(ObservableCollection<SquareModel> board, int row, int col)
    {
        // Оскільки це одновимірний масив 8x8, індекс = row * 8 + col
        return board[row * 8 + col];
    }

    // У ChessTrainerApp.Models.ChessRules

    public static bool IsSquareUnderAttack(SquareModel targetSquare, PieceColor attackerColor, ObservableCollection<SquareModel> board)
    {
        // Проходимо по всіх клітинках дошки
        foreach (var square in board)
        {
            // Якщо це фігура ворога (атакуючого)
            if (square.Piece.Type != PieceType.None && square.Piece.Color == attackerColor)
            {
                // Перевіряємо, чи може ця фігура "з'їсти" фігуру на цільовій клітинці
                // Важливо: тут ми викликаємо нашу базову логіку ходів (IsMoveValid)
                // Але ми не передаємо 'checkKingSafety', щоб уникнути нескінченної рекурсії
                if (IsBasicMoveValid(square, targetSquare, board)) 
                {
                    return true;
                }
            }
        }
        return false;
    }

    public static bool IsMoveValid(SquareModel from, SquareModel to, ObservableCollection<SquareModel> board, SquareModel enPassantTarget = null)
    {
        // 1. Спочатку перевіряємо базову геометрію (чи ходить так кінь/тура тощо)
        if (!IsBasicMoveValid(from, to, board, enPassantTarget)) return false;

        // 2. СИМУЛЯЦІЯ ХОДУ для перевірки на Шах
        // Зберігаємо старі дані, щоб потім відкотити
        var originalPieceOnTo = to.Piece;
        var movingPiece = from.Piece;
        var myColor = movingPiece.Color;

        // Тимчасово робимо хід
        to.Piece = movingPiece;
        from.Piece = new PieceModel { Type = PieceType.None, Color = PieceColor.None };

        // 3. Знаходимо, де тепер МІЙ король
        var myKingSquare = FindKing(myColor, board);

        // 4. Перевіряємо, чи він під атакою ворога
        var enemyColor = myColor == PieceColor.White ? PieceColor.Black : PieceColor.White;
        bool isKingInCheck = IsSquareUnderAttack(myKingSquare, enemyColor, board);

        // 5. ВІДКОЧУЄМО ЗМІНИ НАЗАД (Обов'язково!)
        from.Piece = movingPiece;
        to.Piece = originalPieceOnTo;

        // Якщо король під шахом після цього ходу — хід нелегальний
        return !isKingInCheck;
    }

    // Допоміжний метод для пошуку короля
    private static SquareModel FindKing(PieceColor color, ObservableCollection<SquareModel> board)
    {
        return board.FirstOrDefault(s => s.Piece.Type == PieceType.King && s.Piece.Color == color);
    }


    public static bool HasAnyLegalMove(PieceColor color, ObservableCollection<SquareModel> board, SquareModel enPassantTarget)
    {
        // 1. Знаходимо всі фігури гравця
        var myPiecesSquares = board.Where(s => s.Piece.Type != PieceType.None && s.Piece.Color == color).ToList();

        // 2. Для кожної фігури...
        foreach (var fromSquare in myPiecesSquares)
        {
            // 3. ...перебираємо всі клітинки дошки як потенційні цілі
            foreach (var toSquare in board)
            {
                // Якщо хід легальний (це враховує і захист від шаху!)
                if (IsMoveValid(fromSquare, toSquare, board, enPassantTarget))
                {
                    return true; // Знайшли хоча б один порятунок -> граємо далі
                }
            }
        }

        return false; // Жодного ходу немає -> приїхали (Мат або Пат)
    }
}