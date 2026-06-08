namespace AzPin.Windows.Services;

public interface ITokenCache
{
    Task<string> GetTokenAsync(string subscriptionId, string tenantId, CancellationToken ct = default);
}
