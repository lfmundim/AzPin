using AzPin.Windows.Data;
using AzPin.Windows.Models.Arm;
using AzPin.Windows.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace AzPin.Windows.Services;

public class PinService(IDbContextFactory<AzPinDbContext> dbFactory) : IPinService
{
    public event Action? PinsChanged;
    public async Task<bool> IsResourcePinnedAsync(string resourceId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.PinnedResources.AnyAsync(r => r.Id == resourceId, ct);
    }

    public async Task PinResourceAsync(ArmResource resource, string subscriptionId, string resourceGroupName, int displayOrder, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        if (await db.PinnedResources.AnyAsync(r => r.Id == resource.Id, ct))
            return;

        db.PinnedResources.Add(new PinnedResource
        {
            Id = resource.Id,
            Name = resource.Name,
            Type = resource.Type,
            ResourceGroup = resourceGroupName,
            SubscriptionId = subscriptionId,
            Location = resource.Location,
            DisplayOrder = displayOrder
        });
        await db.SaveChangesAsync(ct);
        PinsChanged?.Invoke();
    }

    public async Task UnpinResourceAsync(string resourceId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var entity = await db.PinnedResources.SingleOrDefaultAsync(r => r.Id == resourceId, ct);
        if (entity is not null)
        {
            db.PinnedResources.Remove(entity);
            await db.SaveChangesAsync(ct);
            PinsChanged?.Invoke();
        }
    }

    public async Task<bool> IsResourceGroupPinnedAsync(string subscriptionId, string rgName, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.PinnedResourceGroups.AnyAsync(
            rg => rg.SubscriptionId == subscriptionId && rg.Name == rgName, ct);
    }

    public async Task PinResourceGroupAsync(ArmResourceGroup rg, string subscriptionId, int displayOrder, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        if (await db.PinnedResourceGroups.AnyAsync(
                r => r.SubscriptionId == subscriptionId && r.Name == rg.Name, ct))
            return;

        db.PinnedResourceGroups.Add(new PinnedResourceGroup
        {
            Id = rg.Id,
            SubscriptionId = subscriptionId,
            Name = rg.Name,
            DisplayOrder = displayOrder
        });
        await db.SaveChangesAsync(ct);
        PinsChanged?.Invoke();
    }

    public async Task UnpinResourceGroupAsync(string subscriptionId, string rgName, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var entity = await db.PinnedResourceGroups.SingleOrDefaultAsync(
            rg => rg.SubscriptionId == subscriptionId && rg.Name == rgName, ct);
        if (entity is not null)
        {
            db.PinnedResourceGroups.Remove(entity);
            await db.SaveChangesAsync(ct);
            PinsChanged?.Invoke();
        }
    }

    public async Task<IReadOnlyList<PinnedResource>> GetPinnedResourcesAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.PinnedResources.OrderBy(r => r.DisplayOrder).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<PinnedResourceGroup>> GetPinnedResourceGroupsAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.PinnedResourceGroups.OrderBy(rg => rg.DisplayOrder).ToListAsync(ct);
    }
}
