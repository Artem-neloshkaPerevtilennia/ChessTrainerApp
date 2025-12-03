using CommunityToolkit.Mvvm.ComponentModel;

namespace ChessTrainerApp.Models;

public partial class PgnMoveModel : ObservableObject
{
    public string Text { get; set; } // Наприклад "1. e4" або "e5"
    
    [ObservableProperty]
    private bool isSelected; // Чи це поточний хід (для білого прямокутника)
}