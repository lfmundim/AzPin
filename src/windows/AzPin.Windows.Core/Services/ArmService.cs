using System.Net.Http.Headers;
using System.Text.Json;
using AzPin.Windows.Models;
using AzPin.Windows.Models.Arm;

namespace AzPin.Windows.Services;

public class ArmException(string message, int statusCode, string body) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
    public string Body { get; } = body;
}

public class ArmService(ITokenCache tokenCache, IHttpClientFactory httpClientFactory) : IArmService
{
    private readonly ITokenCache _tokenCache = tokenCache;
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("arm");
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static readonly HashSet<string> RunnableTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "microsoft.web/sites",
        "microsoft.web/sites/slots",
        "microsoft.app/containerapps",
        "microsoft.logic/workflows"
    };

    public async Task<IReadOnlyList<ArmSubscription>> FetchSubscriptionsAsync(CancellationToken ct = default)
    {
        var response = await GetAsync<ArmListResponse<ArmSubscription>>("/subscriptions?api-version=2022-12-01", string.Empty, string.Empty, ct);
        return response.Value;
    }

    public async Task<IReadOnlyList<ArmResourceGroup>> FetchResourceGroupsAsync(string subscriptionId, string tenantId, CancellationToken ct = default)
    {
        var path = $"/subscriptions/{Uri.EscapeDataString(subscriptionId)}/resourcegroups?api-version=2021-04-01";
        var response = await GetAsync<ArmListResponse<ArmResourceGroup>>(path, subscriptionId, tenantId, ct);
        return response.Value;
    }

    public async Task<IReadOnlyList<ArmResource>> FetchResourcesAsync(string subscriptionId, string tenantId, string resourceGroupName, CancellationToken ct = default)
    {
        var path = $"/subscriptions/{Uri.EscapeDataString(subscriptionId)}/resourceGroups/{Uri.EscapeDataString(resourceGroupName)}/resources?api-version=2021-04-01";
        var response = await GetAsync<ArmListResponse<ArmResource>>(path, subscriptionId, tenantId, ct);
        return response.Value;
    }

    public async Task<AppRunningState> FetchRunningStateAsync(string subscriptionId, string tenantId, ArmResource resource, CancellationToken ct = default)
    {
        if (!RunnableTypes.Contains(resource.Type))
            return AppRunningState.Unknown;

        try
        {
            var (apiVersion, useRunningStatus, runningValue, stoppedValue) = StateConfig(resource.Type);
            var path = $"{resource.Id}?api-version={apiVersion}";
            var response = await GetAsync<ArmResourceStateResponse>(path, subscriptionId, tenantId, ct);

            var rawState = useRunningStatus
                ? response.Properties?.RunningStatus
                : response.Properties?.State;

            return rawState?.ToLowerInvariant() == runningValue
                ? AppRunningState.Running
                : rawState?.ToLowerInvariant() == stoppedValue
                    ? AppRunningState.Stopped
                    : AppRunningState.Unknown;
        }
        catch (ArmException)
        {
            return AppRunningState.Unknown;
        }
    }

    public Task StartResourceAsync(string subscriptionId, string tenantId, ArmResource resource, CancellationToken ct = default)
        => PostActionAsync(subscriptionId, tenantId, resource, "start", ct);

    public Task StopResourceAsync(string subscriptionId, string tenantId, ArmResource resource, CancellationToken ct = default)
        => PostActionAsync(subscriptionId, tenantId, resource, "stop", ct);

    public async Task RestartResourceAsync(string subscriptionId, string tenantId, ArmResource resource, CancellationToken ct = default)
    {
        bool needsStopStart = resource.Type.ToLowerInvariant() is
            "microsoft.app/containerapps" or "microsoft.logic/workflows";
        if (needsStopStart)
        {
            await PostActionAsync(subscriptionId, tenantId, resource, "stop", ct);
            await PostActionAsync(subscriptionId, tenantId, resource, "start", ct);
        }
        else
        {
            await PostActionAsync(subscriptionId, tenantId, resource, "restart", ct);
        }
    }

    // Returns (apiVersion, useRunningStatusField, runningValue, stoppedValue) all lowercased for comparison
    internal static (string ApiVersion, bool UseRunningStatus, string RunningValue, string StoppedValue) StateConfig(string resourceType) =>
        resourceType.ToLowerInvariant() switch
        {
            "microsoft.app/containerapps" => ("2023-05-01", true,  "running", "stopped"),
            "microsoft.logic/workflows"   => ("2019-05-01", false, "enabled", "disabled"),
            _                             => ("2023-01-01", false, "running", "stopped")
        };

    internal static string ApiVersion(string resourceType) => resourceType.ToLowerInvariant() switch
    {
        "microsoft.app/containerapps" => "2023-05-01",
        "microsoft.logic/workflows"   => "2019-05-01",
        _                             => "2023-01-01"
    };

    internal static string MapAction(string action, string resourceType) =>
        resourceType.ToLowerInvariant() == "microsoft.logic/workflows"
            ? action switch { "start" => "enable", "stop" => "disable", _ => action }
            : action;

    private async Task PostActionAsync(string subscriptionId, string tenantId, ArmResource resource, string action, CancellationToken ct)
    {
        var mappedAction = MapAction(action, resource.Type);
        var apiVersion = ApiVersion(resource.Type);
        var path = $"{resource.Id}/{mappedAction}?api-version={apiVersion}";
        using var request = await CreateRequestAsync(HttpMethod.Post, path, subscriptionId, tenantId, ct);
        request.Content = new StringContent(string.Empty);

        using var response = await _httpClient.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
            throw new ArmException($"ARM {action} failed for {resource.Id}", (int)response.StatusCode, body);
    }

    private async Task<T> GetAsync<T>(string path, string subscriptionId, string tenantId, CancellationToken ct)
    {
        using var request = await CreateRequestAsync(HttpMethod.Get, path, subscriptionId, tenantId, ct);
        using var response = await _httpClient.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new ArmException($"ARM GET failed for {path}", (int)response.StatusCode, body);

        var value = JsonSerializer.Deserialize<T>(body, JsonOptions);
        return value ?? throw new ArmException($"Failed to deserialize ARM response for {path}", (int)response.StatusCode, body);
    }

    private async Task<HttpRequestMessage> CreateRequestAsync(HttpMethod method, string path, string subscriptionId, string tenantId, CancellationToken ct)
    {
        var token = string.IsNullOrWhiteSpace(subscriptionId)
            ? string.Empty
            : await _tokenCache.GetTokenAsync(subscriptionId, tenantId, ct);

        var request = new HttpRequestMessage(method, path);
        if (!string.IsNullOrEmpty(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return request;
    }
}
