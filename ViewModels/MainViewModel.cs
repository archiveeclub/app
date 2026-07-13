using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UsbMediaManager.Models;
using UsbMediaManager.Services;
using UsbMediaManager.Views;

namespace UsbMediaManager.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly UsbDetectionService _usb;
    private readonly DriveIdentificationService _ident;
    private readonly OwnerFileService _ownerFile;
    private readonly CustomerService _customers;
    private readonly MediaService _media;
    private readonly TmdbService _tmdb;

    [ObservableProperty] private string _statusText = "در انتظار وصل شدن فلش...";
    [ObservableProperty] private Customer? _activeCustomer;
    [ObservableProperty] private Drive? _activeDrive;
    [ObservableProperty] private string _customerSearch = string.Empty;

    public ObservableCollection<MediaItem> ActiveDriveMedia { get; } = new();
    public ObservableCollection<Customer> SearchResults { get; } = new();

    public MainViewModel(
        UsbDetectionService usb, DriveIdentificationService ident,
        OwnerFileService ownerFile, CustomerService customers,
        MediaService media, TmdbService tmdb)
    {
        _usb = usb; _ident = ident; _ownerFile = ownerFile;
        _customers = customers; _media = media; _tmdb = tmdb;

        _usb.DriveInserted += OnDriveInserted;
        _usb.DriveRemoved += OnDriveRemoved;
        _usb.Start();
    }

    private async void OnDriveInserted(object? sender, UsbDriveEventArgs e)
    {
        await Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            StatusText = $"فلش {e.DriveLetter} وصل شد، در حال شناسایی...";
            var hw = _ident.GetHardwareInfo(e.DriveLetter);
            if (hw == null) { StatusText = "نتونستم فلش رو بخونم."; return; }

            var owner = _ownerFile.Read(e.DriveLetter);
            var resolution = await _customers.ResolveAsync(hw, owner);

            if (resolution == null)
            {
                // ناشناس — دیالوگ ثبت
                var dialog = new CustomerRegisterDialog(await _customers.GetAllCustomersAsync());
                if (dialog.ShowDialog() == true)
                {
                    resolution = await _customers.RegisterAsync(
                        hw, dialog.CustomerName, dialog.ExistingCustomerId);
                    StatusText = $"مشتری جدید ثبت شد: {resolution.Customer.Name}";
                }
                else { StatusText = "ثبت لغو شد."; return; }
            }
            else
            {
                StatusText = $"فلش مال {resolution.Customer.Name} هست ✅";
            }

            ActiveCustomer = resolution.Customer;
            ActiveDrive = resolution.Drive;
            await LoadDriveMediaAsync();

            // پاپ‌اپ لیست فیلم/سریال این مشتری
            var popup = new DrivePopup(resolution.Customer, ActiveDriveMedia.ToList());
            popup.Show();
        });
    }

    private void OnDriveRemoved(object? sender, UsbDriveEventArgs e)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            StatusText = $"فلش {e.DriveLetter} جدا شد.";
            ActiveCustomer = null; ActiveDrive = null;
            ActiveDriveMedia.Clear();
        });
    }

    private async Task LoadDriveMediaAsync()
    {
        ActiveDriveMedia.Clear();
        if (ActiveDrive == null) return;
        foreach (var m in await _media.GetDriveMediaAsync(ActiveDrive.Id))
            ActiveDriveMedia.Add(m);
    }

    [RelayCommand]
    private async Task SearchCustomers()
    {
        SearchResults.Clear();
        if (string.IsNullOrWhiteSpace(CustomerSearch)) return;
        foreach (var c in await _customers.SearchCustomersAsync(CustomerSearch))
            SearchResults.Add(c);
    }

    /// <summary>افزودن فیلم/سریال به فلش فعلی از طریق سرچ TMDB.</summary>
    [RelayCommand]
    private async Task AddMediaFromTmdb()
    {
        if (ActiveDrive == null) { StatusText = "اول یه فلش وصل کن."; return; }
        var dialog = new TmdbSearchDialog(_tmdb);
        if (dialog.ShowDialog() == true && dialog.SelectedResult != null)
        {
            var media = await _media.GetOrCreateMediaAsync(dialog.SelectedResult);
            await _media.AddMediaToDriveAsync(ActiveDrive.Id, media.Id);
            await LoadDriveMediaAsync();
            StatusText = $"اضافه شد: {media.Title}";
        }
    }

    /// <summary>باز کردن پنجره‌ی سرچ «کیا این سریال رو دارن».</summary>
    [RelayCommand]
    private void OpenSeriesSearch()
    {
        var win = new SeriesSearchWindow(_tmdb, _media);
        win.Show();
    }
}