using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using ChessTrainerApp.Models;
using ChessTrainerApp.Services;
using CommunityToolkit.Mvvm.Input;

namespace ChessTrainerApp.ViewModels;

public partial class ArchiveViewModel : ObservableObject
{
    public ObservableCollection<GameRecord> Games { get; } = new();

    public async Task LoadGames()
    {
        var gamesList = await DatabaseService.GetAllGamesAsync();

        Games.Clear();
        foreach (var game in gamesList)
        {
            Games.Add(game);
        }
    }

    [RelayCommand]
    private static async Task CopyPgn(string pgn)
    {
        if (string.IsNullOrWhiteSpace(pgn)) return;

        await Clipboard.Default.SetTextAsync(pgn);

        await Shell.Current.DisplayAlert("Успіх", "PGN скопійовано в буфер обміну!", "ОК");
    }
}