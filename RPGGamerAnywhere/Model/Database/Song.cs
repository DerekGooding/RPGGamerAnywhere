﻿using SQLite;

namespace RPGGamerAnywhere.Model.Database;

public class Song
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public int UrlId { get; set; }
    public string? Url { get; set; }
    public string? Game { get; set; }
    public string? Title { get; set; }
}