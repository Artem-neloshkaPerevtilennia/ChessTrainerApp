using System.Globalization;

namespace ChessTrainerApp.Converters;

public class SelectionToBorderColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {        
        string? selected = value?.ToString();
        string? target = parameter?.ToString();

        if (selected == target)
        {
            return Colors.LightGreen;
        }
        return Colors.Transparent;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}