using AzPin.Windows.Models;
using AzPin.Windows.Models.Arm;

namespace AzPin.Windows.Services;

public interface IArmService
{
    Task<IReadOnlyList<ArmSubscription>> FetchSubscriptionsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ArmResourceGroup>> FetchResourceGroupsAsync(string subscriptionId, string tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<ArmResource>> FetchResourcesAsync(string subscriptionId, string tenantId, string resourceGroupName, CancellationToken ct = default);
    Task<AppRunningState> FetchRunningStateAsync(string subscriptionId, string tenantId, ArmResource resource, CancellationToken ct = default);
    Task StartResourceAsync(string subscriptionId, string tenantId, ArmResource resource, CancellationToken ct = default);
    Task StopResourceAsync(string subscriptionId, string tenantId, ArmResource resource, CancellationToken ct = default);
    Task RestartResourceAsync(string subscriptionId, string tenantId, ArmResource resource, CancellationToken ct = default);
}
