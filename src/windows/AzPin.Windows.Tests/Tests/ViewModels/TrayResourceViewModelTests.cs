using AzPin.Windows.Models;
using AzPin.Windows.Models.Arm;
using AzPin.Windows.Services;
using AzPin.Windows.Tests.Mocks;
using AzPin.Windows.ViewModels;

namespace AzPin.Windows.Tests.Tests.ViewModels;

public class TrayResourceViewModelTests
{
    private static ArmResource WebSite(string name = "app") =>
        new($"/subscriptions/s/resourceGroups/rg/providers/Microsoft.Web/sites/{name}", name, "Microsoft.Web/sites", "eastus");

    private static ArmResource StorageAccount(string name = "st") =>
        new($"/subscriptions/s/resourceGroups/rg/providers/Microsoft.Storage/storageAccounts/{name}", name, "Microsoft.Storage/storageAccounts", "eastus");

    private static ResourcePermissions AllPermissions => new(true, true, true);

    [Fact]
    public void ShowStartButton_FalseWhenRunning_EvenWithPermissions()
    {
        var vm = new TrayResourceViewModel(WebSite(), "s", new FakeArmService());
        vm.RunningState = AppRunningState.Running;
        vm.Permissions = AllPermissions;

        Assert.False(vm.ShowStartButton);
    }

    [Fact]
    public void ShowStartButton_FalseWhenNoPermission()
    {
        var vm = new TrayResourceViewModel(WebSite(), "s", new FakeArmService());
        vm.RunningState = AppRunningState.Stopped;
        vm.Permissions = new ResourcePermissions(false, true, true);

        Assert.False(vm.ShowStartButton);
    }

    [Fact]
    public void ShowStartButton_TrueWhenStoppedAndPermitted()
    {
        var vm = new TrayResourceViewModel(WebSite(), "s", new FakeArmService());
        vm.RunningState = AppRunningState.Stopped;
        vm.Permissions = AllPermissions;

        Assert.True(vm.ShowStartButton);
    }

    [Fact]
    public void ShowStartButton_FalseForNonRunnableType()
    {
        var vm = new TrayResourceViewModel(StorageAccount(), "s", new FakeArmService());
        vm.RunningState = AppRunningState.Stopped;
        vm.Permissions = AllPermissions;

        Assert.False(vm.ShowStartButton);
    }

    [Fact]
    public void ShowStopButton_FalseWhenStopped_EvenWithPermissions()
    {
        var vm = new TrayResourceViewModel(WebSite(), "s", new FakeArmService());
        vm.RunningState = AppRunningState.Stopped;
        vm.Permissions = AllPermissions;

        Assert.False(vm.ShowStopButton);
    }

    [Fact]
    public void ShowStopButton_FalseWhenNoPermission()
    {
        var vm = new TrayResourceViewModel(WebSite(), "s", new FakeArmService());
        vm.RunningState = AppRunningState.Running;
        vm.Permissions = new ResourcePermissions(true, false, true);

        Assert.False(vm.ShowStopButton);
    }

    [Fact]
    public void ShowStopButton_TrueWhenRunningAndPermitted()
    {
        var vm = new TrayResourceViewModel(WebSite(), "s", new FakeArmService());
        vm.RunningState = AppRunningState.Running;
        vm.Permissions = AllPermissions;

        Assert.True(vm.ShowStopButton);
    }

    [Fact]
    public void ShowRestartButton_FalseWhenNoPermission()
    {
        var vm = new TrayResourceViewModel(WebSite(), "s", new FakeArmService());
        vm.RunningState = AppRunningState.Running;
        vm.Permissions = new ResourcePermissions(true, true, false);

        Assert.False(vm.ShowRestartButton);
    }

    [Fact]
    public void ShowRestartButton_TrueWhenRunningAndPermitted()
    {
        var vm = new TrayResourceViewModel(WebSite(), "s", new FakeArmService());
        vm.RunningState = AppRunningState.Running;
        vm.Permissions = AllPermissions;

        Assert.True(vm.ShowRestartButton);
    }

    [Theory]
    [InlineData(AppRunningState.Fetching)]
    [InlineData(AppRunningState.Starting)]
    [InlineData(AppRunningState.Stopping)]
    [InlineData(AppRunningState.Restarting)]
    public void IsTransitional_TrueForTransitionalStates(AppRunningState state)
    {
        var vm = new TrayResourceViewModel(WebSite(), "s", new FakeArmService());
        vm.RunningState = state;

        Assert.True(vm.IsTransitional);
    }

    [Theory]
    [InlineData(AppRunningState.Unknown)]
    [InlineData(AppRunningState.Running)]
    [InlineData(AppRunningState.Stopped)]
    public void IsTransitional_FalseForStableStates(AppRunningState state)
    {
        var vm = new TrayResourceViewModel(WebSite(), "s", new FakeArmService());
        vm.RunningState = state;

        Assert.False(vm.IsTransitional);
    }

    [Fact]
    public async Task StartAsync_RevertsStateOnError()
    {
        var arm = new FakeArmService { ThrowOnStart = new Exception("boom") };
        var vm = new TrayResourceViewModel(WebSite(), "s", arm);
        vm.RunningState = AppRunningState.Stopped;
        vm.Permissions = AllPermissions;

        await vm.StartAsync();

        Assert.Equal(AppRunningState.Stopped, vm.RunningState);
        Assert.NotNull(vm.ActionError);
    }

    [Fact]
    public async Task StopAsync_RevertsStateOnError()
    {
        var arm = new FakeArmService { ThrowOnStop = new Exception("boom") };
        var vm = new TrayResourceViewModel(WebSite(), "s", arm);
        vm.RunningState = AppRunningState.Running;
        vm.Permissions = AllPermissions;

        await vm.StopAsync();

        Assert.Equal(AppRunningState.Running, vm.RunningState);
        Assert.NotNull(vm.ActionError);
    }

    [Fact]
    public async Task RestartAsync_RevertsStateOnError()
    {
        var arm = new FakeArmService { ThrowOnRestart = new Exception("boom") };
        var vm = new TrayResourceViewModel(WebSite(), "s", arm);
        vm.RunningState = AppRunningState.Running;
        vm.Permissions = AllPermissions;

        await vm.RestartAsync();

        Assert.Equal(AppRunningState.Running, vm.RunningState);
        Assert.NotNull(vm.ActionError);
    }

    [Fact]
    public async Task StartAsync_SetsRunningOnSuccess()
    {
        var vm = new TrayResourceViewModel(WebSite(), "s", new FakeArmService());
        vm.RunningState = AppRunningState.Stopped;
        vm.Permissions = AllPermissions;

        await vm.StartAsync();

        Assert.Equal(AppRunningState.Running, vm.RunningState);
    }

    [Fact]
    public async Task StopAsync_SetsStoppedOnSuccess()
    {
        var vm = new TrayResourceViewModel(WebSite(), "s", new FakeArmService());
        vm.RunningState = AppRunningState.Running;
        vm.Permissions = AllPermissions;

        await vm.StopAsync();

        Assert.Equal(AppRunningState.Stopped, vm.RunningState);
    }
}
