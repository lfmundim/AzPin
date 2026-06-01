using H.NotifyIcon;
using H.NotifyIcon.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AzPin.Windows.TrayIcon;

public static class TrayIconFactory
{
    public static TaskbarIcon Create()
    {
        var menu = new MenuFlyout();

        var quitItem = new MenuFlyoutItem
        {
            Text = "Quit AzPin"
        };
        quitItem.Click += (_, _) => Application.Current.Exit();
        menu.Items.Add(quitItem);

        return new TaskbarIcon
        {
            ToolTipText = "AzPin",
            IconSource = new IconFile("Assets/tray.ico"),
            ContextFlyout = menu
        };
    }
}
