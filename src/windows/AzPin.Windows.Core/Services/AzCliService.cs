using System.Globalization;
using System.Text.Json;
using AzPin.Windows.Models;
using AzPin.Windows.Utilities;

namespace AzPin.Windows.Services;

public class AzCliService(IShellRunner shellRunner) : IAzCliService
{
    private readonly IShellRunner _shellRunner = shellRunner;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public string AzPath { get; } = ResolveAzPath();

    public bool IsCliInstalled => !string.Equals(AzPath, "az.cmd", StringComparison.OrdinalIgnoreCase);

    public async Task<AzAccount?> GetCurrentAccountAsync(CancellationToken ct = default)
    {
        try
        {
            var output = await _shellRunner.RunAndReadAsync(AzPath, "account show --output json", ct);
            return JsonSerializer.Deserialize<AzAccount>(output, JsonOptions);
        }
        catch (ShellException)
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<AzSubscription>> ListSubscriptionsAsync(CancellationToken ct = default)
    {
        if (!IsCliInstalled)
        {
            return Array.Empty<AzSubscription>();
        }

        try
        {
            var output = await _shellRunner.RunAndReadAsync(AzPath, "account list --output json", ct);
            var subscriptions = JsonSerializer.Deserialize<List<AzSubscription>>(output, JsonOptions) ?? [];

            return subscriptions
                .OrderByDescending(s => s.IsDefault)
                .ThenBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
        catch (ShellException)
        {
            return Array.Empty<AzSubscription>();
        }
    }

    public async Task<AzTokenResponse> GetAccessTokenAsync(string subscriptionId, CancellationToken ct = default)
    {
        var output = await _shellRunner.RunAndReadAsync(AzPath, $"account get-access-token --subscription {subscriptionId} --output json", ct);
        var raw = JsonSerializer.Deserialize<AzTokenResponseRaw>(output, JsonOptions)
                  ?? throw new InvalidOperationException("Failed to parse az account get-access-token output.");

        var expiresOnUtc = DateTime.ParseExact(
            raw.ExpiresOn,
            "yyyy-MM-dd HH:mm:ss.ffffff",
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeLocal).ToUniversalTime();

        return new AzTokenResponse(raw.AccessToken, raw.ExpiresOn, raw.TenantId, raw.Subscription, expiresOnUtc);
    }

    public static string ResolveAzPath()
    {
        // Check well-known MSI install locations first.
        var candidate1 = @"C:\Program Files (x86)\Microsoft SDKs\Azure\CLI2\wbin\az.cmd";
        if (File.Exists(candidate1)) return candidate1;

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var candidate2 = Path.Combine(localAppData, "Programs", "Microsoft SDKs", "Azure", "CLI2", "wbin", "az.cmd");
        if (File.Exists(candidate2)) return candidate2;

        // Fall back to PATH-based lookup (covers winget, Chocolatey, custom installs).
        try
        {
            using var proc = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "where",
                Arguments = "az.cmd",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            });
            var line = proc?.StandardOutput.ReadLine()?.Trim();
            proc?.WaitForExit(3000);
            if (!string.IsNullOrEmpty(line) && File.Exists(line))
                return line;
        }
        catch
        {
            // 'where' not available or az not on PATH — fall through.
        }

        return "az.cmd";
    }

    private sealed record AzTokenResponseRaw(string AccessToken, string ExpiresOn, string TenantId, string Subscription);
}
