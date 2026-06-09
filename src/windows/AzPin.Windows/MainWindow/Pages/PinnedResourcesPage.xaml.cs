using AzPin.Windows.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

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
        // Persist display order after drag
        _ = ViewModel.PersistOrderAsync();
    }
}
