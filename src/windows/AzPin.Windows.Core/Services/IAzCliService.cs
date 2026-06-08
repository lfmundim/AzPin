using AzPin.Windows.Models;

namespace AzPin.Windows.Services;

public interface IAzCliService
{
    string AzPath { get; }
    bool IsCliInstalled { get; }
    Task<AzAccount?> GetCurrentAccountAsync(CancellationToken ct = default);
    Task<IReadOnlyList<AzSubscription>> ListSubscriptionsAsync(CancellationToken ct = default);
    Task<AzTokenResponse> GetAccessTokenAsync(string subscriptionId, CancellationToken ct = default);
}
