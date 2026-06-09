using System.Net;
using System.Net.Http.Headers;
using AzPin.Windows.Models.Arm;
using AzPin.Windows.Services;
using AzPin.Windows.Tests.Mocks;

namespace AzPin.Windows.Tests.Tests;

public class ArmServiceTests
{
    [Fact]
    public async Task FetchSubscriptionsAsync_DeserializesResponse()
    {
        var handler = new FakeHttpMessageHandler
        {
            Handler = _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"value\":[{\"subscriptionId\":\"sub1\",\"displayName\":\"Sub One\",\"tenantId\":\"t1\"}]}")
            }
        };
        var service = CreateService(handler);

        var result = await service.FetchSubscriptionsAsync();

        Assert.Single(result);
        Assert.Equal("sub1", result[0].SubscriptionId);
    }

    [Fact]
    public async Task FetchResourceGroupsAsync_SetsAuthorizationHeader()
    {
        var handler = new FakeHttpMessageHandler
        {
            Handler = _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"value\":[]}")
            }
        };
        var service = CreateService(handler, "abc-token");

        await service.FetchResourceGroupsAsync("sub1", "tenant");

        Assert.NotNull(handler.LastRequest);
        Assert.Equal(new AuthenticationHeaderValue("Bearer", "abc-token"), handler.LastRequest!.Headers.Authorization);
    }

    [Fact]
    public async Task FetchResourceGroupsAsync_ThrowsArmException_OnHttpError()
    {
        var handler = new FakeHttpMessageHandler
        {
            Handler = _ => new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("bad request")
            }
        };
        var service = CreateService(handler);

        var ex = await Assert.ThrowsAsync<ArmException>(() => service.FetchResourceGroupsAsync("sub1", "tenant"));

        Assert.Equal(400, ex.StatusCode);
        Assert.Contains("bad request", ex.Body);
    }

    [Fact]
    public async Task FetchRunningStateAsync_ReturnsUnknown_ForNonRunnableType_WithoutHttpCall()
    {
        var handler = new FakeHttpMessageHandler();
        var service = CreateService(handler);
        var resource = new ArmResource("/subscriptions/sub/resourceGroups/rg/providers/Microsoft.Storage/storageAccounts/abc", "abc", "Microsoft.Storage/storageAccounts", "westeurope");

        var state = await service.FetchRunningStateAsync("sub", "tenant", resource);

        Assert.Equal(AzPin.Windows.Models.AppRunningState.Unknown, state);
        Assert.Equal(0, handler.Calls);
    }

    private static ArmService CreateService(FakeHttpMessageHandler handler, string token = "token")
    {
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://management.azure.com")
        };

        var tokenCache = new FakeTokenCache { Token = token };
        var factory = new FakeHttpClientFactory(client);
        return new ArmService(tokenCache, factory);
    }
}
