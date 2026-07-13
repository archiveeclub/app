using System.Management;

namespace UsbMediaManager.Services;

public record DriveHardwareInfo(
    string DriveLetter,
    string HardwareSerial,
    string? VidPid,
    string? Label,
    long? CapacityBytes);

public class DriveIdentificationService
{
    /// <summary>
    /// از حرف درایو (مثل "E:\\") اطلاعات سخت‌افزاری فلش رو درمیاره.
    /// زنجیره: LogicalDisk → Partition → DiskDrive.
    /// </summary>
    public DriveHardwareInfo? GetHardwareInfo(string driveLetter)
    {
        var letter = driveLetter.TrimEnd('\\'); // "E:"

        // 1) LogicalDisk -> Partition
        var partition = QueryAssociator(
            $"Win32_LogicalDisk.DeviceID='{letter}'",
            "Win32_LogicalDiskToPartition");
        if (partition == null) return null;

        // 2) Partition -> DiskDrive
        var disk = QueryAssociator(
            $"Win32_DiskPartition.DeviceID='{partition["DeviceID"]}'",
            "Win32_DiskDriveToDiskPartition");
        if (disk == null) return null;

        var pnpId = disk["PNPDeviceID"]?.ToString() ?? string.Empty;
        var serial = ExtractSerial(disk["SerialNumber"]?.ToString(), pnpId);
        var (vid, pid) = ExtractVidPid(pnpId);

        long? capacity = null;
        if (disk["Size"] != null && long.TryParse(disk["Size"].ToString(), out var size))
            capacity = size;

        string? label = null;
        using (var ld = new ManagementObject($"Win32_LogicalDisk.DeviceID='{letter}'"))
        {
            ld.Get();
            label = ld["VolumeName"]?.ToString();
        }

        return new DriveHardwareInfo(
            driveLetter,
            serial,
            vid != null ? $"VID_{vid}&PID_{pid}" : null,
            label,
            capacity);
    }

    private static ManagementBaseObject? QueryAssociator(string path, string assocClass)
    {
        using var searcher = new ManagementObjectSearcher(
            $"ASSOCIATORS OF {{{path}}} WHERE AssocClass={assocClass}");
        foreach (ManagementBaseObject o in searcher.Get())
            return o;
        return null;
    }

    /// <summary>سریال سخت‌افزاری رو ترجیحا از SerialNumber، وگرنه از انتهای PNPDeviceID.</summary>
    private static string ExtractSerial(string? rawSerial, string pnpId)
    {
        if (!string.IsNullOrWhiteSpace(rawSerial))
            return rawSerial.Trim();

        // PNPDeviceID: USBSTOR\DISK&VEN_...&PROD_...\<serial>&0
        var parts = pnpId.Split('\\');
        if (parts.Length >= 3)
        {
            var last = parts[2];
            var amp = last.IndexOf('&');
            return amp > 0 ? last[..amp] : last;
        }
        return pnpId; // fallback
    }

    private static (string? vid, string? pid) ExtractVidPid(string pnpId)
    {
        string? vid = null, pid = null;
        var upper = pnpId.ToUpperInvariant();
        var vi = upper.IndexOf("VID_");
        if (vi >= 0 && upper.Length >= vi + 8) vid = upper.Substring(vi + 4, 4);
        var pi = upper.IndexOf("PID_");
        if (pi >= 0 && upper.Length >= pi + 8) pid = upper.Substring(pi + 4, 4);
        return (vid, pid);
    }
}