using AzPin.Windows.Models.Arm;
using AzPin.Windows.Services;

namespace AzPin.Windows.Tests.Mocks;

internal sealed class FakeArmService : IArmService
{
    public IReadOnlyList<ArmSubscription> Subscriptions { get; set; } = [];
    public IReadOnlyList<ArmResourceGroup> ResourceGroups { get; set; } = [];
    public IReadOnlyList<ArmResource> Resources { get; set; } = [];
    public Exception? ThrowOnFetchResourceGroups { get; set; }
    public Exception? ThrowOnFetchResources { get; set; }

    public Task<IReadOnlyList<ArmSubscription>> FetchSubscriptionsAsync(CancellationToken ct = default)
        => Task.FromResult(Subscriptions);

    public Task<IReadOnlyList<ArmResourceGroup>> FetchResourceGroupsAsync(
        string subscriptionId, string tenantId, CancellationToken ct = default)
    {
        if (ThrowOnFetchResourceGroups is not null) throw ThrowOnFetchResourceGroups;
        return Task.FromResult(ResourceGroups);
    }

    public Task<IReadOnlyList<ArmResource>> FetchResourcesAsync(
        string subscriptionId, string tenantId, string resourceGroupName, CancellationToken ct = default)
    {
        if (ThrowOnFetchResources is not null) throw ThrowOnFetchResources;
        return Task.FromResult(Resources);
    }

    public Task<string?> FetchRunningStateAsync(string subscriptionId, string tenantId, ArmResource resource, CancellationToken ct = default)
        => Task.FromResult<string?>(null);

    public Task StartResourceAsync(string subscriptionId, string tenantId, ArmResource resource, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task StopResourceAsync(string subscriptionId, string tenantId, ArmResource resource, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task RestartResourceAsync(string subscriptionId, string tenantId, ArmResource resource, CancellationToken ct = default)
        => Task.CompletedTask;
}
