using H.NotifyIcon;
using H.NotifyIcon.Core;
using Microsoft.Extensions.DependencyInjection;

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
            IconSource = new IconFile("Assets/tray.ico"),
            PopupActivation = PopupActivationMode.LeftOrRightClick,
            TrayPopup = menuView
        };
    }
}
