using System.IO;
using System.Text.Json;

namespace UsbMediaManager.Services;

public record OwnerInfo(Guid PublicId, string CustomerName, DateTime WrittenAt);

public class OwnerFileService
{
    private const string FileName = ".owner";

    public OwnerInfo? Read(string driveLetter)
    {
        try
        {
            var path = Path.Combine(driveLetter, FileName);
            if (!File.Exists(path)) return null;
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<OwnerInfo>(json);
        }
        catch { return null; }
    }

    public void Write(string driveLetter, OwnerInfo info)
    {
        try
        {
            var path = Path.Combine(driveLetter, FileName);
            var json = JsonSerializer.Serialize(info,
                new JsonSerializerOptions { WriteIndented = true });

            // اگر فایل مخفی وجود داره، اول attribute رو بردار تا قابل نوشتن باشه
            if (File.Exists(path))
                File.SetAttributes(path, FileAttributes.Normal);

            File.WriteAllText(path, json);
            File.SetAttributes(path, FileAttributes.Hidden | FileAttributes.System);
        }
        catch { /* فلش رایت-اونلی یا پر باشه — بی‌خیال، سریال سخت‌افزاری لایه اصلیه */ }
    }
}