using AzPin.Windows.Models.Arm;
using AzPin.Windows.Tests.Mocks;
using AzPin.Windows.TrayIcon;
using AzPin.Windows.ViewModels;

namespace AzPin.Windows.Tests.Tests.ViewModels;

public class TrayMenuViewModelTests
{
    private static (TrayMenuViewModel vm, FakePinService pins) Make(FakeAzCliService? az = null)
    {
        az ??= new FakeAzCliService();
        var auth = new AuthViewModel(az);
        var pins = new FakePinService();
        var arm = new FakeArmService();
        var perms = new FakePermissionsService();
        var vm = new TrayMenuViewModel(auth, pins, arm, perms, new FakeUpdateCheckService(), quit: static () => { }, openMainWindow: static () => { });
        return (vm, pins);
    }

    [Fact]
    public async Task LoadPinnedResourcesAsync_PopulatesSortedByDisplayOrder()
    {
        var (vm, pins) = Make();
        await pins.PinResourceAsync(
            new ArmResource("/sub/s/rg/r/providers/Microsoft.Web/sites/app-b", "app-b", "Microsoft.Web/sites", "eastus"),
            "s", "r", displayOrder: 2);
        await pins.PinResourceAsync(
            new ArmResource("/sub/s/rg/r/providers/Microsoft.Web/sites/app-a", "app-a", "Microsoft.Web/sites", "eastus"),
            "s", "r", displayOrder: 1);

        await vm.OnMenuOpenedAsync();

        Assert.Equal(2, vm.PinnedResources.Count);
        Assert.Equal("app-a", vm.PinnedResources[0].Name);
        Assert.Equal("app-b", vm.PinnedResources[1].Name);
    }

    [Fact]
    public async Task LoadPinnedResourcesAsync_EmptyList_WhenNoPins()
    {
        var (vm, _) = Make();

        await vm.OnMenuOpenedAsync();

        Assert.Empty(vm.PinnedResources);
    }

    [Fact]
    public async Task IsLoadingPinnedResources_TrueWhileLoading_FalseAfter()
    {
        var gate = new TaskCompletionSource();
        var auth = new AuthViewModel(new FakeAzCliService());
        var pins = new FakePinService { DelayBeforeGet = gate.Task };
        var perms = new FakePermissionsService();
        var vm = new TrayMenuViewModel(auth, pins, new FakeArmService(), perms, new FakeUpdateCheckService(), quit: static () => { }, openMainWindow: static () => { });

        var task = vm.OnMenuOpenedAsync();
        await Task.Delay(20);
        Assert.True(vm.IsLoadingPinnedResources);

        gate.SetResult();
        await task;
        Assert.False(vm.IsLoadingPinnedResources);
    }

    [Fact]
    public async Task PinnedResourceItemViewModel_GlyphCode_MapsKnownType()
    {
        var (vm, pins) = Make();
        await pins.PinResourceAsync(
            new ArmResource("/sub/s/rg/r/providers/Microsoft.Web/sites/myapp", "myapp", "Microsoft.Web/sites", "eastus"),
            "s", "r", displayOrder: 0);

        await vm.OnMenuOpenedAsync();

        var item = vm.PinnedResources[0];
        Assert.NotNull(item.GlyphCode);
        Assert.NotEmpty(item.GlyphCode);
    }

    [Fact]
    public async Task PinnedResourceItemViewModel_PortalUri_ContainsResourceId()
    {
        var (vm, pins) = Make();
        const string resourceId = "/subscriptions/sub1/resourceGroups/rg1/providers/Microsoft.Web/sites/myapp";
        await pins.PinResourceAsync(
            new ArmResource(resourceId, "myapp", "Microsoft.Web/sites", "eastus"),
            "sub1", "rg1", displayOrder: 0);

        await vm.OnMenuOpenedAsync();

        var uri = vm.PinnedResources[0].PortalUri.ToString();
        Assert.Contains(resourceId, uri);
        Assert.StartsWith("https://portal.azure.com", uri);
    }

