using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using ChessTrainerApp.Models;
using ChessTrainerApp.Services;
using CommunityToolkit.Mvvm.Input;

namespace ChessTrainerApp.ViewModels;

public partial class ArchiveViewModel : ObservableObject
{
    // Колекція, яка автоматично оновлює список у XAML
    public ObservableCollection<GameRecord> Games { get; } = new();

    public async Task LoadGames()
    {
        // 1. Отримуємо дані з бази
        var gamesList = await DatabaseService.GetAllGamesAsync();

        // 2. Чистимо старий список і додаємо нові дані
        Games.Clear();
        foreach (var game in gamesList)
        {
            Games.Add(game);
        }
    }

    [RelayCommand]
    private async Task CopyPgn(string pgn)
    {
        if (string.IsNullOrWhiteSpace(pgn)) return;

        // Копіюємо в буфер обміну
        await Clipboard.Default.SetTextAsync(pgn);

        // Показуємо маленьке повідомлення
        await Shell.Current.DisplayAlert("Успіх", "PGN скопійовано в буфер обміну!", "ОК");
    }
}