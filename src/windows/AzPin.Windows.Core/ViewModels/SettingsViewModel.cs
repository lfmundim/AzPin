using System.Collections.ObjectModel;
using AzPin.Windows.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace AzPin.Windows.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly AuthViewModel _auth;
    private readonly IAzCliService _azCli;
    private readonly ISubscriptionSettingsService _subscriptionSettings;

    private const string StartupRegKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string StartupValueName = "AzPin";
    private bool _suppressOpenAtLoginChange;

    public AuthViewModel Auth => _auth;

    [ObservableProperty]
    public partial ObservableCollection<SubscriptionItemViewModel> Subscriptions { get; set; } = [];

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial bool OpenAtLogin { get; set; }

    public SettingsViewModel(AuthViewModel auth, IAzCliService azCli, ISubscriptionSettingsService subscriptionSettings)
    {
        _auth = auth;
        _azCli = azCli;
        _subscriptionSettings = subscriptionSettings;
        LoadOpenAtLogin();
    }

    private void LoadOpenAtLogin()
    {
        _suppressOpenAtLoginChange = true;
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(StartupRegKeyPath);
            OpenAtLogin = key?.GetValue(StartupValueName) is not null;
        }
        catch { OpenAtLogin = false; }
        finally { _suppressOpenAtLoginChange = false; }
    }

    partial void OnOpenAtLoginChanged(bool value)
    {
        if (_suppressOpenAtLoginChange) return;
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(StartupRegKeyPath, writable: true);
            if (key is null) return;
            if (value)
            {
                var exePath = Environment.ProcessPath
                              ?? Path.Combine(AppContext.BaseDirectory, "AzPin.Windows.exe");
                key.SetValue(StartupValueName, exePath);
            }
            else
            {
                key.DeleteValue(StartupValueName, throwOnMissingValue: false);
            }
        }
        catch { /* registry write failed */ }
    }

    [RelayCommand]
    public async Task LoadAsync(CancellationToken ct = default)
    {
        IsLoading = true;
        try
        {
            var subs = await _azCli.ListSubscriptionsAsync(ct);
            var hidden = await _subscriptionSettings.GetHiddenSubscriptionIdsAsync(ct);

            Subscriptions = new ObservableCollection<SubscriptionItemViewModel>(
                subs.OrderBy(s => s.Name)
                    .Select(s => new SubscriptionItemViewModel(s, !hidden.Contains(s.Id), _subscriptionSettings)));
        }
        catch { }
        finally { IsLoading = false; }
    }
}
