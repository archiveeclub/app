using System.Windows;
using UsbMediaManager.ViewModels;

namespace UsbMediaManager.Views;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}