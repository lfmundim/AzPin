using AzPin.Windows.Services;
using AzPin.Windows.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace AzPin.Windows.TrayIcon;

public sealed partial class TrayMenuView : UserControl
{
    private TrayMenuViewModel? _vm;

    public TrayMenuView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        RegisterPropertyChangedCallback(VisibilityProperty, (_, _) => _ = RefreshOnOpenAsync());
    }

    private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        if (_vm is not null)
        {
            _vm.Auth.PropertyChanged -= OnAuthPropertyChanged;
            _vm.PropertyChanged -= OnVmPropertyChanged;
        }

        _vm = args.NewValue as TrayMenuViewModel;

        if (_vm is not null)
        {
            _vm.Auth.PropertyChanged += OnAuthPropertyChanged;
            _vm.PropertyChanged += OnVmPropertyChanged;
            UpdateAuthRow(_vm.Auth.State, _vm.Auth.IsRefreshing);
        }
    }

    private void OnVmPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (_vm is null) return;
        if (e.PropertyName is nameof(TrayMenuViewModel.UpdateState)
            or nameof(TrayMenuViewModel.UpdateLatestVersion)
            or nameof(TrayMenuViewModel.UpdateReleaseUrl))
        {
            UpdateUpdateRow();
        }
    }

    private void UpdateUpdateRow()
    {
        if (_vm is null) return;

        CheckUpdatesButton.IsEnabled = _vm.UpdateState != UpdateCheckState.Checking;

        switch (_vm.UpdateState)
        {
            case UpdateCheckState.Idle:
                UpdateStatusText.Visibility = Visibility.Collapsed;
                UpdateReleaseLink.Visibility = Visibility.Collapsed;
                break;

            case UpdateCheckState.Checking:
                UpdateStatusText.Text = "Checking…";
                UpdateStatusText.Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"];
                UpdateStatusText.Visibility = Visibility.Visible;
                UpdateReleaseLink.Visibility = Visibility.Collapsed;
                break;

            case UpdateCheckState.UpToDate:
                UpdateStatusText.Text = $"  {_vm.UpdateLatestVersion} is the latest";
                UpdateStatusText.Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"];
                UpdateStatusText.Visibility = Visibility.Visible;
                UpdateReleaseLink.Visibility = Visibility.Collapsed;
                break;

            case UpdateCheckState.UpdateAvailable:
                UpdateStatusText.Text = $"  v{_vm.UpdateLatestVersion} available — run: winget upgrade lfmundim.AzPin";
                UpdateStatusText.Foreground = (Brush)Application.Current.Resources["TextFillColorPrimaryBrush"];
                UpdateStatusText.Visibility = Visibility.Visible;
                if (_vm.UpdateReleaseUrl is not null && Uri.TryCreate(_vm.UpdateReleaseUrl, UriKind.Absolute, out var uri))
                {
                    UpdateReleaseLink.NavigateUri = uri;
                    UpdateReleaseLink.Visibility = Visibility.Visible;
                }
                break;

            case UpdateCheckState.Failed:
                UpdateStatusText.Text = "  Update check failed";
                UpdateStatusText.Foreground = (Brush)Application.Current.Resources["SystemFillColorCriticalBrush"];
                UpdateStatusText.Visibility = Visibility.Visible;
                UpdateReleaseLink.Visibility = Visibility.Collapsed;
                break;
        }
    }

    private async Task RefreshOnOpenAsync()
    {
        if (Visibility != Visibility.Visible || _vm is null)
            return;

        await _vm.OnMenuOpenedAsync();
        UpdateAuthRow(_vm.Auth.State, _vm.Auth.IsRefreshing);
        UpdatePinnedList();
    }

    private void OnAuthPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (_vm is null) return;
        if (e.PropertyName is nameof(AuthViewModel.State)
            or nameof(AuthViewModel.IsRefreshing)
            or nameof(AuthViewModel.AccountName))
        {
            UpdateAuthRow(_vm.Auth.State, _vm.Auth.IsRefreshing);
        }
    }

    private void UpdateAuthRow(AuthState state, bool isRefreshing)
    {
        RefreshingRing.Visibility = isRefreshing || state == AuthState.Unknown
            ? Visibility.Visible : Visibility.Collapsed;

        StatusTextBlock.Text = state switch
        {
            AuthState.SignedIn when !string.IsNullOrWhiteSpace(_vm?.Auth.AccountName)
                => _vm!.Auth.AccountName!,
            AuthState.SignedIn          => "Signed in",
            AuthState.NotSignedIn       => "Not signed in — run az login",
            AuthState.CliNotInstalled   => "Azure CLI not installed",
            _                           => "Refreshing..."
        };
    }

    private void UpdatePinnedList()
    {
        if (_vm is null) return;

        var loading = _vm.IsLoadingPinnedResources;
        var items   = _vm.PinnedResources;

        PinnedLoadingRing.Visibility = loading ? Visibility.Visible : Visibility.Collapsed;

        if (loading)
        {
            PinnedList.Visibility    = Visibility.Collapsed;
            NoPinnedText.Visibility  = Visibility.Collapsed;
            return;
        }

        if (items.Count == 0)
        {
            PinnedList.Visibility    = Visibility.Collapsed;
            NoPinnedText.Visibility  = Visibility.Visible;
        }
        else
        {
            PinnedList.ItemsSource   = items;
            PinnedList.Visibility    = Visibility.Visible;
            NoPinnedText.Visibility  = Visibility.Collapsed;
        }
    }

    private void OnCheckUpdatesClick(object sender, RoutedEventArgs e) =>
        _ = _vm?.CheckForUpdatesCommand.ExecuteAsync(null);

    private void OnOpenClick(object sender, RoutedEventArgs e) =>
        _vm?.OpenMainWindowCommand.Execute(null);

    private void OnQuitClick(object sender, RoutedEventArgs e) =>
        _vm?.QuitCommand.Execute(null);
}
