namespace AzPin.Windows.Services;

public enum UpdateCheckState
{
    Idle,
    Checking,
    UpToDate,
    UpdateAvailable,
    Failed
}

public record UpdateCheckResult(
    UpdateCheckState State,
    string CurrentVersion,
    string? LatestVersion = null,
    string? ReleaseUrl = null,
    string? ErrorMessage = null);

public interface IUpdateCheckService
{
    Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken ct = default);
}