    [Fact]
    public async Task PinnedResources_ExcludesResourcesWhoseParentRgIsPinned()
    {
        var (vm, pins) = Make();
        // Pin an individual resource under "sub-a / rg-shared"
        await pins.PinResourceAsync(
            new ArmResource("/id/app1", "app1", "Microsoft.Web/sites", "eastus"),
            "sub-a", "rg-shared", displayOrder: 0);
        // Also pin the parent RG — resource should be deduped out of the flat list
        await pins.PinResourceGroupAsync(new ArmResourceGroup("/id/rg", "rg-shared", "eastus"), "sub-a", displayOrder: 0);

        await vm.OnMenuOpenedAsync();

        Assert.Empty(vm.PinnedResources);
        Assert.Single(vm.PinnedRgItems);
    }

    [Fact]
    public async Task PinnedResources_IncludesResourcesWhoseParentRgIsNotPinned()
    {
        var (vm, pins) = Make();
        await pins.PinResourceAsync(
            new ArmResource("/id/app1", "app1", "Microsoft.Web/sites", "eastus"),
            "sub-a", "rg-not-pinned", displayOrder: 0);
        // Pin a different RG — should not affect the flat resource
        await pins.PinResourceGroupAsync(new ArmResourceGroup("/id/rg2", "rg-other", "eastus"), "sub-a", displayOrder: 0);

        await vm.OnMenuOpenedAsync();

        Assert.Single(vm.PinnedResources);
    }

    [Fact]
    public async Task PinnedRgItems_DisplayLabel_HasSubSuffix_WhenNamesCollide()
    {
        var (vm, pins) = Make();
        await pins.PinResourceGroupAsync(new ArmResourceGroup("/id/1", "shared-rg", "eastus"), "sub-aaaa1111", displayOrder: 0);
        await pins.PinResourceGroupAsync(new ArmResourceGroup("/id/2", "shared-rg", "eastus"), "sub-bbbb2222", displayOrder: 1);

        await vm.OnMenuOpenedAsync();

        Assert.Equal(2, vm.PinnedRgItems.Count);
        Assert.All(vm.PinnedRgItems, item => Assert.StartsWith("shared-rg · ", item.DisplayLabel));
        // Each shows a distinct subscription prefix
        Assert.NotEqual(vm.PinnedRgItems[0].DisplayLabel, vm.PinnedRgItems[1].DisplayLabel);
    }

    [Fact]
    public async Task PinnedRgItems_DisplayLabel_NoSuffix_WhenNamesAreUnique()
    {
        var (vm, pins) = Make();
        await pins.PinResourceGroupAsync(new ArmResourceGroup("/id/1", "rg-unique", "eastus"), "sub-a", displayOrder: 0);

        await vm.OnMenuOpenedAsync();

        Assert.Equal("rg-unique", vm.PinnedRgItems[0].DisplayLabel);
    }

    [Fact]
    public void QuitCommand_InvokesQuitAction()
    {
        var auth = new AuthViewModel(new FakeAzCliService());
        var pins = new FakePinService();
        var quitCalled = false;
        var vm = new TrayMenuViewModel(auth, pins, new FakeArmService(), new FakePermissionsService(), new FakeUpdateCheckService(), quit: () => quitCalled = true, openMainWindow: static () => { });

        vm.QuitCommand.Execute(null);

        Assert.True(quitCalled);
    }

    [Fact]
    public void OpenMainWindowCommand_InvokesOpenAction()
    {
        var auth = new AuthViewModel(new FakeAzCliService());
        var pins = new FakePinService();
        var openCalled = false;
        var vm = new TrayMenuViewModel(auth, pins, new FakeArmService(), new FakePermissionsService(), new FakeUpdateCheckService(), quit: static () => { }, openMainWindow: () => openCalled = true);

        vm.OpenMainWindowCommand.Execute(null);

        Assert.True(openCalled);
    }
}
