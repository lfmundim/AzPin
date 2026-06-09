using AzPin.Windows.Models.Arm;
using AzPin.Windows.Models.Entities;
using AzPin.Windows.Tests.Mocks;
using AzPin.Windows.ViewModels;

namespace AzPin.Windows.Tests.Tests.ViewModels;

public class MainWindowViewModelTests
{
    private static PinnedResourceGroup MakeRg(string sub, string name, int order = 0) =>
        new() { SubscriptionId = sub, Name = name, DisplayOrder = order, Id = $"/subscriptions/{sub}/resourceGroups/{name}" };

    [Fact]
    public async Task LoadPinnedRgsAsync_SortsByDisplayOrder()
    {
        var pins = new FakePinService();
        await pins.PinResourceGroupAsync(new ArmResourceGroup("/id/b", "rg-b", ""), "s", displayOrder: 2);
        await pins.PinResourceGroupAsync(new ArmResourceGroup("/id/a", "rg-a", ""), "s", displayOrder: 1);
        await pins.PinResourceGroupAsync(new ArmResourceGroup("/id/c", "rg-c", ""), "s", displayOrder: 0);
        var vm = new MainWindowViewModel(pins);

        await vm.LoadPinnedRgsAsync();

        Assert.Equal(3, vm.PinnedRgs.Count);
        Assert.Equal("rg-c", vm.PinnedRgs[0].Name);
        Assert.Equal("rg-a", vm.PinnedRgs[1].Name);
        Assert.Equal("rg-b", vm.PinnedRgs[2].Name);
    }

    [Fact]
    public async Task LoadPinnedRgsAsync_AppendSubSuffix_WhenDuplicateNames()
    {
        var pins = new FakePinService();
        await pins.PinResourceGroupAsync(new ArmResourceGroup("/id/1", "shared-rg", ""), "sub-aaa", displayOrder: 0);
        await pins.PinResourceGroupAsync(new ArmResourceGroup("/id/2", "shared-rg", ""), "sub-bbb", displayOrder: 1);
        var vm = new MainWindowViewModel(pins);

        await vm.LoadPinnedRgsAsync();

        Assert.All(vm.PinnedRgs, item => Assert.Contains("sub-", item.DisplayName));
    }

    [Fact]
    public async Task LoadPinnedRgsAsync_NoSubSuffix_WhenNamesAreUnique()
    {
        var pins = new FakePinService();
        await pins.PinResourceGroupAsync(new ArmResourceGroup("/id/a", "rg-unique", ""), "s", displayOrder: 0);
        var vm = new MainWindowViewModel(pins);

        await vm.LoadPinnedRgsAsync();

        Assert.Equal("rg-unique", vm.PinnedRgs[0].DisplayName);
    }

    [Fact]
    public async Task UnpinRgAsync_RemovesFromCollection()
    {
        var pins = new FakePinService();
        await pins.PinResourceGroupAsync(new ArmResourceGroup("/id/a", "rg-a", ""), "s", displayOrder: 0);
        var vm = new MainWindowViewModel(pins);
        await vm.LoadPinnedRgsAsync();
        var item = vm.PinnedRgs[0];

        await vm.UnpinRgAsync(item);

        Assert.Empty(vm.PinnedRgs);
    }

    [Fact]
    public async Task UnpinRgAsync_ClearsSelectedRg_WhenItIsRemoved()
    {
        var pins = new FakePinService();
        await pins.PinResourceGroupAsync(new ArmResourceGroup("/id/a", "rg-a", ""), "s", displayOrder: 0);
        var vm = new MainWindowViewModel(pins);
        await vm.LoadPinnedRgsAsync();
        vm.SelectedRg = vm.PinnedRgs[0];

        await vm.UnpinRgAsync(vm.PinnedRgs[0]);

        Assert.Null(vm.SelectedRg);
    }

    [Fact]
    public async Task UnpinRgAsync_PreservesSelectedRg_WhenDifferentItemRemoved()
    {
        var pins = new FakePinService();
        await pins.PinResourceGroupAsync(new ArmResourceGroup("/id/a", "rg-a", ""), "s", displayOrder: 0);
        await pins.PinResourceGroupAsync(new ArmResourceGroup("/id/b", "rg-b", ""), "s", displayOrder: 1);
        var vm = new MainWindowViewModel(pins);
        await vm.LoadPinnedRgsAsync();
        var keepSelected = vm.PinnedRgs[0];
        vm.SelectedRg = keepSelected;

        await vm.UnpinRgAsync(vm.PinnedRgs[1]);

        Assert.Equal(keepSelected, vm.SelectedRg);
    }
}
