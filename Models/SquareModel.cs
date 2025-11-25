using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Maui.Graphics;

namespace ChessTrainerApp.Models
{
    public partial class SquareModel : ObservableObject
    {
        public int Row { get; set; }
        public int Column { get; set; }

        [ObservableProperty]
        private PieceModel piece;
        [ObservableProperty]
        private Color squareColor; // Для кольору дошки (Black/White)

        // Властивість для Binding у XAML
        public string PositionName => $"{(char)('a' + Column)}{8 - Row}"; 
    }
}