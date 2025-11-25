namespace ChessTrainerApp.Models
{
    public enum PieceType { None, Pawn, Knight, Bishop, Rook, Queen, King }
    public enum PieceColor { None, White, Black }

    public class PieceModel
    {
        public PieceType Type { get; set; }
        public PieceColor Color { get; set; }

        // Допоміжна властивість для відображення (наприклад, "WK" для Білого Короля)
        public string Symbol => $"{Color.ToString()[0]}{Type.ToString()[0]}"; 
        
        // Відображення фігур
        public string DisplayValue
        {
            get
            {
                if (Type == PieceType.None) return "";

                // Базові коди Unicode для білих фігур
                // ♔♕♖♗♘♙ (White)
                // ♚♛♜♝♞♟ (Black)
                
                // Хитрість: Коди чорних фігур зміщені відносно білих на 6 позицій
                // Білий Король = 2654 (hex), Чорний Король = 265A
                
                int baseCode = 0x2654; 
                
                // Зміщення для типу фігури
                int offset = Type switch
                {
                    PieceType.King => 0,
                    PieceType.Queen => 1,
                    PieceType.Rook => 2,
                    PieceType.Bishop => 3,
                    PieceType.Knight => 4,
                    PieceType.Pawn => 5,
                    _ => 0
                };

                // Якщо чорні, додаємо ще 6 до коду
                if (Color == PieceColor.Black)
                {
                    offset += 6;
                }

                return char.ConvertFromUtf32(baseCode + offset);
            }
        }
    }
}