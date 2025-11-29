using System.Globalization;

namespace ChessTrainerApp.Converters;

public class SelectionToBorderColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // value - це поточний SelectedColorSide (наприклад "White")
        // parameter - це те, що ми передаємо в XAML (наприклад "White")
        
        string selected = value?.ToString();
        string target = parameter?.ToString();

        if (selected == target)
        {
            return Colors.LightGreen; // Колір виділення
        }
        return Colors.Transparent; // Невибраний
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}