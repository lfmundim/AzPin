using System.Net;
using AzPin.Windows.Models;
using AzPin.Windows.Models.Arm;
using AzPin.Windows.Services;
using AzPin.Windows.Tests.Mocks;

namespace AzPin.Windows.Tests.Tests;

public class ArmServiceTypeAwareTests
{
    private static ArmService Create(FakeHttpMessageHandler handler, string token = "token")
    {
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://management.azure.com") };
        var cache = new FakeTokenCache { Token = token };
        var factory = new FakeHttpClientFactory(client);
        return new ArmService(cache, factory);
    }

    [Theory]
    [InlineData("microsoft.web/sites", "2023-01-01")]
    [InlineData("microsoft.web/sites/slots", "2023-01-01")]
    [InlineData("microsoft.app/containerapps", "2023-05-01")]
    [InlineData("microsoft.logic/workflows", "2019-05-01")]
    public void ApiVersion_ReturnsCorrectVersionByType(string type, string expected)
    {
        Assert.Equal(expected, ArmService.ApiVersion(type));
    }

    [Theory]
    [InlineData("microsoft.logic/workflows", "start", "enable")]
    [InlineData("microsoft.logic/workflows", "stop", "disable")]
    [InlineData("microsoft.logic/workflows", "restart", "restart")]
    [InlineData("microsoft.web/sites", "start", "start")]
    [InlineData("microsoft.web/sites", "stop", "stop")]
    public void MapAction_MapsCorrectlyByType(string type, string action, string expected)
    {
        Assert.Equal(expected, ArmService.MapAction(action, type));
    }

    [Fact]
    public async Task FetchRunningStateAsync_AppService_ReadsPropertiesState_Running()
    {
        string? capturedPath = null;
        var handler = new FakeHttpMessageHandler
        {
            Handler = req =>
            {
                capturedPath = req.RequestUri?.ToString();
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""{"properties":{"state":"Running"}}""")
                };
            }
        };
        var svc = Create(handler);
        var resource = new ArmResource("/subscriptions/s/resourceGroups/rg/providers/Microsoft.Web/sites/app", "app", "Microsoft.Web/sites", "eastus");

        var state = await svc.FetchRunningStateAsync("s", "t", resource);

        Assert.Equal(AppRunningState.Running, state);
        Assert.Contains("api-version=2023-01-01", capturedPath);
    }

    [Fact]
    public async Task FetchRunningStateAsync_ContainerApp_ReadsRunningStatus()
    {
        string? capturedPath = null;
        var handler = new FakeHttpMessageHandler
        {
            Handler = req =>
            {
                capturedPath = req.RequestUri?.ToString();
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""{"properties":{"runningStatus":"Running"}}""")
                };
            }
        };
        var svc = Create(handler);
        var resource = new ArmResource("/subscriptions/s/resourceGroups/rg/providers/Microsoft.App/containerApps/ca", "ca", "Microsoft.App/containerApps", "eastus");

        var state = await svc.FetchRunningStateAsync("s", "t", resource);

        Assert.Equal(AppRunningState.Running, state);
        Assert.Contains("api-version=2023-05-01", capturedPath);
    }

    [Fact]
    public async Task FetchRunningStateAsync_LogicApp_MapsEnabledToRunning()
    {
        string? capturedPath = null;
        var handler = new FakeHttpMessageHandler
        {
            Handler = req =>
            {
                capturedPath = req.RequestUri?.ToString();
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""{"properties":{"state":"Enabled"}}""")
                };
            }
        };
        var svc = Create(handler);
        var resource = new ArmResource("/subscriptions/s/resourceGroups/rg/providers/Microsoft.Logic/workflows/wf", "wf", "Microsoft.Logic/workflows", "eastus");

        var state = await svc.FetchRunningStateAsync("s", "t", resource);

        Assert.Equal(AppRunningState.Running, state);
        Assert.Contains("api-version=2019-05-01", capturedPath);
    }

    [Fact]
    public async Task FetchRunningStateAsync_LogicApp_MapsDisabledToStopped()
    {
        var handler = new FakeHttpMessageHandler
        {
            Handler = _ => new HttpResponseMessage(HttpStatusCode.OK)
                { Content = new StringContent("""{"properties":{"state":"Disabled"}}""") }
        };
        var svc = Create(handler);
        var resource = new ArmResource("/id", "wf", "Microsoft.Logic/workflows", "eastus");

        var state = await svc.FetchRunningStateAsync("s", "t", resource);

        Assert.Equal(AppRunningState.Stopped, state);
    }

    [Fact]
    public async Task FetchRunningStateAsync_OnArmError_ReturnsUnknown()
    {
        var handler = new FakeHttpMessageHandler
        {
            Handler = _ => new HttpResponseMessage(HttpStatusCode.InternalServerError)
                { Content = new StringContent("error") }
        };
        var svc = Create(handler);
        var resource = new ArmResource("/id", "app", "Microsoft.Web/sites", "eastus");

        var state = await svc.FetchRunningStateAsync("s", "t", resource);

        Assert.Equal(AppRunningState.Unknown, state);
    }
}
