using System.Linq;
using System.Runtime.InteropServices;
using AzPin.Windows.MainWindow.Pages;
using AzPin.Windows.Models;
using AzPin.Windows.TrayIcon;
using AzPin.Windows.Utilities;
using AzPin.Windows.ViewModels;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.Foundation;
using Windows.Graphics;

namespace AzPin.Windows.MainWindow;

public sealed partial class MainWindow : Window
{
    // Created in code so ShowAt uses this window's XamlRoot (not H.NotifyIcon's internal popup).
    // H.NotifyIcon's popup window has no WinUI control templates loaded, so SymbolIcon renders blank.
    private readonly MenuFlyout TrayFlyout = new() { AreOpenCloseAnimationsEnabled = false };

    private MainWindowViewModel? _vm;

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out System.Drawing.Point lpPoint);

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
        NavView.SelectedItem = NavView.MenuItems.OfType<NavigationViewItem>().FirstOrDefault(i => i.Tag as string == "Browse");

        _vm = App.Services.GetRequiredService<MainWindowViewModel>();
        PinnedRgList.ItemsSource = _vm.PinnedRgs;
        _vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainWindowViewModel.PinnedRgs))
            {
                PinnedRgList.ItemsSource = _vm.PinnedRgs;
                NoPinnedRgsText.Visibility = _vm.PinnedRgs.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
        };
        _ = _vm.LoadPinnedRgsAsync();
    }

    public void InitializeTrayIcon(TrayMenuViewModel vm)
    {
        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "tray.ico");
        TrayIcon.Icon = new System.Drawing.Icon(iconPath);
        TrayIcon.ToolTipText = "AzPin";
        TrayIcon.LeftClickCommand = vm.OpenMainWindowCommand;
        AppWindow.SetIcon(iconPath);

        RebuildContextMenu(vm);
        vm.PropertyChanged += (_, _) => DispatcherQueue.TryEnqueue(() => RebuildContextMenu(vm));
        vm.Auth.PropertyChanged += (_, _) => DispatcherQueue.TryEnqueue(() => RebuildContextMenu(vm));

        // Show flyout manually so it anchors to this window's XamlRoot, not H.NotifyIcon's.
        TrayIcon.RightClickCommand = new RelayCommand(() => DispatcherQueue.TryEnqueue(ShowTrayFlyout));

        _ = vm.OnMenuOpenedAsync();
    }

    public async Task<bool> ShowOnboardingAsync(OnboardingViewModel vm)
    {
        var page = new OnboardingPage(vm);
        var dialog = new ContentDialog
        {
            Title = "Welcome to AzPin",
            Content = page,
            PrimaryButtonText = "Get Started",
            XamlRoot = Content.XamlRoot
        };
        dialog.SetBinding(ContentDialog.IsPrimaryButtonEnabledProperty,
            new Microsoft.UI.Xaml.Data.Binding
            {
                Source = vm,
                Path = new Microsoft.UI.Xaml.PropertyPath(nameof(OnboardingViewModel.CanGetStarted)),
                Mode = Microsoft.UI.Xaml.Data.BindingMode.OneWay
            });

        vm.StartPolling();
        var result = await dialog.ShowAsync();
        vm.StopPolling();

        if (result == ContentDialogResult.Primary)
        {
            vm.CompleteOnboarding();
            return true;
        }
        return false;
    }

    private void ShowTrayFlyout()
    {
        GetCursorPos(out var screenPt);
        var winPos = AppWindow.Position;
        double scale = ((FrameworkElement)Content).XamlRoot.RasterizationScale;
        double x = (screenPt.X - winPos.X) / scale;
        double y = (screenPt.Y - winPos.Y) / scale;
        TrayFlyout.ShowAt((FrameworkElement)Content, new FlyoutShowOptions
        {
            Position = new Point(x, y),
            Placement = FlyoutPlacementMode.TopEdgeAlignedLeft,
            ShowMode = FlyoutShowMode.Transient
        });
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

        if (vm.Auth.State == AuthState.CliNotInstalled)
        {
            TrayFlyout.Items.Add(new MenuFlyoutItem
            {
                Text = "Install Azure CLI...",
                Command = new RelayCommand(() =>
                    _ = global::Windows.System.Launcher.LaunchUriAsync(
                        new Uri("https://aka.ms/installazurecliwindows")))
            });
        }
        else if (vm.Auth.State == AuthState.NotSignedIn)
        {
            TrayFlyout.Items.Add(new MenuFlyoutItem
            {
                Text = "Open Terminal",
                Command = vm.OpenTerminalCommand
            });
        }

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
            Text = rg.DisplayLabel,
            Icon = new SymbolIcon(Symbol.Folder)
        };

        if (rg.HasError)
        {
            rgItem.Items.Add(new MenuFlyoutItem
            {
                Text = rg.ErrorMessage ?? "Failed to load resources",
                IsEnabled = false
            });
        }
        else
        {
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
        }

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

        if (res.ShowStartButton)
            item.Items.Add(new MenuFlyoutItem { Text = "Start", Command = res.StartCommand });

        if (res.ShowStopButton)
            item.Items.Add(new MenuFlyoutItem { Text = "Stop", Command = res.StopCommand });

        if (res.ShowRestartButton)
            item.Items.Add(new MenuFlyoutItem { Text = "Restart", Command = res.RestartCommand });

        if (res.ActionError is not null)
            item.Items.Add(new MenuFlyoutItem { Text = res.ActionError, IsEnabled = false });

        item.Items.Add(new MenuFlyoutSeparator());

        var uri = res.PortalUri;
        item.Items.Add(new MenuFlyoutItem
        {
            Text = "Open in Portal",
            Command = new RelayCommand(() => { _ = global::Windows.System.Launcher.LaunchUriAsync(uri); })
        });

        return item;
    }

    private static SymbolIcon SymbolIconFor(string? type) => new((type?.ToLowerInvariant() ?? "") switch
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

    private void OnRgSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_vm is null) return;
        _vm.SelectedRg = PinnedRgList.SelectedItem as SidebarRgItemViewModel;
        if (_vm.SelectedRg is not null)
            ContentFrame.Navigate(typeof(DetailPage), _vm.SelectedRg);
    }

    private void OnRgDragCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
    {
        if (_vm is null) return;
        _ = _vm.PersistOrderAsync();
    }

    private void ShowRgContextMenu(SidebarRgItemViewModel rg, FrameworkElement anchor)
    {
        if (_vm is null) return;
        var flyout = new MenuFlyout();

        var uri = rg.PortalUri;
        flyout.Items.Add(new MenuFlyoutItem
        {
            Text = "Open in Portal",
            Command = new RelayCommand(() => _ = global::Windows.System.Launcher.LaunchUriAsync(uri))
        });
        flyout.Items.Add(new MenuFlyoutItem
        {
            Text = "Unpin",
            Command = new RelayCommand(() => _ = _vm.UnpinRgAsync(rg))
        });
        flyout.ShowAt(anchor);
    }
}
