using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Media.Imaging;

namespace AzPin.Windows.TrayIcon;

public static class TrayIconFactory
{
    public static TaskbarIcon Create()
    {
        var menuView = new TrayMenuView
        {
            DataContext = App.Services.GetRequiredService<TrayMenuViewModel>()
        };

        return new TaskbarIcon
        {
            ToolTipText = "AzPin",
            IconSource = new BitmapImage(new Uri("ms-appx:///Assets/tray.ico")),
            PopupActivation = PopupActivationMode.LeftOrRightClick,
            TrayPopup = menuView
        };
    }
}
