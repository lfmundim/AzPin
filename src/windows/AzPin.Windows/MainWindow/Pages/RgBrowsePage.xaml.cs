using AzPin.Windows.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

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
}
