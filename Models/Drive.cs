using System.Collections.Generic;

namespace UsbMediaManager.Models;

public class Drive
{
    public int Id { get; set; }

    /// <summary>شناسه یکتای سخت‌افزاری (iSerialNumber). با فرمت پاک نمیشه.</summary>
    public string HardwareSerial { get; set; } = string.Empty;

    /// <summary>ترکیب VID+PID برای فلش‌های بدون سریال یکتا.</summary>
    public string? VidPid { get; set; }

    /// <summary>شناسه یکتای داخلی که تو فایل .owner هم ذخیره میشه.</summary>
    public Guid PublicId { get; set; } = Guid.NewGuid();

    public string? Label { get; set; }
    public long? CapacityBytes { get; set; }
    public DateTime FirstSeen { get; set; } = DateTime.UtcNow;
    public DateTime LastSeen { get; set; } = DateTime.UtcNow;

    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public List<DriveMedia> Media { get; set; } = new();
}