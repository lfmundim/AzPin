using AzPin.Windows.Data;
using AzPin.Windows.Models.Entities;
using AzPin.Windows.Services;
using AzPin.Windows.Tests.Mocks;
using Microsoft.EntityFrameworkCore;

namespace AzPin.Windows.Tests.Tests;

public class TokenCacheTests
{
    [Fact]
    public async Task GetTokenAsync_ReturnsCachedToken_WhenTokenIsStillValid()
    {
        await using var db = CreateDb();
        db.CachedTokens.Add(new CachedToken
        {
            SubscriptionId = "sub1",
            TenantId = "tenant",
            AccessToken = "cached-token",
            ExpiresOn = DateTime.UtcNow.AddMinutes(30)
        });
        await db.SaveChangesAsync();

        var az = new FakeAzCliService();
        var cache = new TokenCache(db, az);

        var token = await cache.GetTokenAsync("sub1", "tenant");

        Assert.Equal("cached-token", token);
        Assert.Equal(0, az.GetTokenCalls);
    }

    [Fact]
    public async Task GetTokenAsync_Refreshes_WhenTokenIsExpired()
    {
        await using var db = CreateDb();
        db.CachedTokens.Add(new CachedToken
        {
            SubscriptionId = "sub1",
            TenantId = "tenant",
            AccessToken = "old-token",
            ExpiresOn = DateTime.UtcNow.AddMinutes(1)
        });
        await db.SaveChangesAsync();

        var az = new FakeAzCliService
        {
            TokenToReturn = new AzPin.Windows.Models.AzTokenResponse("new-token", "2024-01-15 14:30:00.000000", "tenant", "sub1", DateTime.UtcNow.AddHours(2))
        };
        var cache = new TokenCache(db, az);

        var token = await cache.GetTokenAsync("sub1", "tenant");
        var updated = await db.CachedTokens.SingleAsync(t => t.SubscriptionId == "sub1");

        Assert.Equal("new-token", token);
        Assert.Equal(1, az.GetTokenCalls);
        Assert.Equal("new-token", updated.AccessToken);
    }

    [Fact]
    public async Task GetTokenAsync_Refreshes_WhenTokenIsMissing()
    {
        await using var db = CreateDb();
        var az = new FakeAzCliService
        {
            TokenToReturn = new AzPin.Windows.Models.AzTokenResponse("fresh-token", "2024-01-15 14:30:00.000000", "tenant", "sub2", DateTime.UtcNow.AddHours(2))
        };

        var cache = new TokenCache(db, az);
        var token = await cache.GetTokenAsync("sub2", "tenant");

        Assert.Equal("fresh-token", token);
        Assert.Equal(1, az.GetTokenCalls);
        Assert.NotNull(await db.CachedTokens.SingleOrDefaultAsync(t => t.SubscriptionId == "sub2"));
    }

    private static AzPinDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AzPinDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AzPinDbContext(options);
    }
}
