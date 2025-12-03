using System.Globalization;
using Microsoft.Maui.Graphics;
using ChessTrainerApp.Models;

namespace ChessTrainerApp.Converters
{
    public class PieceColorToMauiColorConverter : IValueConverter
    {
        [Obsolete]
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Models.PieceColor pieceColor)
            {
                return pieceColor switch
                {
                    Models.PieceColor.White => Color.FromHex("#FFFFFF"), 
                    
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
