using AzPin.Windows.Models;
using AzPin.Windows.Models.Arm;
using AzPin.Windows.Models.Entities;
using AzPin.Windows.Tests.Mocks;
using AzPin.Windows.ViewModels;

namespace AzPin.Windows.Tests.Tests.ViewModels;

public class TrayRgViewModelTests
{
    private static PinnedResourceGroup MakeRg(string sub = "s", string name = "rg") =>
        new() { SubscriptionId = sub, Name = name, Id = $"/subscriptions/{sub}/resourceGroups/{name}" };

    private static ArmResource WebSite(string n = "app") =>
        new($"/id/{n}", n, "Microsoft.Web/sites", "eastus");

    private static ArmResource Storage(string n = "st") =>
        new($"/id/{n}", n, "Microsoft.Storage/storageAccounts", "eastus");

    [Fact]
    public async Task LoadResourcesAsync_SetsHasErrorOnException()
    {
        var arm = new FakeArmService { ThrowOnFetchResources = new Exception("network error") };
        var vm = new TrayRgViewModel(MakeRg(), arm, new FakePinService());

        await vm.LoadResourcesAsync();

        Assert.True(vm.HasError);
        Assert.Equal("network error", vm.ErrorMessage);
        Assert.Empty(vm.Resources);
    }

    [Fact]
    public async Task LoadResourcesAsync_ClearsErrorOnSuccess()
    {
        var arm = new FakeArmService { Resources = [WebSite()] };
        var vm = new TrayRgViewModel(MakeRg(), arm, new FakePinService());
        vm.HasError = true;
        vm.ErrorMessage = "old error";

        await vm.LoadResourcesAsync();

        Assert.False(vm.HasError);
        Assert.Null(vm.ErrorMessage);
        Assert.Single(vm.Resources);
    }

    [Fact]
    public async Task FetchRunningStatesAsync_SetsFetchingThenFinalState()
    {
        var states = new List<AppRunningState>();
        var arm = new FakeArmService
        {
            Resources = [WebSite()],
            RunningState = AppRunningState.Running
        };
        var vm = new TrayRgViewModel(MakeRg(), arm, new FakePinService());
        await vm.LoadResourcesAsync();

        var rv = vm.Resources[0];
        rv.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(rv.RunningState))
                states.Add(rv.RunningState);
        };

        await vm.FetchRunningStatesAsync();

        // Should have transitioned through Fetching then to final
        Assert.Contains(AppRunningState.Fetching, states);
        Assert.Equal(AppRunningState.Running, rv.RunningState);
    }

    [Fact]
    public async Task FetchRunningStatesAsync_SkipsNonRunnableResources()
    {
        var arm = new FakeArmService { Resources = [Storage()] };
        var vm = new TrayRgViewModel(MakeRg(), arm, new FakePinService());
        await vm.LoadResourcesAsync();

        await vm.FetchRunningStatesAsync();

        Assert.Equal(AppRunningState.Unknown, vm.Resources[0].RunningState);
    }

    [Fact]
    public async Task CheckPermissionsAsync_SetsPermissionsOnRunnableResources()
    {
        var arm = new FakeArmService { Resources = [WebSite()] };
        var perms = new FakePermissionsService { Permissions = new(true, false, true) };
        var vm = new TrayRgViewModel(MakeRg(), arm, new FakePinService());
        await vm.LoadResourcesAsync();

        await vm.CheckPermissionsAsync(perms);

        var rv = vm.Resources[0];
        Assert.NotNull(rv.Permissions);
        Assert.True(rv.Permissions!.CanStart);
        Assert.False(rv.Permissions.CanStop);
    }
}
