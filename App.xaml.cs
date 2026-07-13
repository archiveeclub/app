using System.IO;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UsbMediaManager.Data;
using UsbMediaManager.Services;
using UsbMediaManager.ViewModels;
using UsbMediaManager.Views;

namespace UsbMediaManager;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var sc = new ServiceCollection();
        sc.AddSingleton<IConfiguration>(config);

        var dbFile = config["Database:FileName"] ?? "usbmedia.db";
        var dbPath = Path.Combine(AppContext.BaseDirectory, dbFile);
        sc.AddDbContextFactory<AppDbContext>(o => o.UseSqlite($"Data Source={dbPath}"));

        sc.AddSingleton<UsbDetectionService>();
        sc.AddSingleton<DriveIdentificationService>();
        sc.AddSingleton<OwnerFileService>();
        sc.AddSingleton<TmdbService>();
        sc.AddSingleton<CustomerService>();
        sc.AddSingleton<MediaService>();

        sc.AddSingleton<MainViewModel>();
        sc.AddSingleton<MainWindow>();

        Services = sc.BuildServiceProvider();

        // اطمینان از ساخت دیتابیس
        using (var scope = Services.CreateScope())
        {
            var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
            using var db = factory.CreateDbContext();
            db.Database.EnsureCreated();
        }

        var window = Services.GetRequiredService<MainWindow>();
        window.Show();
    }
}