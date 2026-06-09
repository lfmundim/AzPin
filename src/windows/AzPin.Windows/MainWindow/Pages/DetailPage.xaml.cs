using AzPin.Windows.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace AzPin.Windows.MainWindow.Pages;

public sealed partial class DetailPage : Page
{
    private SidebarRgItemViewModel? _rg;

    public DetailPage()
    {
        InitializeComponent();
        TabBar.SelectedItem = PinnedTab;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is SidebarRgItemViewModel rg)
        {
            _rg = rg;
            RgNameText.Text = rg.DisplayName;
            NavigateToTab("Pinned");
        }
    }

    private void OnTabSelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
    {
        var tag = (sender.SelectedItem as SelectorBarItem)?.Tag as string;
        NavigateToTab(tag ?? "Pinned");
    }

    private void NavigateToTab(string tag)
    {
        if (_rg is null) return;
        if (tag == "Pinned")
        {
            var vm = App.Services.GetRequiredService<PinnedResourcesViewModel>();
            vm.Initialize(_rg.SubscriptionId, _rg.Name);
            TabFrame.Navigate(typeof(PinnedResourcesPage), vm);
        }
        else
        {
            var vm = App.Services.GetRequiredService<RgBrowseViewModel>();
            vm.Initialize(_rg.SubscriptionId, _rg.Name);
            TabFrame.Navigate(typeof(RgBrowsePage), vm);
        }
    }
}
