using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Maui.Graphics;

namespace ChessTrainerApp.Models;

public partial class SquareModel : ObservableObject 
{
    public int Row { get; set; }
    public int Column { get; set; }

    [ObservableProperty]
    private PieceModel? piece;

    [ObservableProperty]
    private Color? squareColor;

    [ObservableProperty]
    private double textOpacity = 1;

    
    [ObservableProperty] 
    private bool isRegularMoveHint;

    [ObservableProperty]
    private bool isCaptureHint;

    public string PositionName => $"{(char)('a' + Column)}{8 - Row}"; 
}