using SQLite;
using App_v3.Models;

namespace App_v3.Services;

public class DatabaseService : IDatabaseService
{
    private readonly string _dbPath;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private SQLiteAsyncConnection? _db;

    public DatabaseService(string dbPath) => _dbPath = dbPath;

    private async Task<SQLiteAsyncConnection> InitAsync()
    {
        if (_db is not null) return _db;

        await _initLock.WaitAsync();
        try
        {
            if (_db is not null) return _db;

            var db = new SQLiteAsyncConnection(
                _dbPath,
                SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);

            await db.CreateTablesAsync<Persona, Clase>();
            _db = db;
            return _db;
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task<List<T>> GetAllAsync<T>() where T : class, new()
        => await (await InitAsync()).Table<T>().ToListAsync();

    public async Task<int> SaveAsync<T>(T item) where T : class, IEntidad, new()
    {
        var db = await InitAsync();
        return item.Id != 0 ? await db.UpdateAsync(item) : await db.InsertAsync(item);
    }

    public async Task<int> DeleteAsync<T>(T item) where T : class, new()
        => await (await InitAsync()).DeleteAsync(item);

    public async Task ResetAsync()
    {
        await _initLock.WaitAsync();
        try
        {
            if (_db is not null)
            {
                await _db.CloseAsync();
                _db = null;
            }

            SQLiteAsyncConnection.ResetPool();

            if (File.Exists(_dbPath))
                File.Delete(_dbPath);
        }
        finally
        {
            _initLock.Release();
        }

        await InitAsync();
    }
}