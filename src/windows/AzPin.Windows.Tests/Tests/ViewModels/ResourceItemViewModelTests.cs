using AzPin.Windows.Models.Arm;
using AzPin.Windows.Tests.Mocks;
using AzPin.Windows.ViewModels;

namespace AzPin.Windows.Tests.Tests.ViewModels;

public class ResourceItemViewModelTests
{
    private static ArmResource MakeResource(string type = "microsoft.web/sites") =>
        new("/subscriptions/s/resourceGroups/rg/providers/Microsoft.Web/sites/myapp", "myapp", type, "eastus");

    private static ResourceItemViewModel MakeVm(ArmResource? resource = null, FakePinService? pin = null) =>
        new(resource ?? MakeResource(), "sub1", "my-rg", pin ?? new FakePinService());

    [Fact]
    public async Task TogglePinAsync_Pins_WhenNotPinned()
    {
        var pin = new FakePinService();
        var vm = MakeVm(pin: pin);
        Assert.False(vm.IsPinned);

        await vm.TogglePinAsync();

        Assert.True(vm.IsPinned);
        Assert.Equal(1, pin.PinResourceCalls);
    }

    [Fact]
    public async Task TogglePinAsync_Unpins_WhenPinned()
    {
        var resource = MakeResource();
        var pin = new FakePinService();
        await pin.PinResourceAsync(resource, "sub1", "my-rg", 0);

        var vm = MakeVm(resource: resource, pin: pin);
        await vm.InitializeAsync();
        Assert.True(vm.IsPinned);

        await vm.TogglePinAsync();

        Assert.False(vm.IsPinned);
        Assert.Equal(1, pin.UnpinResourceCalls);
    }

    [Fact]
    public async Task InitializeAsync_SetsPinnedState_FromService()
    {
        var resource = MakeResource();
        var pin = new FakePinService();
        await pin.PinResourceAsync(resource, "sub1", "my-rg", 0);

        var vm = MakeVm(resource: resource, pin: pin);
        await vm.InitializeAsync();

        Assert.True(vm.IsPinned);
    }

    [Fact]
    public void ShowPinButton_ReturnsFalse_WhenIsRgPinnedTrue()
    {
        var vm = MakeVm();
        vm.IsRgPinned = true;

        Assert.False(vm.ShowPinButton);
    }

    [Fact]
    public void GlyphCode_ReturnsCorrectGlyph_ForKnownType()
    {
        var vm = MakeVm(MakeResource("microsoft.web/sites"));
        Assert.False(string.IsNullOrEmpty(vm.GlyphCode));
    }

    [Fact]
    public void PortalUri_ContainsResourceId()
    {
        var resource = MakeResource();
        var vm = MakeVm(resource);

        Assert.Contains("myapp", vm.PortalUri.ToString());
    }
}
