using Microsoft.UI.Xaml;
using Windows.Graphics;

namespace AzPin.Windows.TrayIcon;

public sealed partial class TrayHostWindow : Window
{
    public TrayHostWindow()
    {
        InitializeComponent();
        // Permanent 1×1 host for the tray icon — never hidden so H.NotifyIcon XamlRoot stays valid.
        AppWindow.IsShownInSwitchers = false;
        AppWindow.Resize(new SizeInt32(1, 1));
        // Prevent accidental close from killing the tray.
        AppWindow.Closing += (_, args) => args.Cancel = true;
    }

    public void InitializeTrayIcon(TrayMenuViewModel vm)
    {
        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "tray.ico");
        TrayIcon.Icon = new System.Drawing.Icon(iconPath);
        TrayIcon.TrayPopup = new TrayMenuView { DataContext = vm };
        TrayIcon.ForceCreate();
    }
}
