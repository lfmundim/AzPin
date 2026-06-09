using System.Net;
using AzPin.Windows.Services;
using AzPin.Windows.Tests.Mocks;

namespace AzPin.Windows.Tests.Tests;

public class PermissionsServiceTests
{
    private static PermissionsService Create(FakeHttpMessageHandler handler, string token = "token")
    {
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://management.azure.com") };
        var cache = new FakeTokenCache { Token = token };
        var factory = new FakeHttpClientFactory(client);
        return new PermissionsService(cache, factory);
    }

    private static string WildcardResponse(params string[] notActions)
    {
        var notActionsJson = string.Join(",", notActions.Select(a => $"\"{a}\""));
        return $$"""{"value":[{"actions":["*"],"notActions":[{{notActionsJson}}]}]}""";
    }

    private static string ExplicitActionsResponse(string[] actions, string[] notActions)
    {
        var actionsJson = string.Join(",", actions.Select(a => $"\"{a}\""));
        var notActionsJson = string.Join(",", notActions.Select(a => $"\"{a}\""));
        return $$"""{"value":[{"actions":[{{actionsJson}}],"notActions":[{{notActionsJson}}]}]}""";
    }

    [Fact]
    public async Task WildcardWithNoNotActions_GrantsAll()
    {
        var handler = new FakeHttpMessageHandler
        {
            Handler = _ => new HttpResponseMessage(HttpStatusCode.OK)
                { Content = new StringContent(WildcardResponse()) }
        };
        var svc = Create(handler);

        var result = await svc.CheckAccessAsync("sub", "tenant", "/subscriptions/sub/rg/r/providers/t/n", "microsoft.web/sites");

        Assert.True(result.CanStart);
        Assert.True(result.CanStop);
        Assert.True(result.CanRestart);
    }

    [Fact]
    public async Task WildcardWithStartInNotActions_DeniesStart()
    {
        var handler = new FakeHttpMessageHandler
        {
            Handler = _ => new HttpResponseMessage(HttpStatusCode.OK)
                { Content = new StringContent(WildcardResponse("Microsoft.Web/sites/start/action")) }
        };
        var svc = Create(handler);

        var result = await svc.CheckAccessAsync("sub", "tenant", "/subscriptions/sub/rg/r/providers/t/n", "microsoft.web/sites");

        Assert.False(result.CanStart);
        Assert.True(result.CanStop);
    }

    [Fact]
    public async Task ExplicitActions_GrantsSpecificActions()
    {
        var handler = new FakeHttpMessageHandler
        {
            Handler = _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(ExplicitActionsResponse(
                    ["Microsoft.Web/sites/start/action", "Microsoft.Web/sites/stop/action", "Microsoft.Web/sites/write"],
                    []))
            }
        };
        var svc = Create(handler);

        var result = await svc.CheckAccessAsync("sub", "tenant", "/id", "microsoft.web/sites");

        Assert.True(result.CanStart);
        Assert.True(result.CanStop);
        Assert.True(result.CanRestart);
    }

    [Fact]
    public async Task EmptyValueArray_DeniesAll()
    {
        var handler = new FakeHttpMessageHandler
        {
            Handler = _ => new HttpResponseMessage(HttpStatusCode.OK)
                { Content = new StringContent("""{"value":[]}""") }
        };
        var svc = Create(handler);

        var result = await svc.CheckAccessAsync("sub", "tenant", "/id", "microsoft.web/sites");

        Assert.Equal(ResourcePermissions.None, result);
    }

    [Fact]
    public async Task Http403_ReturnsNone()
    {
        var handler = new FakeHttpMessageHandler
        {
            Handler = _ => new HttpResponseMessage(HttpStatusCode.Forbidden)
                { Content = new StringContent("forbidden") }
        };
        var svc = Create(handler);

        var result = await svc.CheckAccessAsync("sub", "tenant", "/id", "microsoft.web/sites");

        Assert.Equal(ResourcePermissions.None, result);
    }

    [Fact]
    public async Task NetworkError_ReturnsNone_DoesNotThrow()
    {
        var handler = new FakeHttpMessageHandler
        {
            Handler = _ => throw new HttpRequestException("network error")
        };
        var svc = Create(handler);

        var result = await svc.CheckAccessAsync("sub", "tenant", "/id", "microsoft.web/sites");

        Assert.Equal(ResourcePermissions.None, result);
    }

    [Fact]
    public async Task Caches_SecondCallNoHttpRequest()
    {
        var callCount = 0;
        var handler = new FakeHttpMessageHandler
        {
            Handler = _ =>
            {
                callCount++;
                return new HttpResponseMessage(HttpStatusCode.OK)
                    { Content = new StringContent(WildcardResponse()) };
            }
        };
        var svc = Create(handler);

        await svc.CheckAccessAsync("sub", "tenant", "/id", "microsoft.web/sites");
        await svc.CheckAccessAsync("sub", "tenant", "/id", "microsoft.web/sites");

        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task ActionMatchingIsCaseInsensitive()
    {
        var handler = new FakeHttpMessageHandler
        {
            Handler = _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(ExplicitActionsResponse(
                    ["MICROSOFT.WEB/SITES/START/ACTION"],
                    []))
            }
        };
        var svc = Create(handler);

        var result = await svc.CheckAccessAsync("sub", "tenant", "/id", "microsoft.web/sites");

        Assert.True(result.CanStart);
    }
}
