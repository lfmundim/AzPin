using System.Collections.ObjectModel;
using System.Reflection;
using AzPin.Windows.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Windows.Storage;

namespace AzPin.Windows.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly AuthViewModel _auth;
    private readonly IAzCliService _azCli;
    private readonly ISubscriptionSettingsService _subscriptionSettings;
    private readonly IUpdateCheckService _updateCheck;

    public event Action? ReRunSetupRequested;

    private const string StartupRegKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string StartupValueName = "AzPin";
    private bool _suppressOpenAtLoginChange;

    public AuthViewModel Auth => _auth;

    public string CurrentVersion { get; } = GetCurrentVersion();

    [ObservableProperty]
    public partial ObservableCollection<SubscriptionItemViewModel> Subscriptions { get; set; } = [];

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial bool OpenAtLogin { get; set; }

    [ObservableProperty]
    public partial UpdateCheckState UpdateState { get; set; } = UpdateCheckState.Idle;

    [ObservableProperty]
    public partial string? LatestVersion { get; set; }

    [ObservableProperty]
    public partial string? ReleaseUrl { get; set; }

    [ObservableProperty]
    public partial string? UpdateErrorMessage { get; set; }

    public bool IsUpdateIdle => UpdateState == UpdateCheckState.Idle;
    public bool IsUpdateChecking => UpdateState == UpdateCheckState.Checking;
    public bool IsUpdateUpToDate => UpdateState == UpdateCheckState.UpToDate;
    public bool IsUpdateAvailable => UpdateState == UpdateCheckState.UpdateAvailable;
    public bool IsUpdateFailed => UpdateState == UpdateCheckState.Failed;
    public bool IsUpdateNotIdle => UpdateState != UpdateCheckState.Idle;

    partial void OnUpdateStateChanged(UpdateCheckState value)
    {
        OnPropertyChanged(nameof(IsUpdateIdle));
        OnPropertyChanged(nameof(IsUpdateChecking));
        OnPropertyChanged(nameof(IsUpdateUpToDate));
        OnPropertyChanged(nameof(IsUpdateAvailable));
        OnPropertyChanged(nameof(IsUpdateFailed));
        OnPropertyChanged(nameof(IsUpdateNotIdle));
    }

    public SettingsViewModel(AuthViewModel auth, IAzCliService azCli, ISubscriptionSettingsService subscriptionSettings, IUpdateCheckService updateCheck)
    {
        _auth = auth;
        _azCli = azCli;
        _subscriptionSettings = subscriptionSettings;
        _updateCheck = updateCheck;
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
    public void ReRunSetup()
    {
        ApplicationData.Current.LocalSettings.Values["HasCompletedOnboarding"] = false;
        ReRunSetupRequested?.Invoke();
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

    [RelayCommand]
    public async Task CheckForUpdatesAsync(CancellationToken ct = default)
    {
        if (UpdateState == UpdateCheckState.Checking) return;
        UpdateState = UpdateCheckState.Checking;
        var result = await _updateCheck.CheckForUpdatesAsync(ct);
        LatestVersion = result.LatestVersion;
        ReleaseUrl = result.ReleaseUrl;
        UpdateErrorMessage = result.ErrorMessage;
        UpdateState = result.State;
    }

    private static string GetCurrentVersion()
    {
        var v = Assembly.GetEntryAssembly()?.GetName().Version;
        if (v is null) return "0.0.0";
        return $"{v.Major}.{v.Minor}.{v.Build}";
    }
}
