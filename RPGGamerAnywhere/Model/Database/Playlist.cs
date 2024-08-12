using SQLite;

namespace RPGGamerAnywhere.Model.Database;

public class Playlist
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [NotNull]
    public string? Name { get; set; }
}