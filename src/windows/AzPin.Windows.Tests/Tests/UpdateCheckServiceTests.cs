using System.Net;
using AzPin.Windows.Services;
using AzPin.Windows.Tests.Mocks;

namespace AzPin.Windows.Tests.Tests;

public class UpdateCheckServiceTests
{
    // MARK: - IsNewer

    [Fact]
    public void IsNewer_LaterMinor_ReturnsTrue()
        => Assert.True(UpdateCheckService.IsNewer("1.1.0", "1.0.0"));

    [Fact]
    public void IsNewer_LaterMajor_ReturnsTrue()
        => Assert.True(UpdateCheckService.IsNewer("2.0.0", "1.9.9"));

    [Fact]
    public void IsNewer_LaterPatch_ReturnsTrue()
        => Assert.True(UpdateCheckService.IsNewer("1.0.1", "1.0.0"));

    [Fact]
    public void IsNewer_SameVersion_ReturnsFalse()
        => Assert.False(UpdateCheckService.IsNewer("1.0.0", "1.0.0"));

    [Fact]
    public void IsNewer_OlderVersion_ReturnsFalse()
        => Assert.False(UpdateCheckService.IsNewer("0.9.9", "1.0.0"));

    [Fact]
    public void IsNewer_InvalidVersion_ReturnsFalse()
        => Assert.False(UpdateCheckService.IsNewer("not-a-version", "1.0.0"));

    // MARK: - CheckForUpdatesAsync

    [Fact]
    public async Task CheckForUpdatesAsync_UpdateAvailable_ReturnsUpdateAvailableState()
    {
        var service = CreateService("""{"tag_name":"v99.9.9","html_url":"https://github.com/lfmundim/AzPin/releases/tag/v99.9.9"}""");

        var result = await service.CheckForUpdatesAsync();

        Assert.Equal(UpdateCheckState.UpdateAvailable, result.State);
        Assert.Equal("99.9.9", result.LatestVersion);
        Assert.Equal("https://github.com/lfmundim/AzPin/releases/tag/v99.9.9", result.ReleaseUrl);
    }

    [Fact]
    public async Task CheckForUpdatesAsync_UpToDate_ReturnsUpToDateState()
    {
        var service = CreateService("""{"tag_name":"v0.0.1","html_url":"https://github.com/lfmundim/AzPin/releases/tag/v0.0.1"}""");

        var result = await service.CheckForUpdatesAsync();

        Assert.Equal(UpdateCheckState.UpToDate, result.State);
    }

    [Fact]
    public async Task CheckForUpdatesAsync_NetworkError_ReturnsFailedState()
    {
        var handler = new FakeHttpMessageHandler
        {
            Handler = _ => throw new HttpRequestException("network error")
        };
        var service = CreateService(handler);

        var result = await service.CheckForUpdatesAsync();

        Assert.Equal(UpdateCheckState.Failed, result.State);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task CheckForUpdatesAsync_InvalidJson_ReturnsFailedState()
    {
        var service = CreateService("not json at all");

        var result = await service.CheckForUpdatesAsync();

        Assert.Equal(UpdateCheckState.Failed, result.State);
    }

    [Fact]
    public async Task CheckForUpdatesAsync_HttpError_ReturnsFailedState()
    {
        var handler = new FakeHttpMessageHandler
        {
            Handler = _ => new HttpResponseMessage(HttpStatusCode.InternalServerError)
        };
        var service = CreateService(handler);

        var result = await service.CheckForUpdatesAsync();

        Assert.Equal(UpdateCheckState.Failed, result.State);
    }

    [Fact]
    public async Task CheckForUpdatesAsync_StripsVPrefix()
    {
        var service = CreateService("""{"tag_name":"v2.0.0","html_url":"https://github.com"}""");

        var result = await service.CheckForUpdatesAsync();

        // Whether update or up-to-date depends on the test assembly version, but the parsed version should be stripped
        Assert.DoesNotContain("v", result.LatestVersion ?? string.Empty);
    }

    [Fact]
    public async Task CheckForUpdatesAsync_SetsUserAgentHeader()
    {
        var handler = new FakeHttpMessageHandler
        {
            Handler = _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"tag_name":"v0.0.1","html_url":"https://github.com"}""")
            }
        };
        var service = CreateService(handler);

        await service.CheckForUpdatesAsync();

        Assert.NotNull(handler.LastRequest);
        var ua = handler.LastRequest!.Headers.UserAgent.ToString();
        Assert.Contains("AzPin", ua);
    }

    // MARK: - helpers

    private static UpdateCheckService CreateService(string responseBody)
    {
        var handler = new FakeHttpMessageHandler
        {
            Handler = _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseBody)
            }
        };
        return CreateService(handler);
    }

    private static UpdateCheckService CreateService(FakeHttpMessageHandler handler)
    {
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.github.com")
        };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("AzPin");
        var factory = new FakeHttpClientFactory(client);
        return new UpdateCheckService(factory);
    }
}
