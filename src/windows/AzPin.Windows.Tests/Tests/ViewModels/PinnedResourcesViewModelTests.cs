using AzPin.Windows.Models.Arm;
using AzPin.Windows.Tests.Mocks;
using AzPin.Windows.ViewModels;

namespace AzPin.Windows.Tests.Tests.ViewModels;

public class PinnedResourcesViewModelTests
{
    private static FakePinService PinsWithResources()
    {
        var pins = new FakePinService();
        // sub-a / rg-a
        pins.PinResourceAsync(
            new ArmResource("/id/app1", "app1", "Microsoft.Web/sites", "eastus"),
            "sub-a", "rg-a", displayOrder: 0).GetAwaiter().GetResult();
        pins.PinResourceAsync(
            new ArmResource("/id/app2", "app2", "Microsoft.Web/sites", "eastus"),
            "sub-a", "rg-a", displayOrder: 1).GetAwaiter().GetResult();
        // sub-a / rg-b (different RG)
        pins.PinResourceAsync(
            new ArmResource("/id/app3", "app3", "Microsoft.Web/sites", "eastus"),
            "sub-a", "rg-b", displayOrder: 0).GetAwaiter().GetResult();
        // sub-b / rg-a (different subscription)
        pins.PinResourceAsync(
            new ArmResource("/id/app4", "app4", "Microsoft.Web/sites", "eastus"),
            "sub-b", "rg-a", displayOrder: 0).GetAwaiter().GetResult();
        return pins;
    }

    [Fact]
    public async Task LoadAsync_FiltersToMatchingSubscriptionAndRg()
    {
        var vm = new PinnedResourcesViewModel(PinsWithResources());
        vm.Initialize("sub-a", "rg-a");

        await vm.LoadAsync();

        Assert.Equal(2, vm.Resources.Count);
        Assert.All(vm.Resources, r => Assert.True(r.Name is "app1" or "app2"));
    }

    [Fact]
    public async Task LoadAsync_FilterIsCaseInsensitiveForRgName()
    {
        var vm = new PinnedResourcesViewModel(PinsWithResources());
        vm.Initialize("sub-a", "RG-A");

        await vm.LoadAsync();

        Assert.Equal(2, vm.Resources.Count);
    }

    [Fact]
    public async Task LoadAsync_OrdersByDisplayOrder()
    {
        var vm = new PinnedResourcesViewModel(PinsWithResources());
        vm.Initialize("sub-a", "rg-a");

        await vm.LoadAsync();

        Assert.Equal("app1", vm.Resources[0].Name);
        Assert.Equal("app2", vm.Resources[1].Name);
    }

    [Fact]
    public async Task UnpinAsync_RemovesFromCollection()
    {
        var pins = PinsWithResources();
        var vm = new PinnedResourcesViewModel(pins);
        vm.Initialize("sub-a", "rg-a");
        await vm.LoadAsync();
        var item = vm.Resources[0];

        await vm.UnpinAsync(item);

        Assert.Single(vm.Resources);
        Assert.DoesNotContain(item, vm.Resources);
    }

    [Fact]
    public async Task UnpinAsync_CallsPinService()
    {
        var pins = PinsWithResources();
        var vm = new PinnedResourcesViewModel(pins);
        vm.Initialize("sub-a", "rg-a");
        await vm.LoadAsync();

        await vm.UnpinAsync(vm.Resources[0]);

        Assert.Equal(1, pins.UnpinResourceCalls);
    }
}
