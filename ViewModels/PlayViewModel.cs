using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ChessTrainerApp.Models;

namespace ChessTrainerApp.ViewModels;

public partial class PlayViewModel : ObservableObject
{
    // Обраний колір: "White", "Black" або "Random"
    [ObservableProperty]
    private string selectedColorSide = "White"; 

    // Обранa складність (індекс у пікері: 0=Easy, 1=Medium, 2=Hard)
    [ObservableProperty]
    private int selectedDifficultyIndex = 1; // За замовчуванням Середня

    [RelayCommand]
    private void SelectSide(string side)
    {
        SelectedColorSide = side;
    }

    [RelayCommand]
    private async Task StartGame()
    {
        // 1. Визначаємо глибину ШІ
        int depth = SelectedDifficultyIndex switch
        {
            0 => 1, // Легка
            1 => 2, // Середня
            2 => 3, // Важка
            _ => 2
        };

        // 2. Визначаємо колір
        PieceColor playerColor;
        if (SelectedColorSide == "Random")
        {
            playerColor = new Random().Next(0, 2) == 0 ? PieceColor.White : PieceColor.Black;
        }
        else
        {
            playerColor = SelectedColorSide == "White" ? PieceColor.White : PieceColor.Black;
        }

        // 3. Створюємо сторінку (викликається стандартний конструктор)
        var gamePage = new ChessBoardPage();

        // 4. Дістаємо з неї ViewModel і налаштовуємо гру ВРУЧНУ
        if (gamePage.BindingContext is ChessBoardViewModel vm)
        {
            vm.SetupGame(playerColor, depth);
        }

        // 5. Переходимо на сторінку
        await Shell.Current.Navigation.PushAsync(gamePage);
    }
}