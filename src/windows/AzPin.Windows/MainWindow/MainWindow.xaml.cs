using AzPin.Windows.MainWindow.Pages;
using AzPin.Windows.TrayIcon;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;

namespace AzPin.Windows.MainWindow;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Title = "AzPin";
        ExtendsContentIntoTitleBar = true;
        SystemBackdrop = new MicaBackdrop();
        AppWindow.Resize(new SizeInt32(960, 640));
        AppWindow.IsShownInSwitchers = false;

        // Close button hides instead of exits so the tray persists
        AppWindow.Closing += (_, args) =>
        {
            args.Cancel = true;
            AppWindow.IsShownInSwitchers = false;
            AppWindow.Hide();
        };
    }

    // Called after DI is configured so BrowsePage can resolve its ViewModel
    public void InitializeContent()
    {
        ContentFrame.Navigate(typeof(BrowsePage));
    }

    public void InitializeTrayIcon(TrayMenuViewModel vm)
    {
        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "tray.ico");
        TrayIcon.IconSource = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri(iconPath));
        TrayIcon.PopupActivation = Hardcodet.Wpf.TaskbarNotification.PopupActivationMode.LeftOrRightClick;
        TrayIcon.TrayPopup = new TrayMenuView { DataContext = vm };
    }

    private void OnNavSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
            return; // Settings page stub — add in 3.x

        if (args.SelectedItem is NavigationViewItem { Tag: "Browse" })
            ContentFrame.Navigate(typeof(BrowsePage));
    }
}
