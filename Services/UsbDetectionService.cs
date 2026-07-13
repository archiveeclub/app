using System.Management;

namespace UsbMediaManager.Services;

public class UsbDriveEventArgs : EventArgs
{
    public string DriveLetter { get; init; } = string.Empty; // e.g. "E:\\"
}

public class UsbDetectionService : IDisposable
{
    private ManagementEventWatcher? _insertWatcher;
    private ManagementEventWatcher? _removeWatcher;

    public event EventHandler<UsbDriveEventArgs>? DriveInserted;
    public event EventHandler<UsbDriveEventArgs>? DriveRemoved;

    public void Start()
    {
        // EventType 2 = arrival, 3 = removal (Win32_VolumeChangeEvent)
        var insertQuery = new WqlEventQuery(
            "SELECT * FROM Win32_VolumeChangeEvent WHERE EventType = 2");
        _insertWatcher = new ManagementEventWatcher(insertQuery);
        _insertWatcher.EventArrived += (s, e) => Raise(e, DriveInserted);
        _insertWatcher.Start();

        var removeQuery = new WqlEventQuery(
            "SELECT * FROM Win32_VolumeChangeEvent WHERE EventType = 3");
        _removeWatcher = new ManagementEventWatcher(removeQuery);
        _removeWatcher.EventArrived += (s, e) => Raise(e, DriveRemoved);
        _removeWatcher.Start();
    }

    private void Raise(EventArrivedEventArgs e, EventHandler<UsbDriveEventArgs>? handler)
    {
        var letter = e.NewEvent.Properties["DriveName"]?.Value?.ToString() ?? string.Empty;
        if (!string.IsNullOrEmpty(letter))
            handler?.Invoke(this, new UsbDriveEventArgs { DriveLetter = letter + "\\" });
    }

    public void Dispose()
    {
        _insertWatcher?.Stop();
        _insertWatcher?.Dispose();
        _removeWatcher?.Stop();
        _removeWatcher?.Dispose();
    }
}