using System.Collections.ObjectModel;
using System.Diagnostics;
using AzPin.Windows.Services;
using AzPin.Windows.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AzPin.Windows.TrayIcon;

public partial class TrayMenuViewModel : ObservableObject
{
    private readonly AuthViewModel _auth;
    private readonly IPinService _pinService;
    private readonly IArmService _arm;
    private readonly IPermissionsService _permissions;
    private readonly IUpdateCheckService _updateCheck;
    private readonly Action _quit;
    private readonly Action _openMainWindow;

    public AuthViewModel Auth => _auth;

    [ObservableProperty]
    public partial ObservableCollection<PinnedResourceItemViewModel> PinnedResources { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<TrayRgViewModel> PinnedRgItems { get; set; } = [];

    [ObservableProperty]
    public partial bool IsLoadingPinnedResources { get; set; }

    [ObservableProperty]
    public partial UpdateCheckState UpdateState { get; set; } = UpdateCheckState.Idle;

    [ObservableProperty]
    public partial string? UpdateLatestVersion { get; set; }

    [ObservableProperty]
    public partial string? UpdateReleaseUrl { get; set; }

    public TrayMenuViewModel(AuthViewModel auth, IPinService pinService, IArmService arm, IPermissionsService permissions, IUpdateCheckService updateCheck, Action quit, Action openMainWindow)
    {
        _auth = auth;
        _pinService = pinService;
        _arm = arm;
        _permissions = permissions;
        _updateCheck = updateCheck;
        _quit = quit;
        _openMainWindow = openMainWindow;
        pinService.PinsChanged += () => _ = LoadPinnedResourcesAsync();
    }

    [RelayCommand]
    public async Task CheckForUpdatesAsync()
    {
        if (UpdateState == UpdateCheckState.Checking) return;
        UpdateState = UpdateCheckState.Checking;
        var result = await _updateCheck.CheckForUpdatesAsync();
        UpdateLatestVersion = result.LatestVersion;
        UpdateReleaseUrl = result.ReleaseUrl;
        UpdateState = result.State;
    }

    [RelayCommand]
    public async Task OnMenuOpenedAsync()
    {
        await _auth.RefreshAsync();
        await LoadPinnedResourcesAsync();
    }

    private async Task LoadPinnedResourcesAsync()
    {
        IsLoadingPinnedResources = true;
        try
        {
            var pinned = await _pinService.GetPinnedResourcesAsync();
            var pinnedRgs = await _pinService.GetPinnedResourceGroupsAsync();

            var rgVms = pinnedRgs.OrderBy(rg => rg.DisplayOrder)
                                 .Select(rg => new TrayRgViewModel(rg, _arm, _pinService))
                                 .ToList();

            // Disambiguate RG display labels when two pinned RGs share the same name across subs.
            var duplicateNames = pinnedRgs
                .GroupBy(rg => rg.Name, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var vm in rgVms.Where(vm => duplicateNames.Contains(vm.Name)))
            {
                var subPrefix = vm.ResourceGroup.SubscriptionId[..Math.Min(8, vm.ResourceGroup.SubscriptionId.Length)];
                vm.DisplayLabel = $"{vm.Name} · {subPrefix}";
            }

            await Task.WhenAll(rgVms.Select(vm => vm.LoadResourcesAsync()));
            await Task.WhenAll(rgVms.Select(vm => vm.FetchRunningStatesAsync()));
            await Task.WhenAll(rgVms.Select(vm => vm.CheckPermissionsAsync(_permissions)));

            // Exclude individually-pinned resources whose parent RG is also pinned.
            var pinnedRgKeys = pinnedRgs
                .Select(rg => (rg.SubscriptionId, Name: rg.Name.ToLowerInvariant()))
                .ToHashSet();

            var dedupedPinned = pinned
                .Where(p => !pinnedRgKeys.Contains((p.SubscriptionId, p.ResourceGroup.ToLowerInvariant())))
                .OrderBy(p => p.DisplayOrder);

            PinnedResources = new ObservableCollection<PinnedResourceItemViewModel>(
                dedupedPinned.Select(p => new PinnedResourceItemViewModel(p)));

            PinnedRgItems = new ObservableCollection<TrayRgViewModel>(rgVms);
        }
        finally
        {
            IsLoadingPinnedResources = false;
        }
    }

    [RelayCommand]
    private void OpenMainWindow() => _openMainWindow();

    [RelayCommand]
    private void Quit() => _quit();

    [RelayCommand]
    private void OpenTerminal()
    {
        try { Process.Start(new ProcessStartInfo("wt.exe") { UseShellExecute = true }); }
        catch { Process.Start(new ProcessStartInfo("cmd.exe") { UseShellExecute = true }); }
    }
}
