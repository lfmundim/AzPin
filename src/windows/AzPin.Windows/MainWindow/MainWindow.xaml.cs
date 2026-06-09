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

        // Close button hides instead of exits so the tray persists.
        // Guard: AppWindow.Hide() requires the window to have been shown at least once.
        AppWindow.Closing += (_, args) =>
        {
            args.Cancel = true;
            AppWindow.IsShownInSwitchers = false;
            AppWindow.Resize(new Windows.Graphics.SizeInt32(1, 1));
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
        // Use System.Drawing.Icon directly — more reliable than BitmapImage for .ico
        // files and avoids any async conversion race before the window is shown.
        TrayIcon.Icon = new System.Drawing.Icon(iconPath);
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
