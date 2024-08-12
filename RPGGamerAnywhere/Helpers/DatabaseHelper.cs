using SQLite;

namespace RPGGamerAnywhere.Helpers;

public static class DatabaseHelper
{
    public enum Target
    {
        Database,
        Preferences,
    }

    private static readonly string _localAppData = Environment.GetFolderPath(
                              Environment.SpecialFolder.LocalApplicationData);

    private static readonly string _userFilePath = Path.Combine(_localAppData, "LibertasInfinitum");

    private static readonly string _dbFile = Path.Combine(_userFilePath, "mvvmDb.db");
    private static readonly string _preferences = Path.Combine(_userFilePath, "preferences.db");
    private const string _importUrl = "https://github.com/DerekGooding/RPGGamers-Radio-Premium/raw/main/mvvmDb.db?raw=true";

    public static void InitializeFolder()
    {
        if (!Directory.Exists(_userFilePath))
            Directory.CreateDirectory(_userFilePath);
    }

    public static bool Insert<T>(T item, Target target)
    {
        using SQLiteConnection connection = new(target == Target.Preferences ? _preferences : _dbFile);
        connection.CreateTable<T>();
        int rows = connection.Insert(item);
        return rows > 0;
    }

    public static bool Update<T>(T item, Target target)
    {
        using SQLiteConnection connection = new(target == Target.Preferences ? _preferences : _dbFile);
        connection.CreateTable<T>();
        int rows = connection.Update(item);
        return rows > 0;
    }

    public static bool Delete<T>(T item, Target target)
    {
        using SQLiteConnection connection = new(target == Target.Preferences ? _preferences : _dbFile);
        connection.CreateTable<T>();
        int rows = connection.Delete(item);
        return rows > 0;
    }

    public static List<T> Read<T>(Target target) where T : new()
    {
        using SQLiteConnection connection = new(target == Target.Preferences ? _preferences : _dbFile);
        connection.CreateTable<T>();
        return [.. connection.Table<T>()];
    }

    public static async Task ImportFromOnlineAsync()
    {
        using var client = new HttpClient();
        {
            using var response = client.GetStreamAsync(_importUrl);
            {
                await using var stream = new FileStream(_dbFile, FileMode.Create);
                {
                    try
                    {
                        await response.Result.CopyToAsync(stream);
                    }
                    catch { }
                }
            }
        }
    }

    public static async Task Refresh()
    {
        File.Delete(_dbFile);
        await ImportFromOnlineAsync();
    }
}