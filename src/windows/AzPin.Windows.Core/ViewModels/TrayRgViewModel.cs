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
    public Uri PortalUri => PortalUrl.ForResourceGroup(ResourceGroup.SubscriptionId, ResourceGroup.Name);

    [ObservableProperty]
    public partial ObservableCollection<TrayResourceViewModel> Resources { get; set; } = [];

    public TrayRgViewModel(PinnedResourceGroup rg, IArmService arm, IPinService pinService)
    {
        ResourceGroup = rg;
        _arm = arm;
        _pinService = pinService;
    }

    public async Task LoadResourcesAsync(CancellationToken ct = default)
    {
        try
        {
            var raw = await _arm.FetchResourcesAsync(ResourceGroup.SubscriptionId, string.Empty, ResourceGroup.Name, ct);
            var vms = raw.OrderBy(r => r.Type.ToLowerInvariant())
                         .Select(r => new TrayResourceViewModel(r, ResourceGroup.SubscriptionId, _arm))
                         .ToList();

            await Task.WhenAll(vms.Where(v => v.IsRunnable).Select(async v =>
            {
                var state = await _arm.FetchRunningStateAsync(ResourceGroup.SubscriptionId, string.Empty, v.Resource, ct);
                v.RunningState = state?.ToLowerInvariant() switch
                {
                    "running" => AppRunningState.Running,
                    "stopped" => AppRunningState.Stopped,
                    _ => AppRunningState.Unknown
                };
            }));

            Resources = new ObservableCollection<TrayResourceViewModel>(vms);
        }
        catch
        {
            Resources = [];
        }
    }

    [RelayCommand]
    public async Task UnpinAsync(CancellationToken ct = default)
    {
        await _pinService.UnpinResourceGroupAsync(ResourceGroup.SubscriptionId, ResourceGroup.Name, ct);
    }
}
