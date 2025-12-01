using System.Globalization;

namespace ChessTrainerApp.Converters;

public class BlindfoldOpacityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isBlindfold && isBlindfold)
        {
            return 0; // Повністю прозорий (невидимий)
        }
        return 1; // Видимий
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}