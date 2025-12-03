namespace ChessTrainerApp.Models
{
    public enum PieceType { None, Pawn, Knight, Bishop, Rook, Queen, King }
    public enum PieceColor { None, White, Black }

    public class PieceModel
    {
        public PieceType Type { get; set; }
        public PieceColor Color { get; set; }
        public bool HasMoved { get; set; } = false;

        public string Symbol => $"{Color.ToString()[0]}{Type.ToString()[0]}"; 
        
        public string DisplayValue
        {
            get
            {
                if (Type == PieceType.None) return "";

                int baseCode = 0x2654; 
                
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

                if (Color == PieceColor.Black)
                {
                    offset += 6;
                }

                return char.ConvertFromUtf32(baseCode + offset);
            }
        }
    }
}