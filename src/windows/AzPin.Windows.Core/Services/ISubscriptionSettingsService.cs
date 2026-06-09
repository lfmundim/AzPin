namespace AzPin.Windows.Services;

public interface ISubscriptionSettingsService
{
    Task<IReadOnlySet<string>> GetHiddenSubscriptionIdsAsync(CancellationToken ct = default);
    Task SetVisibilityAsync(string subscriptionId, bool visible, CancellationToken ct = default);
}
