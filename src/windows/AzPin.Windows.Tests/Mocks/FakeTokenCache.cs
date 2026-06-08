using AzPin.Windows.Services;

namespace AzPin.Windows.Tests.Mocks;

internal sealed class FakeTokenCache : ITokenCache
{
    public string Token { get; set; } = "test-token";
    public int Calls { get; private set; }

    public Task<string> GetTokenAsync(string subscriptionId, string tenantId, CancellationToken ct = default)
    {
        Calls++;
        return Task.FromResult(Token);
    }
}
