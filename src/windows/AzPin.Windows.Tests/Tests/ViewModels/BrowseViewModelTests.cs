using AzPin.Windows.Models;
using AzPin.Windows.Models.Arm;
using AzPin.Windows.Tests.Mocks;
using AzPin.Windows.ViewModels;

namespace AzPin.Windows.Tests.Tests.ViewModels;

public class BrowseViewModelTests
{
    [Fact]
    public async Task LoadSubscriptionsAsync_PopulatesSubscriptions_AndSelectsDefault()
    {
        var az = new FakeAzCliService
        {
            Subscriptions = [
                new AzSubscription("s2", "Bravo", "t1", false),
                new AzSubscription("s1", "Alpha", "t1", true)
            ]
        };
        var vm = new BrowseViewModel(az, new FakeArmService(), new FakePinService(), new FakeSubscriptionSettingsService());

        await vm.LoadSubscriptionsAsync();

        Assert.Equal(2, vm.Subscriptions.Count);
        Assert.NotNull(vm.SelectedSubscription);
        Assert.Equal("s1", vm.SelectedSubscription!.Id);
    }

    [Fact]
    public async Task LoadSubscriptionsAsync_SetsErrorMessage_OnException()
    {
        var az = new FakeAzCliService
        {
            ThrowOnListSubscriptions = new InvalidOperationException("forced failure")
        };
        var vm = new BrowseViewModel(az, new FakeArmService(), new FakePinService(), new FakeSubscriptionSettingsService());

        await vm.LoadSubscriptionsAsync();

        Assert.NotNull(vm.ErrorMessage);
        Assert.Empty(vm.Subscriptions);
    }

    [Fact]
    public async Task LoadSubscriptionsAsync_TracksIsLoading()
    {
        var gate = new TaskCompletionSource();
        var az = new FakeAzCliService
        {
            Subscriptions = [new AzSubscription("s1", "Sub", "t1", true)],
            DelayBeforeList = gate.Task
        };
        var vm = new BrowseViewModel(az, new FakeArmService(), new FakePinService(), new FakeSubscriptionSettingsService());

        var task = vm.LoadSubscriptionsAsync();
        await Task.Delay(20);
        Assert.True(vm.IsLoadingSubscriptions);

        gate.SetResult();
        await task;
        Assert.False(vm.IsLoadingSubscriptions);
    }

    [Fact]
    public async Task LoadResourceGroupsAsync_PopulatesGroups_SortedAlphabetically()
    {
        var az = new FakeAzCliService
        {
            Subscriptions = [new AzSubscription("s1", "Alpha", "t1", true)]
        };
        var arm = new FakeArmService
        {
            ResourceGroups = [
                new ArmResourceGroup("id-z", "zeta-rg", "eastus"),
                new ArmResourceGroup("id-a", "alpha-rg", "westus")
            ]
        };
        var vm = new BrowseViewModel(az, arm, new FakePinService(), new FakeSubscriptionSettingsService());
        await vm.LoadSubscriptionsAsync();

        Assert.Equal(2, vm.ResourceGroups.Count);
        Assert.Equal("alpha-rg", vm.ResourceGroups[0].ResourceGroup.Name);
        Assert.Equal("zeta-rg", vm.ResourceGroups[1].ResourceGroup.Name);
    }

    [Fact]
    public async Task FilteredResourceGroups_ReturnsAll_WhenSearchEmpty()
    {
        var az = new FakeAzCliService
        {
            Subscriptions = [new AzSubscription("s1", "Sub", "t1", true)]
        };
        var arm = new FakeArmService
        {
            ResourceGroups = [new ArmResourceGroup("id1", "rg-one", "eus"), new ArmResourceGroup("id2", "rg-two", "wus")]
        };
        var vm = new BrowseViewModel(az, arm, new FakePinService(), new FakeSubscriptionSettingsService());
        await vm.LoadSubscriptionsAsync();

        Assert.Equal(2, vm.FilteredResourceGroups.Count());
    }

    [Fact]
    public async Task FilteredResourceGroups_FiltersCorrectly_CaseInsensitive()
    {
        var az = new FakeAzCliService
        {
            Subscriptions = [new AzSubscription("s1", "Sub", "t1", true)]
        };
        var arm = new FakeArmService
        {
            ResourceGroups = [
                new ArmResourceGroup("id1", "Production-RG", "eus"),
                new ArmResourceGroup("id2", "dev-resources", "wus")
            ]
        };
        var vm = new BrowseViewModel(az, arm, new FakePinService(), new FakeSubscriptionSettingsService());
        await vm.LoadSubscriptionsAsync();

        vm.SearchText = "PROD";
        var results = vm.FilteredResourceGroups.ToList();

        Assert.Single(results);
        Assert.Equal("Production-RG", results[0].ResourceGroup.Name);
    }

    [Fact]
    public async Task FilteredResourceGroups_ReturnsEmpty_WhenNoMatch()
    {
        var az = new FakeAzCliService
        {
            Subscriptions = [new AzSubscription("s1", "Sub", "t1", true)]
        };
        var arm = new FakeArmService
        {
            ResourceGroups = [new ArmResourceGroup("id1", "my-rg", "eus")]
        };
        var vm = new BrowseViewModel(az, arm, new FakePinService(), new FakeSubscriptionSettingsService());
        await vm.LoadSubscriptionsAsync();

        vm.SearchText = "zzznomatch";

        Assert.Empty(vm.FilteredResourceGroups);
    }
}
