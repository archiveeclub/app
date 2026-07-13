namespace UsbMediaManager.Models;

/// <summary>رابطه many-to-many: کدوم فیلم/سریال روی کدوم فلش ریخته شده.</summary>
public class DriveMedia
{
    public int Id { get; set; }
    public int DriveId { get; set; }
    public Drive Drive { get; set; } = null!;
    public int MediaItemId { get; set; }
    public MediaItem MediaItem { get; set; } = null!;
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}