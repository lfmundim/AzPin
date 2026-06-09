using AzPin.Windows.MainWindow.Pages;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;

namespace AzPin.Windows.MainWindow;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Title = "AzPin";
        ExtendsContentIntoTitleBar = true;
        SystemBackdrop = new MicaBackdrop();
        AppWindow.Resize(new SizeInt32(960, 640));
        AppWindow.IsShownInSwitchers = false;

        AppWindow.Closing += (_, args) =>
        {
            args.Cancel = true;
            AppWindow.IsShownInSwitchers = false;
            AppWindow.Hide();
        };
    }

    public void InitializeContent()
    {
        ContentFrame.Navigate(typeof(BrowsePage));
    }

    private void OnNavSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
            return;

        if (args.SelectedItem is NavigationViewItem { Tag: "Browse" })
            ContentFrame.Navigate(typeof(BrowsePage));
    }
}
