using AzPin.Windows.Models;
using AzPin.Windows.Services;

namespace AzPin.Windows.Tests.Mocks;

internal sealed class FakeAzCliService : IAzCliService
{
    public string AzPath { get; init; } = "az.cmd";
    public bool IsCliInstalled { get; set; } = true;
    public int GetTokenCalls { get; private set; }
    public AzTokenResponse TokenToReturn { get; set; } = new("token", "2024-01-15 14:30:00.000000", "tenant", "sub", DateTime.UtcNow.AddHours(1));
    public AzAccount? CurrentAccount { get; set; } = new(new AzAccountUser("test@example.com"), "tenant", "Tenant");
    public IReadOnlyList<AzSubscription> Subscriptions { get; set; } = Array.Empty<AzSubscription>();
    public Exception? ThrowOnListSubscriptions { get; set; }
    public Task DelayBeforeAccount { get; set; } = Task.CompletedTask;
    public Task DelayBeforeList { get; set; } = Task.CompletedTask;

    public async Task<AzAccount?> GetCurrentAccountAsync(CancellationToken ct = default)
    {
        await DelayBeforeAccount;
        return CurrentAccount;
    }

    public async Task<IReadOnlyList<AzSubscription>> ListSubscriptionsAsync(CancellationToken ct = default)
    {
        await DelayBeforeList;
        if (ThrowOnListSubscriptions is not null) throw ThrowOnListSubscriptions;
        return Subscriptions;
    }

    public Task<AzTokenResponse> GetAccessTokenAsync(string subscriptionId, CancellationToken ct = default)
    {
        GetTokenCalls++;
        return Task.FromResult(TokenToReturn with { Subscription = subscriptionId });
    }
}
