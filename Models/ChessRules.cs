using System.Collections.ObjectModel;

namespace ChessTrainerApp.Models; // або Services

public static class ChessRules
{
    public static bool IsBasicMoveValid(SquareModel from, SquareModel to, ObservableCollection<SquareModel> board)
    {
        // 1. Не можна ходити на клітинку, зайняту своєю фігурою
        if (to.Piece.Color == from.Piece.Color) return false;

        int dRow = to.Row - from.Row; // Різниця по рядках
        int dCol = to.Column - from.Column; // Різниця по стовпцях

        return from.Piece.Type switch
        {
            PieceType.Pawn => ValidatePawn(from, to, dRow, dCol, board),
            PieceType.Knight => ValidateKnight(dRow, dCol),
            PieceType.Rook => ValidateRook(from, to, board),
            PieceType.Bishop => ValidateBishop(from, to, board),
            PieceType.Queen => ValidateRook(from, to, board) || ValidateBishop(from, to, board), // Ферзь = Тура + Слон
            PieceType.King => ValidateKing(dRow, dCol),
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
    private static bool ValidateKing(int dRow, int dCol)
    {
        // Король ходить на 1 клітинку в будь-який бік
        return Math.Abs(dRow) <= 1 && Math.Abs(dCol) <= 1;
    }

    // ♟️ ПІШАК (Трохи складніше)
    private static bool ValidatePawn(SquareModel from, SquareModel to, int dRow, int dCol, ObservableCollection<SquareModel> board)
    {
        var direction = from.Piece.Color == PieceColor.White ? -1 : 1; // Білі йдуть вгору (-), Чорні вниз (+)
        
        // 1. Звичайний хід вперед
        if (dCol == 0 && dRow == direction)
        {
            return to.Piece.Type == PieceType.None; // Тільки якщо пусто
        }

        // 2. Подвійний хід зі старту
        bool isStartRow = (from.Piece.Color == PieceColor.White && from.Row == 6) || 
                          (from.Piece.Color == PieceColor.Black && from.Row == 1);
        
        if (isStartRow && dCol == 0 && dRow == 2 * direction)
        {
            // Перевіряємо, чи немає нікого попереду (проміжна клітинка)
            var intermediateSquare = GetSquare(board, from.Row + direction, from.Column);
            return to.Piece.Type == PieceType.None && intermediateSquare.Piece.Type == PieceType.None;
        }

        // 3. Взяття (по діагоналі)
        if (Math.Abs(dCol) == 1 && dRow == direction)
        {
            return to.Piece.Type != PieceType.None && to.Piece.Color != from.Piece.Color;
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

    public static bool IsMoveValid(SquareModel from, SquareModel to, ObservableCollection<SquareModel> board)
    {
        // 1. Спочатку перевіряємо базову геометрію (чи ходить так кінь/тура тощо)
        if (!IsBasicMoveValid(from, to, board)) return false;

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
}