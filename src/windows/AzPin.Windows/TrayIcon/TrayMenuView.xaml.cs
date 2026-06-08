using AzPin.Windows.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

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
            _vm.Auth.PropertyChanged -= OnAuthPropertyChanged;

        _vm = args.NewValue as TrayMenuViewModel;

        if (_vm is not null)
        {
            _vm.Auth.PropertyChanged += OnAuthPropertyChanged;
            UpdateAuthRow(_vm.Auth.State, _vm.Auth.IsRefreshing);
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

    private void OnOpenClick(object sender, RoutedEventArgs e) =>
        _vm?.OpenMainWindowCommand.Execute(null);

    private void OnQuitClick(object sender, RoutedEventArgs e) =>
        _vm?.QuitCommand.Execute(null);
}
