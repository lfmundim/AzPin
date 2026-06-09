using AzPin.Windows.Models.Arm;
using AzPin.Windows.Models.Entities;
using AzPin.Windows.Services;

namespace AzPin.Windows.Tests.Mocks;

internal sealed class FakePinService : IPinService
{
    private readonly List<PinnedResource> _resources = [];
    private readonly List<PinnedResourceGroup> _resourceGroups = [];

    public event Action? PinsChanged;

    public int PinResourceCalls { get; private set; }
    public int UnpinResourceCalls { get; private set; }

    public Task<bool> IsResourcePinnedAsync(string resourceId, CancellationToken ct = default)
        => Task.FromResult(_resources.Any(r => r.Id == resourceId));

    public Task PinResourceAsync(ArmResource resource, string subscriptionId, string rgName, int displayOrder, CancellationToken ct = default)
    {
        PinResourceCalls++;
        if (_resources.All(r => r.Id != resource.Id))
            _resources.Add(new PinnedResource
            {
                Id = resource.Id, Name = resource.Name, Type = resource.Type,
                ResourceGroup = rgName, SubscriptionId = subscriptionId,
                Location = resource.Location, DisplayOrder = displayOrder
            });
        return Task.CompletedTask;
    }

    public Task UnpinResourceAsync(string resourceId, CancellationToken ct = default)
    {
        UnpinResourceCalls++;
        _resources.RemoveAll(r => r.Id == resourceId);
        return Task.CompletedTask;
    }

    public Task<bool> IsResourceGroupPinnedAsync(string subscriptionId, string rgName, CancellationToken ct = default)
        => Task.FromResult(_resourceGroups.Any(rg => rg.SubscriptionId == subscriptionId && rg.Name == rgName));

    public Task PinResourceGroupAsync(ArmResourceGroup rg, string subscriptionId, int displayOrder, CancellationToken ct = default)
    {
        if (_resourceGroups.All(r => !(r.SubscriptionId == subscriptionId && r.Name == rg.Name)))
            _resourceGroups.Add(new PinnedResourceGroup
            {
                Id = rg.Id, SubscriptionId = subscriptionId, Name = rg.Name, DisplayOrder = displayOrder
            });
        return Task.CompletedTask;
    }

    public Task UnpinResourceGroupAsync(string subscriptionId, string rgName, CancellationToken ct = default)
    {
        _resourceGroups.RemoveAll(rg => rg.SubscriptionId == subscriptionId && rg.Name == rgName);
        return Task.CompletedTask;
    }

    public Task DelayBeforeGet { get; set; } = Task.CompletedTask;

    public async Task<IReadOnlyList<PinnedResource>> GetPinnedResourcesAsync(CancellationToken ct = default)
    {
        await DelayBeforeGet;
        return _resources.OrderBy(r => r.DisplayOrder).ToList();
    }

    public Task<IReadOnlyList<PinnedResourceGroup>> GetPinnedResourceGroupsAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<PinnedResourceGroup>>(_resourceGroups.OrderBy(rg => rg.DisplayOrder).ToList());
}
