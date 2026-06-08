using AzPin.Windows.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AzPin.Windows.ViewModels;

public enum AuthState
{
    Unknown,
    CliNotInstalled,
    NotSignedIn,
    SignedIn
}

public partial class AuthViewModel(IAzCliService azCli) : ObservableObject
{
    private readonly IAzCliService _azCli = azCli;

    [ObservableProperty]
    private AuthState _state = AuthState.Unknown;

    [ObservableProperty]
    private string? _accountName;

    [ObservableProperty]
    private string? _tenantId;

    [ObservableProperty]
    private string? _activeSubscriptionName;

    [ObservableProperty]
    private bool _isRefreshing;

    [RelayCommand]
    public async Task RefreshAsync(CancellationToken ct = default)
    {
        IsRefreshing = true;
        try
        {
            if (!_azCli.IsCliInstalled)
            {
                State = AuthState.CliNotInstalled;
                AccountName = null;
                TenantId = null;
                ActiveSubscriptionName = null;
                return;
            }

            var account = await _azCli.GetCurrentAccountAsync(ct);
            if (account is null)
            {
                State = AuthState.NotSignedIn;
                AccountName = null;
                TenantId = null;
                ActiveSubscriptionName = null;
                return;
            }

            var subs = await _azCli.ListSubscriptionsAsync(ct);
            AccountName = account.User?.Name;
            TenantId = account.TenantId;
            ActiveSubscriptionName = subs.FirstOrDefault(s => s.IsDefault)?.Name
                                     ?? subs.FirstOrDefault()?.Name;
            State = AuthState.SignedIn;
        }
        catch
        {
            State = AuthState.NotSignedIn;
            AccountName = null;
            TenantId = null;
            ActiveSubscriptionName = null;
        }
        finally
        {
            IsRefreshing = false;
        }
    }
}
