using System.Collections.Generic;

namespace UsbMediaManager.Models;

public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<Drive> Drives { get; set; } = new();
    public List<SeriesProgress> Progress { get; set; } = new();
}