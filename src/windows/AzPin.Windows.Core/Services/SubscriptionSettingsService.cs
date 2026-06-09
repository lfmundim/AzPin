using AzPin.Windows.Data;
using AzPin.Windows.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace AzPin.Windows.Services;

public class SubscriptionSettingsService(IDbContextFactory<AzPinDbContext> dbFactory) : ISubscriptionSettingsService
{
    public async Task<IReadOnlySet<string>> GetHiddenSubscriptionIdsAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var ids = await db.HiddenSubscriptions.Select(h => h.SubscriptionId).ToListAsync(ct);
        return new HashSet<string>(ids, StringComparer.OrdinalIgnoreCase);
    }

    public async Task SetVisibilityAsync(string subscriptionId, bool visible, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        if (visible)
        {
            var entity = await db.HiddenSubscriptions.FindAsync([subscriptionId], ct);
            if (entity is not null)
            {
                db.HiddenSubscriptions.Remove(entity);
                await db.SaveChangesAsync(ct);
            }
        }
        else
        {
            if (!await db.HiddenSubscriptions.AnyAsync(h => h.SubscriptionId == subscriptionId, ct))
            {
                db.HiddenSubscriptions.Add(new HiddenSubscription { SubscriptionId = subscriptionId });
                await db.SaveChangesAsync(ct);
            }
        }
    }
}
