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
        var options = CreateOptions();
        await using (var db = new AzPinDbContext(options))
        {
            db.CachedTokens.Add(new CachedToken
            {
                SubscriptionId = "sub1",
                TenantId = "tenant",
                AccessToken = "cached-token",
                ExpiresOn = DateTime.UtcNow.AddMinutes(30)
            });
            await db.SaveChangesAsync();
        }

        var az = new FakeAzCliService();
        var cache = new TokenCache(new TestDbContextFactory(options), az);

        var token = await cache.GetTokenAsync("sub1", "tenant");

        Assert.Equal("cached-token", token);
        Assert.Equal(0, az.GetTokenCalls);
    }

    [Fact]
    public async Task GetTokenAsync_Refreshes_WhenTokenIsExpired()
    {
        var options = CreateOptions();
        await using (var db = new AzPinDbContext(options))
        {
            db.CachedTokens.Add(new CachedToken
            {
                SubscriptionId = "sub1",
                TenantId = "tenant",
                AccessToken = "old-token",
                ExpiresOn = DateTime.UtcNow.AddMinutes(1)
            });
            await db.SaveChangesAsync();
        }

        var az = new FakeAzCliService
        {
            TokenToReturn = new AzPin.Windows.Models.AzTokenResponse("new-token", "2024-01-15 14:30:00.000000", "tenant", "sub1", DateTime.UtcNow.AddHours(2))
        };
        var cache = new TokenCache(new TestDbContextFactory(options), az);

        var token = await cache.GetTokenAsync("sub1", "tenant");

        await using var verifyDb = new AzPinDbContext(options);
        var updated = await verifyDb.CachedTokens.SingleAsync(t => t.SubscriptionId == "sub1");

        Assert.Equal("new-token", token);
        Assert.Equal(1, az.GetTokenCalls);
        Assert.Equal("new-token", updated.AccessToken);
    }

    [Fact]
    public async Task GetTokenAsync_Refreshes_WhenTokenIsMissing()
    {
        var options = CreateOptions();
        var az = new FakeAzCliService
        {
            TokenToReturn = new AzPin.Windows.Models.AzTokenResponse("fresh-token", "2024-01-15 14:30:00.000000", "tenant", "sub2", DateTime.UtcNow.AddHours(2))
        };

        var cache = new TokenCache(new TestDbContextFactory(options), az);
        var token = await cache.GetTokenAsync("sub2", "tenant");

        await using var verifyDb = new AzPinDbContext(options);

        Assert.Equal("fresh-token", token);
        Assert.Equal(1, az.GetTokenCalls);
        Assert.NotNull(await verifyDb.CachedTokens.SingleOrDefaultAsync(t => t.SubscriptionId == "sub2"));
    }

    [Fact]
    public async Task GetTokenAsync_HandlesMissingTenantId_WhenAzCliReturnsNull()
    {
        var options = CreateOptions();
        var az = new FakeAzCliService
        {
            TokenToReturn = new AzPin.Windows.Models.AzTokenResponse("token-no-tenant", "2024-01-15 14:30:00.000000", null!, "sub3", DateTime.UtcNow.AddHours(2))
        };

        var cache = new TokenCache(new TestDbContextFactory(options), az);
        var token = await cache.GetTokenAsync("sub3", string.Empty);

        await using var verifyDb = new AzPinDbContext(options);
        var saved = await verifyDb.CachedTokens.SingleAsync(t => t.SubscriptionId == "sub3");

        Assert.Equal("token-no-tenant", token);
        Assert.Equal(string.Empty, saved.TenantId);
    }

    private static DbContextOptions<AzPinDbContext> CreateOptions() =>
        new DbContextOptionsBuilder<AzPinDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

    private sealed class TestDbContextFactory(DbContextOptions<AzPinDbContext> options) : IDbContextFactory<AzPinDbContext>
    {
        public AzPinDbContext CreateDbContext() => new(options);
        public Task<AzPinDbContext> CreateDbContextAsync(CancellationToken ct = default) =>
            Task.FromResult(new AzPinDbContext(options));
    }
}
