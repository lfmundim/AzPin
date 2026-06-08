using AzPin.Windows.Models.Arm;
using AzPin.Windows.Tests.Mocks;
using AzPin.Windows.ViewModels;

namespace AzPin.Windows.Tests.Tests.ViewModels;

public class ResourceGroupItemViewModelTests
{
    private static ResourceGroupItemViewModel MakeVm(FakeArmService? arm = null, FakePinService? pin = null) =>
        new(
            new ArmResourceGroup("rg-id", "my-rg", "eastus"),
            "sub1", "tenant1",
            arm ?? new FakeArmService(),
            pin ?? new FakePinService());

    [Fact]
    public async Task ToggleAsync_SetsIsExpanded_And_LoadsResources()
    {
        var arm = new FakeArmService
        {
            Resources = [new ArmResource("/sub/rg/res1", "res1", "microsoft.web/sites", "eastus")]
        };
        var vm = MakeVm(arm: arm);

        await vm.ToggleAsync();

        Assert.True(vm.IsExpanded);
        Assert.Single(vm.Resources);
    }

    [Fact]
    public async Task ToggleAsync_Collapses_WithoutReload_WhenAlreadyLoaded()
    {
        var arm = new FakeArmService
        {
            Resources = [new ArmResource("/sub/rg/res1", "res1", "microsoft.web/sites", "eastus")]
        };
        var vm = MakeVm(arm: arm);

        await vm.ToggleAsync(); // expand + load
        int callCount = 1;      // first expand loads resources
        arm.Resources = [];     // clear — if reload happens, Resources would become empty
        await vm.ToggleAsync(); // collapse — should NOT reload

        Assert.False(vm.IsExpanded);
        Assert.Single(vm.Resources); // still has cached resources
    }

    [Fact]
    public async Task LoadResourcesAsync_SortsResourcesByType()
    {
        var arm = new FakeArmService
        {
            Resources = [
                new ArmResource("/id2", "b", "Microsoft.Storage/storageAccounts", "eastus"),
                new ArmResource("/id1", "a", "microsoft.app/containerapps", "eastus")
            ]
        };
        var vm = MakeVm(arm: arm);

        await vm.LoadResourcesAsync();

        Assert.Equal("microsoft.app/containerapps", vm.Resources[0].Type);
        Assert.Equal("Microsoft.Storage/storageAccounts", vm.Resources[1].Type);
    }

    [Fact]
    public async Task LoadResourcesAsync_SetsResourceError_OnArmFailure()
    {
        var arm = new FakeArmService
        {
            ThrowOnFetchResources = new InvalidOperationException("ARM down")
        };
        var vm = MakeVm(arm: arm);

        await vm.LoadResourcesAsync();

        Assert.NotNull(vm.ResourceError);
        Assert.Empty(vm.Resources);
    }

    [Fact]
    public void PortalUri_ContainsSubscriptionAndRgName()
    {
        var vm = MakeVm();
        var uri = vm.PortalUri.ToString();

        Assert.Contains("sub1", uri);
        Assert.Contains("my-rg", uri);
    }
}
