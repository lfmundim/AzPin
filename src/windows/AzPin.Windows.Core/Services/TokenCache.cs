using AzPin.Windows.Data;
using AzPin.Windows.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace AzPin.Windows.Services;

public class TokenCache(IDbContextFactory<AzPinDbContext> dbFactory, IAzCliService azCliService) : ITokenCache
{
    public async Task<string> GetTokenAsync(string subscriptionId, string tenantId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var existing = await db.CachedTokens
            .SingleOrDefaultAsync(t => t.SubscriptionId == subscriptionId, ct);

        if (existing is not null && existing.ExpiresOn > DateTime.UtcNow.AddMinutes(5))
            return existing.AccessToken;

        var token = await azCliService.GetAccessTokenAsync(subscriptionId, ct);

        if (existing is null)
        {
            db.CachedTokens.Add(new CachedToken
            {
                SubscriptionId = subscriptionId,
                TenantId = token.TenantId ?? string.Empty,
                AccessToken = token.AccessToken,
                ExpiresOn = token.ExpiresOnUtc
            });
        }
        else
        {
            existing.TenantId = token.TenantId ?? string.Empty;
            existing.AccessToken = token.AccessToken;
            existing.ExpiresOn = token.ExpiresOnUtc;
        }

        await db.SaveChangesAsync(ct);
        return token.AccessToken;
    }
}
