using AzPin.Windows.Services;

namespace AzPin.Windows.Tests.Mocks;

internal sealed class FakeUpdateCheckService : IUpdateCheckService
{
    public UpdateCheckResult Result { get; set; } =
        new UpdateCheckResult(UpdateCheckState.UpToDate, "1.0.0", "1.0.0");

    public Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken ct = default)
        => Task.FromResult(Result);
}
