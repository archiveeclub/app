using System.Collections.Generic;

namespace UsbMediaManager.Models;

public enum MediaType { Movie, Series }

public class MediaItem
{
    public int Id { get; set; }
    public int TmdbId { get; set; }
    public MediaType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? OriginalTitle { get; set; }
    public int? Year { get; set; }
    public string? PosterUrl { get; set; }
    public string? Overview { get; set; }

    /// <summary>فقط برای سریال: تعداد کل قسمت‌های پخش‌شده (از TMDB).</summary>
    public int? TotalEpisodes { get; set; }
    public int? TotalSeasons { get; set; }
    public DateTime LastTmdbSync { get; set; } = DateTime.UtcNow;

    public List<DriveMedia> Drives { get; set; } = new();
}