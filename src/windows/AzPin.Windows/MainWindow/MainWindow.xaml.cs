using AzPin.Windows.MainWindow.Pages;
using AzPin.Windows.TrayIcon;
using AzPin.Windows.ViewModels;
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
        AppWindow.IsShownInSwitchers = false;

        AppWindow.Closing += (_, args) =>
        {
            args.Cancel = true;
            AppWindow.IsShownInSwitchers = false;
            AppWindow.Hide();
        };
    }

    public void InitializeContent()
    {
        ContentFrame.Navigate(typeof(BrowsePage));
    }

    public void InitializeTrayIcon(TrayMenuViewModel vm)
    {
        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "tray.ico");
        TrayIcon.Icon = new System.Drawing.Icon(iconPath);
        TrayIcon.ToolTipText = "AzPin";

        TrayIcon.TrayLeftMouseClick += (_, _) => vm.OpenMainWindowCommand.Execute(null);
        TrayIcon.PreviewTrayContextMenuOpen += (_, _) => RebuildContextMenu(vm);

        _ = vm.OnMenuOpenedAsync();
    }

    private void RebuildContextMenu(TrayMenuViewModel vm)
    {
        TrayFlyout.Items.Clear();

        TrayFlyout.Items.Add(new MenuFlyoutItem
        {
            Text = vm.Auth.State switch
            {
                AuthState.SignedIn when !string.IsNullOrWhiteSpace(vm.Auth.AccountName) => vm.Auth.AccountName!,
                AuthState.SignedIn => "Signed in",
                AuthState.NotSignedIn => "Not signed in — run az login",
                AuthState.CliNotInstalled => "Azure CLI not installed",
                _ => "Refreshing..."
            },
            IsEnabled = false
        });
        TrayFlyout.Items.Add(new MenuFlyoutSeparator());

        if (vm.PinnedResources.Count > 0)
        {
            foreach (var r in vm.PinnedResources)
            {
                var item = new MenuFlyoutItem
                {
                    Text = r.Name,
                    Icon = new FontIcon
                    {
                        FontFamily = new FontFamily("Segoe Fluent Icons"),
                        Glyph = r.GlyphCode
                    }
                };
                var uri = r.PortalUri;
                item.Click += async (_, _) => await Windows.System.Launcher.LaunchUriAsync(uri);
                TrayFlyout.Items.Add(item);
            }
        }
        else
        {
            TrayFlyout.Items.Add(new MenuFlyoutItem { Text = "No pinned resources", IsEnabled = false });
        }

        TrayFlyout.Items.Add(new MenuFlyoutSeparator());

        var openItem = new MenuFlyoutItem { Text = "Open AzPin" };
        openItem.Click += (_, _) => vm.OpenMainWindowCommand.Execute(null);
        TrayFlyout.Items.Add(openItem);

        var quitItem = new MenuFlyoutItem { Text = "Quit AzPin" };
        quitItem.Click += (_, _) => vm.QuitCommand.Execute(null);
        TrayFlyout.Items.Add(quitItem);
    }

    private void OnNavSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
            return;

        if (args.SelectedItem is NavigationViewItem { Tag: "Browse" })
            ContentFrame.Navigate(typeof(BrowsePage));
    }
}
