using CommunityToolkit.Mvvm.ComponentModel;

namespace ChessTrainerApp.Models;

public partial class PgnMoveModel : ObservableObject
{
    public string MoveText { get; set; } // Наприклад "e4"
    public int MoveIndex { get; set; }   // Порядковий номер у списку

    // Ця властивість відповідає за білий прямокутничок
    [ObservableProperty]
    private bool isSelected; 
    
    // Номер ходу для відображення (наприклад "1." або просто порожньо для чорних)
    public string MoveNumberText { get; set; } 
}