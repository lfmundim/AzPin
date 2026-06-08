using AzPin.Windows.Models;
using AzPin.Windows.ViewModels;
using AzPin.Windows.Tests.Mocks;

namespace AzPin.Windows.Tests.Tests.ViewModels;

public class AuthViewModelTests
{
    [Fact]
    public async Task RefreshAsync_SetsCliNotInstalled_WhenCliUnavailable()
    {
        var az = new FakeAzCliService { IsCliInstalled = false };
        var vm = new AuthViewModel(az);

        await vm.RefreshAsync();

        Assert.Equal(AuthState.CliNotInstalled, vm.State);
        Assert.Null(vm.AccountName);
    }

    [Fact]
    public async Task RefreshAsync_SetsNotSignedIn_WhenNoAccount()
    {
        var az = new FakeAzCliService
        {
            IsCliInstalled = true,
            CurrentAccount = null
        };
        var vm = new AuthViewModel(az);

        await vm.RefreshAsync();

        Assert.Equal(AuthState.NotSignedIn, vm.State);
        Assert.Null(vm.AccountName);
    }

    [Fact]
    public async Task RefreshAsync_SetsSignedIn_AndPopulatesAccount_WhenAccountPresent()
    {
        var az = new FakeAzCliService
        {
            IsCliInstalled = true,
            CurrentAccount = new AzAccount(new AzAccountUser("operator@unit.test"), "tenant-1", "Unit Tenant"),
            Subscriptions =
            [
                new AzSubscription("s-2", "Bravo", "tenant-1", false),
                new AzSubscription("s-1", "Alpha", "tenant-1", true)
            ]
        };
        var vm = new AuthViewModel(az);

        await vm.RefreshAsync();

        Assert.Equal(AuthState.SignedIn, vm.State);
        Assert.Equal("operator@unit.test", vm.AccountName);
        Assert.Equal("tenant-1", vm.TenantId);
        Assert.Equal("Alpha", vm.ActiveSubscriptionName);
    }

    [Fact]
    public async Task RefreshAsync_TracksIsRefreshing_DuringAndAfterOperation()
    {
        var gate = new TaskCompletionSource();
        var az = new FakeAzCliService
        {
            IsCliInstalled = true,
            DelayBeforeAccount = gate.Task,
            CurrentAccount = new AzAccount(new AzAccountUser("operator@unit.test"), "tenant-1", "Unit Tenant")
        };

        var vm = new AuthViewModel(az);
        var refreshTask = vm.RefreshAsync();

        await Task.Delay(20);
        Assert.True(vm.IsRefreshing);

        gate.SetResult();
        await refreshTask;

        Assert.False(vm.IsRefreshing);
    }
}
