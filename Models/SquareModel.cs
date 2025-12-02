using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Maui.Graphics;

namespace ChessTrainerApp.Models;

// 1. Клас має бути partial
// 2. Має успадковувати ObservableObject
public partial class SquareModel : ObservableObject 
{
    public int Row { get; set; }
    public int Column { get; set; }

    [ObservableProperty]
    private PieceModel piece;

    [ObservableProperty]
    private Color squareColor;

    [ObservableProperty]
    private double textOpacity = 1;

    // 👇 НАЙВАЖЛИВІШЕ: ЦІ ПОЛЯ МАЮТЬ БУТИ САМЕ ТАКИМИ 👇
    
    [ObservableProperty] 
    private bool isRegularMoveHint; // Генерує IsRegularMoveHint

    [ObservableProperty]
    private bool isCaptureHint;     // Генерує IsCaptureHint

    // Властивість для Binding у XAML (для дебагу можна)
    public string PositionName => $"{(char)('a' + Column)}{8 - Row}"; 
}