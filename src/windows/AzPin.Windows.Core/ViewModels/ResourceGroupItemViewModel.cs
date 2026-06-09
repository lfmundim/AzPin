using System.Collections.ObjectModel;
using AzPin.Windows.Models.Arm;
using AzPin.Windows.Services;
using AzPin.Windows.Utilities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AzPin.Windows.ViewModels;

public partial class ResourceGroupItemViewModel : ObservableObject
{
    private readonly IArmService _arm;
    private readonly IPinService _pinService;

    public ArmResourceGroup ResourceGroup { get; }
    public string SubscriptionId { get; }
    public string TenantId { get; }
    public Uri PortalUri => PortalUrl.ForResourceGroup(SubscriptionId, ResourceGroup.Name);

    [ObservableProperty]
    public partial bool IsExpanded { get; set; }

    [ObservableProperty]
    public partial bool IsLoadingResources { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<ResourceItemViewModel> Resources { get; set; } = [];

    [ObservableProperty]
    public partial string? ResourceError { get; set; }

    [ObservableProperty]
    public partial bool IsRgPinned { get; set; }

    public ResourceGroupItemViewModel(
        ArmResourceGroup rg,
        string subscriptionId,
        string tenantId,
        IArmService arm,
        IPinService pinService)
    {
        ResourceGroup = rg;
        SubscriptionId = subscriptionId;
        TenantId = tenantId;
        _arm = arm;
        _pinService = pinService;
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        IsRgPinned = await _pinService.IsResourceGroupPinnedAsync(SubscriptionId, ResourceGroup.Name, ct);
    }

    [RelayCommand]
    public async Task ToggleRgPinAsync(CancellationToken ct = default)
    {
        if (IsRgPinned)
        {
            await _pinService.UnpinResourceGroupAsync(SubscriptionId, ResourceGroup.Name, ct);
            IsRgPinned = false;
        }
        else
        {
            await _pinService.PinResourceGroupAsync(ResourceGroup, SubscriptionId, 0, ct);
            IsRgPinned = true;
        }
    }

    [RelayCommand]
    public async Task ToggleAsync(CancellationToken ct = default)
    {
        IsExpanded = !IsExpanded;
        if (IsExpanded && Resources.Count == 0)
            await LoadResourcesAsync(ct);
    }

    public async Task LoadResourcesAsync(CancellationToken ct = default)
    {
        IsLoadingResources = true;
        ResourceError = null;
        try
        {
            var raw = await _arm.FetchResourcesAsync(SubscriptionId, TenantId, ResourceGroup.Name, ct);
            var items = raw
                .OrderBy(r => r.Type.ToLowerInvariant())
                .Select(r => new ResourceItemViewModel(r, SubscriptionId, ResourceGroup.Name, _pinService));

            var initialized = await Task.WhenAll(items.Select(async item =>
            {
                await item.InitializeAsync(ct);
                return item;
            }));

            Resources = new ObservableCollection<ResourceItemViewModel>(initialized);
        }
        catch (Exception ex)
        {
            ResourceError = ex.Message;
        }
        finally
        {
            IsLoadingResources = false;
        }
    }
}
