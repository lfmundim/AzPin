using System.Collections.ObjectModel;
using AzPin.Windows.Models;
using AzPin.Windows.Models.Entities;
using AzPin.Windows.Services;
using AzPin.Windows.Utilities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AzPin.Windows.ViewModels;

public partial class TrayRgViewModel : ObservableObject
{
    private readonly IArmService _arm;
    private readonly IPinService _pinService;

    public PinnedResourceGroup ResourceGroup { get; }
    public string Name => ResourceGroup.Name;
    public string DisplayLabel { get; set; }
    public Uri PortalUri => PortalUrl.ForResourceGroup(ResourceGroup.SubscriptionId, ResourceGroup.Name);

    [ObservableProperty]
    public partial ObservableCollection<TrayResourceViewModel> Resources { get; set; } = [];

    [ObservableProperty]
    public partial bool HasError { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    public TrayRgViewModel(PinnedResourceGroup rg, IArmService arm, IPinService pinService)
    {
        ResourceGroup = rg;
        _arm = arm;
        _pinService = pinService;
        DisplayLabel = rg.Name;
    }

    public async Task LoadResourcesAsync(CancellationToken ct = default)
    {
        HasError = false;
        ErrorMessage = null;
        try
        {
            var raw = await _arm.FetchResourcesAsync(ResourceGroup.SubscriptionId, string.Empty, ResourceGroup.Name, ct);
            var vms = raw.OrderBy(r => r.Type.ToLowerInvariant())
                         .Select(r => new TrayResourceViewModel(r, ResourceGroup.SubscriptionId, _arm))
                         .ToList();
            Resources = new ObservableCollection<TrayResourceViewModel>(vms);
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
            Resources = [];
        }
    }

    public async Task FetchRunningStatesAsync(CancellationToken ct = default)
    {
        var runnables = Resources.Where(v => v.IsRunnable).ToList();
        await Task.WhenAll(runnables.Select(async v =>
        {
            v.RunningState = AppRunningState.Fetching;
            v.RunningState = await _arm.FetchRunningStateAsync(ResourceGroup.SubscriptionId, string.Empty, v.Resource, ct);
        }));
    }

    public async Task CheckPermissionsAsync(IPermissionsService permissions, CancellationToken ct = default)
    {
        var runnables = Resources.Where(v => v.IsRunnable).ToList();
        await Task.WhenAll(runnables.Select(async v =>
        {
            v.Permissions = await permissions.CheckAccessAsync(
                ResourceGroup.SubscriptionId, string.Empty,
                v.Resource.Id, v.Resource.Type, ct);
        }));
    }

    [RelayCommand]
    public async Task UnpinAsync(CancellationToken ct = default)
    {
        await _pinService.UnpinResourceGroupAsync(ResourceGroup.SubscriptionId, ResourceGroup.Name, ct);
    }
}
