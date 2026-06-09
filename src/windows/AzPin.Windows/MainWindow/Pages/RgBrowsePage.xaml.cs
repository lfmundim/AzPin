using AzPin.Windows.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Windows.ApplicationModel.DataTransfer;

namespace AzPin.Windows.MainWindow.Pages;

public sealed partial class RgBrowsePage : Page
{
    public RgBrowseViewModel ViewModel { get; private set; } = null!;

    public RgBrowsePage()
    {
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is RgBrowseViewModel vm)
        {
            ViewModel = vm;
            Bindings.Update();
            await vm.LoadAsync();
        }
    }

    private void OnResourceRightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: string resourceId }) return;
        ShowCopyFlyout((FrameworkElement)sender, e.GetPosition((FrameworkElement)sender), resourceId);
        e.Handled = true;
    }

    private static void ShowCopyFlyout(FrameworkElement anchor, global::Windows.Foundation.Point position, string resourceId)
    {
        var item = new MenuFlyoutItem { Text = "Copy Resource ID" };
        item.Click += (_, _) =>
        {
            var dp = new DataPackage();
            dp.SetText(resourceId);
            Clipboard.SetContent(dp);
        };
        var flyout = new MenuFlyout();
        flyout.Items.Add(item);
        flyout.ShowAt(anchor, new FlyoutShowOptions { Position = position });
    }
}
