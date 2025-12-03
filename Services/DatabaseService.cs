using SQLite;
using ChessTrainerApp.Models;

namespace ChessTrainerApp.Services;

public static class DatabaseService
{
    private static SQLiteAsyncConnection? _db;

    private static async Task Init()
    {
        if (_db != null) return;

        var databasePath = Path.Combine(FileSystem.AppDataDirectory, "ChessGames.db");
        
        _db = new SQLiteAsyncConnection(databasePath);
        
        await _db.CreateTableAsync<GameRecord>();
    }

    public static async Task AddGameAsync(GameRecord game)
    {
        await Init();
        _ = await _db.InsertAsync(game);
    }

    public static async Task<List<GameRecord>> GetAllGamesAsync()
    {
        await Init();
        return await _db.Table<GameRecord>().OrderByDescending(g => g.DatePlayed).ToListAsync();
    }
}