using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AzPin.Windows.Services;

public class UpdateCheckService(IHttpClientFactory httpClientFactory) : IUpdateCheckService
{
    private readonly HttpClient _http = httpClientFactory.CreateClient("github");
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken ct = default)
    {
        var current = GetCurrentVersion();
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/repos/lfmundim/AzPin/releases/latest");
            using var response = await _http.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadAsStringAsync(ct);
            var release = JsonSerializer.Deserialize<GitHubRelease>(body, JsonOptions)
                ?? throw new InvalidOperationException("Empty response from GitHub API.");

            var latest = release.TagName.TrimStart('v', 'V');

            if (IsNewer(latest, current))
                return new UpdateCheckResult(UpdateCheckState.UpdateAvailable, current, latest, release.HtmlUrl);

            return new UpdateCheckResult(UpdateCheckState.UpToDate, current, latest);
        }
        catch (Exception ex)
        {
            return new UpdateCheckResult(UpdateCheckState.Failed, current, ErrorMessage: ex.Message);
        }
    }

    internal static bool IsNewer(string latest, string current)
    {
        if (!Version.TryParse(latest, out var l) || !Version.TryParse(current, out var c))
            return false;
        return l > c;
    }

    private static string GetCurrentVersion()
    {
        var v = Assembly.GetEntryAssembly()?.GetName().Version;
        if (v is null) return "0.0.0";
        return v.Build >= 0 ? $"{v.Major}.{v.Minor}.{v.Build}" : $"{v.Major}.{v.Minor}";
    }

    private sealed class GitHubRelease
    {
        [JsonPropertyName("tag_name")]
        public string TagName { get; init; } = string.Empty;

        [JsonPropertyName("html_url")]
        public string HtmlUrl { get; init; } = string.Empty;
    }
}
