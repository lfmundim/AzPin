using AzPin.Windows.Models.Arm;
using AzPin.Windows.Models.Entities;

namespace AzPin.Windows.Services;

public interface IPinService
{
    event Action? PinsChanged;
    Task<bool> IsResourcePinnedAsync(string resourceId, CancellationToken ct = default);
    Task PinResourceAsync(ArmResource resource, string subscriptionId, string resourceGroupName, int displayOrder, CancellationToken ct = default);
    Task UnpinResourceAsync(string resourceId, CancellationToken ct = default);
    Task<bool> IsResourceGroupPinnedAsync(string subscriptionId, string rgName, CancellationToken ct = default);
    Task PinResourceGroupAsync(ArmResourceGroup rg, string subscriptionId, int displayOrder, CancellationToken ct = default);
    Task UnpinResourceGroupAsync(string subscriptionId, string rgName, CancellationToken ct = default);
    Task<IReadOnlyList<PinnedResource>> GetPinnedResourcesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<PinnedResourceGroup>> GetPinnedResourceGroupsAsync(CancellationToken ct = default);
}
