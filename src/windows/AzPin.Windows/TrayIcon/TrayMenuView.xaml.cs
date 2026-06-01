using AzPin.Windows.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AzPin.Windows.TrayIcon;

public sealed partial class TrayMenuView : UserControl
{
    private AuthViewModel? _boundAuth;

    public TrayMenuView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        RegisterPropertyChangedCallback(VisibilityProperty, (_, _) => _ = RefreshOnOpenAsync());
    }

    private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        if (_boundAuth is not null)
        {
            _boundAuth.PropertyChanged -= OnAuthPropertyChanged;
            _boundAuth = null;
        }

        if (args.NewValue is TrayMenuViewModel vm)
        {
            _boundAuth = vm.Auth;
            vm.Auth.PropertyChanged += OnAuthPropertyChanged;
            UpdateStatusRows(vm.Auth.State, vm.Auth.IsRefreshing);
        }
    }

    private async Task RefreshOnOpenAsync()
    {
        if (Visibility != Visibility.Visible)
        {
            return;
        }

        if (DataContext is TrayMenuViewModel vm)
        {
            await vm.OnMenuOpenedAsync();
            UpdateStatusRows(vm.Auth.State, vm.Auth.IsRefreshing);
        }
    }

    private void OnAuthPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (sender is AuthViewModel auth &&
            (e.PropertyName == nameof(AuthViewModel.State) || e.PropertyName == nameof(AuthViewModel.IsRefreshing)))
        {
            UpdateStatusRows(auth.State, auth.IsRefreshing);
        }
    }

    private void UpdateStatusRows(AuthState state, bool isRefreshing)
    {
        RefreshingRow.Visibility = isRefreshing || state == AuthState.Unknown ? Visibility.Visible : Visibility.Collapsed;
        SignedInRow.Visibility = state == AuthState.SignedIn && !isRefreshing ? Visibility.Visible : Visibility.Collapsed;
        NotSignedInRow.Visibility = state == AuthState.NotSignedIn && !isRefreshing ? Visibility.Visible : Visibility.Collapsed;
        CliMissingRow.Visibility = state == AuthState.CliNotInstalled && !isRefreshing ? Visibility.Visible : Visibility.Collapsed;
    }
}
