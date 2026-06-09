namespace AzPin.Windows.Services;

public record ResourcePermissions(bool CanStart, bool CanStop, bool CanRestart)
{
    public static readonly ResourcePermissions None = new(false, false, false);
}

public interface IPermissionsService
{
    Task<ResourcePermissions> CheckAccessAsync(
        string subscriptionId, string tenantId,
        string resourceId, string resourceType,
        CancellationToken ct = default);
}
