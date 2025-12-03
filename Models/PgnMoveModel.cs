using CommunityToolkit.Mvvm.ComponentModel;

namespace ChessTrainerApp.Models;

public partial class PgnMoveModel : ObservableObject
{
    public string? Text { get; set; }
    
    [ObservableProperty]
    private bool isSelected;
}
