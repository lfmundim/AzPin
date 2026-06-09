using AzPin.Windows.Services;

namespace AzPin.Windows.Tests.Mocks;

internal sealed class FakePermissionsService : IPermissionsService
{
    public ResourcePermissions Permissions { get; set; } = ResourcePermissions.None;
    public int CallCount { get; private set; }

    public Task<ResourcePermissions> CheckAccessAsync(
        string subscriptionId, string tenantId,
        string resourceId, string resourceType,
        CancellationToken ct = default)
    {
        CallCount++;
        return Task.FromResult(Permissions);
    }
}
