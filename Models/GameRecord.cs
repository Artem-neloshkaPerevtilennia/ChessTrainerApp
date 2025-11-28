using SQLite;

namespace ChessTrainerApp.Models;

public class GameRecord
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public DateTime DatePlayed { get; set; }
    
    public string WhitePlayer { get; set; }
    public string BlackPlayer { get; set; }
    
    public string Winner { get; set; } // "White", "Black", "Draw"
    
    public string PGN { get; set; } // Усі ходи одним рядком: "1. e4 e5 2. Nf3..."
}