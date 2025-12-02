using System.Globalization;
using ChessTrainerApp.Models;

namespace ChessTrainerApp.Converters;

public class WinnerConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // Ми очікуємо, що сюди прилетить цілий об'єкт GameRecord
        if (value is GameRecord game)
        {
            if (game.Winner == "Draw") return "🤝 Нічия";

            // Логіка: Перевіряємо, чи переміг той колір, за який грав Юзер
            bool userWon = false;

            if (game.Winner == "White" && game.WhitePlayer == "User") userWon = true;
            if (game.Winner == "Black" && game.BlackPlayer == "User") userWon = true;

            // Якщо параметр "Color" - повертаємо колір тексту
            if (parameter as string == "Color")
            {
                return userWon ? Colors.LightGreen : Colors.IndianRed;
            }

            // Інакше повертаємо текст
            return userWon ? "🏆 Перемога (Ви)" : "💀 Поразка (Бот)";
        }

        return "Невідомо";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}