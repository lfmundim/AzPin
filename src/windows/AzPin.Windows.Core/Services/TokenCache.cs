using AzPin.Windows.Data;
using AzPin.Windows.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace AzPin.Windows.Services;

public class TokenCache(AzPinDbContext dbContext, IAzCliService azCliService) : ITokenCache
{
    private readonly AzPinDbContext _dbContext = dbContext;
    private readonly IAzCliService _azCliService = azCliService;

    public async Task<string> GetTokenAsync(string subscriptionId, string tenantId, CancellationToken ct = default)
    {
        var existing = await _dbContext.CachedTokens
            .SingleOrDefaultAsync(t => t.SubscriptionId == subscriptionId, ct);

        if (existing is not null && existing.ExpiresOn > DateTime.UtcNow.AddMinutes(5))
        {
            return existing.AccessToken;
        }

        var token = await _azCliService.GetAccessTokenAsync(subscriptionId, ct);
        var expiresOnUtc = token.ExpiresOnUtc;

        if (existing is null)
        {
            _dbContext.CachedTokens.Add(new CachedToken
            {
                SubscriptionId = subscriptionId,
                TenantId = token.TenantId,
                AccessToken = token.AccessToken,
                ExpiresOn = expiresOnUtc
            });
        }
        else
        {
            existing.TenantId = token.TenantId;
            existing.AccessToken = token.AccessToken;
            existing.ExpiresOn = expiresOnUtc;
        }

        await _dbContext.SaveChangesAsync(ct);
        return token.AccessToken;
    }
}
