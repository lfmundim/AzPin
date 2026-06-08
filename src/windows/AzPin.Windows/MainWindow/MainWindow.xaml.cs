using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Graphics;

namespace AzPin.Windows.MainWindow;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Title = "AzPin";
        ExtendsContentIntoTitleBar = true;
        AppWindow.Resize(new SizeInt32(900, 600));
        AppWindow.IsShownInSwitchers = false;
    }

    public void ShowMainWindow()
    {
        AppWindow.IsShownInSwitchers = true;
        Activate();
    }

    public void HideMainWindow()
    {
        AppWindow.IsShownInSwitchers = false;
    }
}
