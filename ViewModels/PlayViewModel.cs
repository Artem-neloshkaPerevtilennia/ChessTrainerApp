using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ChessTrainerApp.Models;

namespace ChessTrainerApp.ViewModels;

public partial class PlayViewModel : ObservableObject
{
    [ObservableProperty]
    private string selectedColorSide = "White";

    [ObservableProperty]
    private int selectedDifficultyIndex = 1;

    [ObservableProperty]
    private int _selectedGameModeIndex = 0;

    [ObservableProperty]
    private bool isBlindfoldEnabled;

    [RelayCommand]
    private void SelectSide(string side)
    {
        SelectedColorSide = side;
    }

    [RelayCommand]
    [Obsolete]
    private async Task StartGame()
    {
        int depth = SelectedDifficultyIndex switch
        {
            0 => 2,
            1 => 3,
            2 => 4,
            _ => 3
        };

        PieceColor playerColor = SelectedColorSide switch
        {
            "Random" => new Random().Next(0, 2) == 0 ? PieceColor.White : PieceColor.Black,
            "Black" => PieceColor.Black,
            _ => PieceColor.White
        };

        GameMode mode = GameMode.Training;
        if (SelectedGameModeIndex == 1) mode = GameMode.Challenge;

        var gamePage = new ChessBoardPage();
        if (gamePage.BindingContext is ChessBoardViewModel vm)
        {
            vm.SetupGame(playerColor, depth, mode, IsBlindfoldEnabled);
        }

        await Shell.Current.Navigation.PushAsync(gamePage);
    }
}