namespace UsbMediaManager.Models;

/// <summary>پیشرفت دیدن سریال برای هر مشتری.</summary>
public class SeriesProgress
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public int MediaItemId { get; set; }
    public MediaItem MediaItem { get; set; } = null!;

    public int SeasonWatched { get; set; }
    public int EpisodeWatched { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}