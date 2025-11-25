using System.Globalization;
using Microsoft.Maui.Graphics;
using ChessTrainerApp.Models;

namespace ChessTrainerApp.Converters
{
    public class PieceColorToMauiColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Models.PieceColor pieceColor)
            {
                return pieceColor switch
                {
                    // Білі фігури (майже білі, трохи теплі)
                    Models.PieceColor.White => Color.FromHex("#FFFFFF"), 
                    
                    // Чорні фігури (справжній чорний)
                    Models.PieceColor.Black => Color.FromHex("#000000"), 
                    
                    _ => Colors.Transparent
                };
            }
            return Colors.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
