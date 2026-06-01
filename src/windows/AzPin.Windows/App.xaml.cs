using AzPin.Windows.TrayIcon;
using H.NotifyIcon;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using LaunchActivatedEventArgs = Microsoft.UI.Xaml.LaunchActivatedEventArgs;

namespace AzPin.Windows;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    private TaskbarIcon? _trayIcon;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        Services = ConfigureServices();
        _trayIcon = Services.GetRequiredService<TaskbarIcon>();
        _trayIcon.ForceCreate();
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<TaskbarIcon>(_ => TrayIconFactory.Create());
        return services.BuildServiceProvider();
    }
}
