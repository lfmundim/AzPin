using AzPin.Windows.Models;
using AzPin.Windows.Services;

namespace AzPin.Windows.Tests.Mocks;

internal sealed class FakeAzCliService : IAzCliService
{
    public string AzPath { get; init; } = "az.cmd";
    public bool IsCliInstalled { get; init; } = true;
    public int GetTokenCalls { get; private set; }
    public AzTokenResponse TokenToReturn { get; set; } = new("token", "2024-01-15 14:30:00.000000", "tenant", "sub", DateTime.UtcNow.AddHours(1));

    public Task<AzAccount?> GetCurrentAccountAsync(CancellationToken ct = default) => Task.FromResult<AzAccount?>(null);

    public Task<IReadOnlyList<AzSubscription>> ListSubscriptionsAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<AzSubscription>>(Array.Empty<AzSubscription>());

    public Task<AzTokenResponse> GetAccessTokenAsync(string subscriptionId, CancellationToken ct = default)
    {
        GetTokenCalls++;
        return Task.FromResult(TokenToReturn with { Subscription = subscriptionId });
    }
}
