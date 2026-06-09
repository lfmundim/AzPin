using AzPin.Windows.Services;

namespace AzPin.Windows.Tests.Mocks;

internal sealed class FakeSubscriptionSettingsService : ISubscriptionSettingsService
{
    private readonly HashSet<string> _hidden = new(StringComparer.OrdinalIgnoreCase);

    public Task<IReadOnlySet<string>> GetHiddenSubscriptionIdsAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlySet<string>>(_hidden);

    public Task SetVisibilityAsync(string subscriptionId, bool visible, CancellationToken ct = default)
    {
        if (visible) _hidden.Remove(subscriptionId);
        else _hidden.Add(subscriptionId);
        return Task.CompletedTask;
    }

    public void Hide(string subscriptionId) => _hidden.Add(subscriptionId);
}
