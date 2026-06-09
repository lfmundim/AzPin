using AzPin.Windows.MainWindow.Pages;
using AzPin.Windows.Models;
using AzPin.Windows.TrayIcon;
using AzPin.Windows.Utilities;
using AzPin.Windows.ViewModels;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Graphics;

namespace AzPin.Windows.MainWindow;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Title = "AzPin";
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

        bool hasAny = vm.PinnedRgItems.Count > 0 || vm.PinnedResources.Count > 0;

        foreach (var rg in vm.PinnedRgItems)
            TrayFlyout.Items.Add(BuildRgSubItem(rg));

        if (vm.PinnedResources.Count > 0)
        {
            if (vm.PinnedRgItems.Count > 0)
                TrayFlyout.Items.Add(new MenuFlyoutSeparator());

            foreach (var r in vm.PinnedResources)
            {
                var uri = r.PortalUri;
                TrayFlyout.Items.Add(new MenuFlyoutItem
                {
                    Text = r.Name,
                    Icon = SymbolIconFor(r.Type),
                    Command = new RelayCommand(() => { _ = global::Windows.System.Launcher.LaunchUriAsync(uri); })
                });
            }
        }

        if (!hasAny)
            TrayFlyout.Items.Add(new MenuFlyoutItem { Text = "No pinned resources", IsEnabled = false });

        TrayFlyout.Items.Add(new MenuFlyoutSeparator());
        TrayFlyout.Items.Add(new MenuFlyoutItem { Text = "Open AzPin", Command = vm.OpenMainWindowCommand });
        TrayFlyout.Items.Add(new MenuFlyoutItem { Text = "Quit AzPin", Command = vm.QuitCommand });
    }

    private static MenuFlyoutSubItem BuildRgSubItem(TrayRgViewModel rg)
    {
        var rgItem = new MenuFlyoutSubItem
        {
            Text = rg.Name,
            Icon = new SymbolIcon(Symbol.Folder)
        };

        foreach (var res in rg.Resources)
        {
            if (res.IsRunnable)
                rgItem.Items.Add(BuildRunnableResourceSubItem(res));
            else
            {
                var resUri = res.PortalUri;
                rgItem.Items.Add(new MenuFlyoutItem
                {
                    Text = res.Name,
                    Icon = SymbolIconFor(res.Resource.Type),
                    Command = new RelayCommand(() => { _ = global::Windows.System.Launcher.LaunchUriAsync(resUri); })
                });
            }
        }

        if (rg.Resources.Count == 0)
            rgItem.Items.Add(new MenuFlyoutItem { Text = "Loading...", IsEnabled = false });

        rgItem.Items.Add(new MenuFlyoutSeparator());
        var rgUri = rg.PortalUri;
        rgItem.Items.Add(new MenuFlyoutItem
        {
            Text = "Open Resource Group in Portal",
            Command = new RelayCommand(() => { _ = global::Windows.System.Launcher.LaunchUriAsync(rgUri); })
        });
        rgItem.Items.Add(new MenuFlyoutItem { Text = "Unpin", Command = rg.UnpinCommand });

        return rgItem;
    }

    private static MenuFlyoutSubItem BuildRunnableResourceSubItem(TrayResourceViewModel res)
    {
        var item = new MenuFlyoutSubItem
        {
            Text = res.Name,
            Icon = SymbolIconFor(res.Resource.Type)
        };

        if (res.RunningState != AppRunningState.Running)
            item.Items.Add(new MenuFlyoutItem { Text = "Start", Command = res.StartCommand });

        if (res.RunningState != AppRunningState.Stopped)
            item.Items.Add(new MenuFlyoutItem { Text = "Stop", Command = res.StopCommand });

        item.Items.Add(new MenuFlyoutItem { Text = "Restart", Command = res.RestartCommand });
        item.Items.Add(new MenuFlyoutSeparator());

        var uri = res.PortalUri;
        item.Items.Add(new MenuFlyoutItem
        {
            Text = "Open in Portal",
            Command = new RelayCommand(() => { _ = global::Windows.System.Launcher.LaunchUriAsync(uri); })
        });

        return item;
    }

    private static SymbolIcon SymbolIconFor(string type) => new(type.ToLowerInvariant() switch
    {
        "microsoft.web/sites" or "microsoft.web/sites/slots" => Symbol.Globe,
        "microsoft.keyvault/vaults"                          => Symbol.Permissions,
        "microsoft.storage/storageaccounts"                  => Symbol.SaveLocal,
        "microsoft.insights/components"                      => Symbol.Find,
        "microsoft.sql/servers"                              => Symbol.List,
        "microsoft.documentdb/databaseaccounts"              => Symbol.List,
        "microsoft.app/containerapps"                        => Symbol.AllApps,
        "microsoft.logic/workflows"                          => Symbol.Sync,
        "microsoft.servicebus/namespaces"                    => Symbol.Forward,
        "microsoft.apimanagement/service"                    => Symbol.Manage,
        "microsoft.resources/resourcegroups"                 => Symbol.Folder,
        _                                                    => Symbol.Globe
    });

    private void OnNavSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem { Tag: "Settings" })
            ContentFrame.Navigate(typeof(SettingsPage));
        else if (args.SelectedItem is NavigationViewItem { Tag: "Browse" })
            ContentFrame.Navigate(typeof(BrowsePage));
    }
}
