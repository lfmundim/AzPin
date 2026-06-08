using AzPin.Windows.Data;
using AzPin.Windows.Models.Arm;
using AzPin.Windows.Services;
using Microsoft.EntityFrameworkCore;

namespace AzPin.Windows.Tests.Tests;

public class PinServiceTests
{
    private static IDbContextFactory<AzPinDbContext> CreateFactory()
    {
        var options = new DbContextOptionsBuilder<AzPinDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new TestDbContextFactory(options);
    }

    [Fact]
    public async Task PinResourceAsync_InsertsRecord_IsResourcePinnedReturnsTrue()
    {
        var svc = new PinService(CreateFactory());
        var resource = new ArmResource("/sub/rg/res1", "res1", "microsoft.web/sites", "eastus");

        await svc.PinResourceAsync(resource, "sub1", "rg1", 0);

        Assert.True(await svc.IsResourcePinnedAsync("/sub/rg/res1"));
    }

    [Fact]
    public async Task PinResourceAsync_IsDuplicateNoOp_SecondCallDoesNotInsertTwice()
    {
        var svc = new PinService(CreateFactory());
        var resource = new ArmResource("/sub/rg/res1", "res1", "microsoft.web/sites", "eastus");

        await svc.PinResourceAsync(resource, "sub1", "rg1", 0);
        await svc.PinResourceAsync(resource, "sub1", "rg1", 1);

        var pinned = await svc.GetPinnedResourcesAsync();
        Assert.Single(pinned);
    }

    [Fact]
    public async Task UnpinResourceAsync_DeletesRecord_IsResourcePinnedReturnsFalse()
    {
        var svc = new PinService(CreateFactory());
        var resource = new ArmResource("/sub/rg/res1", "res1", "microsoft.web/sites", "eastus");
        await svc.PinResourceAsync(resource, "sub1", "rg1", 0);

        await svc.UnpinResourceAsync("/sub/rg/res1");

        Assert.False(await svc.IsResourcePinnedAsync("/sub/rg/res1"));
    }

    [Fact]
    public async Task GetPinnedResourcesAsync_ReturnsSortedByDisplayOrder()
    {
        var svc = new PinService(CreateFactory());
        await svc.PinResourceAsync(new ArmResource("/id2", "second", "t", "l"), "s", "rg", displayOrder: 2);
        await svc.PinResourceAsync(new ArmResource("/id1", "first",  "t", "l"), "s", "rg", displayOrder: 1);

        var result = await svc.GetPinnedResourcesAsync();

        Assert.Equal("/id1", result[0].Id);
        Assert.Equal("/id2", result[1].Id);
    }

    private sealed class TestDbContextFactory(DbContextOptions<AzPinDbContext> options)
        : IDbContextFactory<AzPinDbContext>
    {
        public AzPinDbContext CreateDbContext() => new(options);
    }
}
