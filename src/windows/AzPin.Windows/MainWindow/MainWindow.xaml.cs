using AzPin.Windows.TrayIcon;
using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Graphics;

namespace AzPin.Windows.MainWindow;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Title = "AzPin";
        ExtendsContentIntoTitleBar = true;
        AppWindow.Resize(new SizeInt32(900, 600));
        AppWindow.IsShownInSwitchers = false;

        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "tray.ico");
        TrayIcon.IconSource = new BitmapImage(new Uri(iconPath));
        TrayIcon.PopupActivation = PopupActivationMode.LeftOrRightClick;
        TrayIcon.TrayPopup = new TrayMenuView
        {
            DataContext = App.Services.GetRequiredService<TrayMenuViewModel>()
        };
    }

    public void ShowMainWindow()
    {
        AppWindow.IsShownInSwitchers = true;
        Activate();
    }

    public void HideMainWindow()
    {
        AppWindow.IsShownInSwitchers = false;
    }
}
