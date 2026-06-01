using AzPin.Windows.Services;
using AzPin.Windows.Tests.Mocks;
using AzPin.Windows.Utilities;

namespace AzPin.Windows.Tests.Tests;

public class AzCliServiceTests
{
    [Fact]
    public void ResolveAzPath_Returns_NonEmptyPath()
    {
        var path = AzCliService.ResolveAzPath();
        Assert.False(string.IsNullOrWhiteSpace(path));
    }

    [Fact]
    public async Task GetCurrentAccountAsync_ReturnsNull_OnShellFailure()
    {
        var shell = new FakeShellRunner
        {
            Handler = (_, _) => new ShellResult { ExitCode = 1, Stderr = "not signed in" }
        };
        var service = new AzCliService(shell);

        var account = await service.GetCurrentAccountAsync();

        Assert.Null(account);
    }

    [Fact]
    public async Task GetAccessTokenAsync_ParsesExpiresOn_ToUtc()
    {
        var json = """
        {
          "accessToken": "token",
          "expiresOn": "2024-01-15 14:30:00.000000",
          "tenantId": "tenant",
          "subscription": "sub"
        }
        """;

        var shell = new FakeShellRunner
        {
            Handler = (_, _) => new ShellResult { ExitCode = 0, Stdout = json }
        };

        var service = new AzCliService(shell);
        var token = await service.GetAccessTokenAsync("sub");

        Assert.Equal("2024-01-15 14:30:00.000000", token.ExpiresOn);
        Assert.Equal(DateTimeKind.Utc, token.ExpiresOnUtc.Kind);
    }
}
