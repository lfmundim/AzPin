using AzPin.Windows.MainWindow.Pages;
using AzPin.Windows.TrayIcon;
using AzPin.Windows.Utilities;
using AzPin.Windows.ViewModels;
using CommunityToolkit.Mvvm.Input;
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
        TrayIcon.LeftClickCommand = vm.OpenMainWindowCommand;

        // Set app window icon (taskbar / app switcher)
        AppWindow.SetIcon(iconPath);

        // PopupMenu mode (the default) reads ContextFlyout.Items fresh on every right-click,
        // so reactive rebuilds work. SecondWindow copies items once at setup and ignores updates.
        // PropertyChanged may fire on a background thread after async pin ops — always dispatch
        // to UI thread before touching MenuFlyout.Items.

        RebuildContextMenu(vm);
        vm.PropertyChanged += (_, _) => DispatcherQueue.TryEnqueue(() => RebuildContextMenu(vm));
        vm.Auth.PropertyChanged += (_, _) => DispatcherQueue.TryEnqueue(() => RebuildContextMenu(vm));

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

        bool hasAny = vm.PinnedResourceGroups.Count > 0 || vm.PinnedResources.Count > 0;

        if (vm.PinnedResourceGroups.Count > 0)
        {
            foreach (var rg in vm.PinnedResourceGroups)
            {
                var uri = PortalUrl.ForResourceGroup(rg.SubscriptionId, rg.Name);
                TrayFlyout.Items.Add(new MenuFlyoutItem
                {
                    Text = rg.Name,
                    Icon = new FontIcon { FontFamily = new FontFamily("Segoe Fluent Icons"), Glyph = "" },
                    Command = new RelayCommand(() => { _ = global::Windows.System.Launcher.LaunchUriAsync(uri); })
                });
            }
        }

        if (vm.PinnedResources.Count > 0)
        {
            if (vm.PinnedResourceGroups.Count > 0)
                TrayFlyout.Items.Add(new MenuFlyoutSeparator());

            foreach (var r in vm.PinnedResources)
            {
                var uri = r.PortalUri;
                TrayFlyout.Items.Add(new MenuFlyoutItem
                {
                    Text = r.Name,
                    Icon = new FontIcon { FontFamily = new FontFamily("Segoe Fluent Icons"), Glyph = r.GlyphCode },
                    Command = new RelayCommand(() => { _ = global::Windows.System.Launcher.LaunchUriAsync(uri); })
                });
            }
        }

        if (!hasAny)
            TrayFlyout.Items.Add(new MenuFlyoutItem { Text = "No pinned resources", IsEnabled = false });

        TrayFlyout.Items.Add(new MenuFlyoutSeparator());
        TrayFlyout.Items.Add(new MenuFlyoutItem { Text = "Open AzPin", Command = vm.OpenMainWindowCommand });
        TrayFlyout.Items.Add(new MenuFlyoutItem { Text = "Quit AzPin",  Command = vm.QuitCommand });
    }

    private void OnNavSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem { Tag: "Settings" })
            ContentFrame.Navigate(typeof(SettingsPage));
        else if (args.SelectedItem is NavigationViewItem { Tag: "Browse" })
            ContentFrame.Navigate(typeof(BrowsePage));
    }
}
