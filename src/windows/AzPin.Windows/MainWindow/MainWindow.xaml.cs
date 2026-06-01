using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;

namespace AzPin.Windows.MainWindow;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
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
