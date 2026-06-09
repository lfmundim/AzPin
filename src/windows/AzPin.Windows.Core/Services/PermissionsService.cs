using System.Net.Http.Headers;
using System.Text.Json;
using AzPin.Windows.Models.Arm;

namespace AzPin.Windows.Services;

public class PermissionsService(ITokenCache tokenCache, IHttpClientFactory httpClientFactory) : IPermissionsService
{
    private readonly ITokenCache _tokenCache = tokenCache;
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("arm");
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly Dictionary<string, ResourcePermissions> _cache = new(StringComparer.OrdinalIgnoreCase);

    public async Task<ResourcePermissions> CheckAccessAsync(
        string subscriptionId, string tenantId,
        string resourceId, string resourceType,
        CancellationToken ct = default)
    {
        if (_cache.TryGetValue(resourceId, out var cached))
            return cached;

        try
        {
            var path = $"{resourceId}/providers/Microsoft.Authorization/permissions?api-version=2022-04-01";
            var token = string.IsNullOrWhiteSpace(subscriptionId)
                ? string.Empty
                : await _tokenCache.GetTokenAsync(subscriptionId, tenantId, ct);

            using var request = new HttpRequestMessage(HttpMethod.Get, path);
            if (!string.IsNullOrEmpty(token))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await _httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
                return Cache(resourceId, ResourcePermissions.None);

            var body = await response.Content.ReadAsStringAsync(ct);
            var perms = JsonSerializer.Deserialize<ArmPermissionsResponse>(body, JsonOptions);
            if (perms is null)
                return Cache(resourceId, ResourcePermissions.None);

            var (startAction, stopAction, restartAction) = ActionStrings(resourceType);
            var result = new ResourcePermissions(
                CanStart:   IsGranted(perms.Value, startAction),
                CanStop:    IsGranted(perms.Value, stopAction),
                CanRestart: IsGranted(perms.Value, restartAction));

            return Cache(resourceId, result);
        }
        catch
        {
            return ResourcePermissions.None;
        }
    }

    private ResourcePermissions Cache(string resourceId, ResourcePermissions perms)
    {
        _cache[resourceId] = perms;
        return perms;
    }

    private static bool IsGranted(List<ArmPermissionsEntry> entries, string action)
    {
        foreach (var entry in entries)
        {
            bool granted = entry.Actions.Any(a =>
                string.Equals(a, "*", StringComparison.Ordinal) ||
                string.Equals(a, action, StringComparison.OrdinalIgnoreCase));
            if (!granted) continue;

            bool denied = entry.NotActions.Any(na =>
                string.Equals(na, action, StringComparison.OrdinalIgnoreCase));
            if (!denied) return true;
        }
        return false;
    }

    private static (string Start, string Stop, string Restart) ActionStrings(string resourceType) =>
        resourceType.ToLowerInvariant() switch
        {
            "microsoft.app/containerapps" =>
                ("Microsoft.App/containerApps/start/action",
                 "Microsoft.App/containerApps/stop/action",
                 "Microsoft.App/containerApps/write"),
            "microsoft.logic/workflows" =>
                ("Microsoft.Logic/workflows/write",
                 "Microsoft.Logic/workflows/write",
                 "Microsoft.Logic/workflows/write"),
            _ =>
                ("Microsoft.Web/sites/start/action",
                 "Microsoft.Web/sites/stop/action",
                 "Microsoft.Web/sites/write")
        };
}
