using AzPin.Windows.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Windows.ApplicationModel.DataTransfer;

namespace AzPin.Windows.MainWindow.Pages;

public sealed partial class PinnedResourcesPage : Page
{
    public PinnedResourcesViewModel ViewModel { get; private set; } = null!;

    public PinnedResourcesPage()
    {
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is PinnedResourcesViewModel vm)
        {
            ViewModel = vm;
            Bindings.Update();
            await vm.LoadAsync();
        }
    }

    private async void OnUnpinClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: PinnedResourceItemViewModel item })
            await ViewModel.UnpinAsync(item);
    }

    private void OnDragCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
    {
        _ = ViewModel.PersistOrderAsync();
    }

    private void OnResourceRightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: string resourceId }) return;
        var item = new MenuFlyoutItem { Text = "Copy Resource ID" };
        item.Click += (_, _) =>
        {
            var dp = new DataPackage();
            dp.SetText(resourceId);
            Clipboard.SetContent(dp);
        };
        var flyout = new MenuFlyout();
        flyout.Items.Add(item);
        flyout.ShowAt((FrameworkElement)sender, new FlyoutShowOptions { Position = e.GetPosition((FrameworkElement)sender) });
        e.Handled = true;
    }
}
