using Microsoft.EntityFrameworkCore;
using UsbMediaManager.Data;
using UsbMediaManager.Models;

namespace UsbMediaManager.Services;

public record DriveResolution(Drive Drive, Customer Customer, bool IsNew);

public class CustomerService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly OwnerFileService _ownerFile;

    public CustomerService(IDbContextFactory<AppDbContext> factory, OwnerFileService ownerFile)
    {
        _factory = factory;
        _ownerFile = ownerFile;
    }

    /// <summary>
    /// فلش رو با سریال سخت‌افزاری / VidPid / فایل owner پیدا میکنه.
    /// اگر نبود null برمیگردونه (یعنی باید دیالوگ ثبت باز بشه).
    /// </summary>
    public async Task<DriveResolution?> ResolveAsync(DriveHardwareInfo hw, OwnerInfo? owner)
    {
        await using var db = await _factory.CreateDbContextAsync();

        Drive? drive = null;

        // 1) از روی فایل owner (PublicId)
        if (owner != null)
            drive = await db.Drives.Include(d => d.Customer)
                .FirstOrDefaultAsync(d => d.PublicId == owner.PublicId);

        // 2) از روی سریال سخت‌افزاری
        if (drive == null && !string.IsNullOrEmpty(hw.HardwareSerial))
            drive = await db.Drives.Include(d => d.Customer)
                .FirstOrDefaultAsync(d =>
                    d.HardwareSerial == hw.HardwareSerial &&
                    (hw.VidPid == null || d.VidPid == hw.VidPid));

        if (drive == null) return null; // ناشناس — باید ثبت بشه

        // آپدیت LastSeen
        drive.LastSeen = DateTime.UtcNow;
        await db.SaveChangesAsync();

        // اگر فایل owner نداشت (فرمت شده) دوباره بنویس
        if (owner == null)
            _ownerFile.Write(hw.DriveLetter,
                new OwnerInfo(drive.PublicId, drive.Customer.Name, DateTime.UtcNow));

        return new DriveResolution(drive, drive.Customer, false);
    }

    /// <summary>ثبت مشتری جدید (یا اتصال به مشتری موجود) و ثبت فلش.</summary>
    public async Task<DriveResolution> RegisterAsync(
        DriveHardwareInfo hw, string customerName, int? existingCustomerId = null)
    {
        await using var db = await _factory.CreateDbContextAsync();

        Customer customer;
        if (existingCustomerId is int id)
            customer = await db.Customers.FirstAsync(c => c.Id == id);
        else
        {
            customer = new Customer { Name = customerName.Trim() };
            db.Customers.Add(customer);
            await db.SaveChangesAsync();
        }

        var drive = new Drive
        {
            HardwareSerial = hw.HardwareSerial,
            VidPid = hw.VidPid,
            Label = hw.Label,
            CapacityBytes = hw.CapacityBytes,
            CustomerId = customer.Id
        };
        db.Drives.Add(drive);
        await db.SaveChangesAsync();

        _ownerFile.Write(hw.DriveLetter,
            new OwnerInfo(drive.PublicId, customer.Name, DateTime.UtcNow));

        return new DriveResolution(drive, customer, true);
    }

    /// <summary>سرچ مشتری براساس اسم — همه‌ی فلش‌هاش رو هم میاره.</summary>
    public async Task<List<Customer>> SearchCustomersAsync(string name)
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.Customers
            .Include(c => c.Drives)
            .Where(c => EF.Functions.Like(c.Name, $"%{name}%"))
            .ToListAsync();
    }

    public async Task<List<Customer>> GetAllCustomersAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.Customers.OrderBy(c => c.Name).ToListAsync();
    }
}