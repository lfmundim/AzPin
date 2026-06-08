using AzPin.Windows.Data;
using AzPin.Windows.Services;
using AzPin.Windows.TrayIcon;
using AzPin.Windows.Utilities;
using AzPin.Windows.ViewModels;
using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Xaml;
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

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        Services = ConfigureServices();
        await using (var scope = Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AzPinDbContext>();
            await db.Database.EnsureCreatedAsync();
        }
        _trayIcon = Services.GetRequiredService<TaskbarIcon>();
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();
        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AzPin", "azpin.db");
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

        services.AddDbContext<AzPinDbContext>(options =>
            options.UseSqlite($"Data Source={dbPath}"));
        services.AddSingleton<IShellRunner, ShellRunner>();
        services.AddSingleton<IAzCliService, AzCliService>();
        services.AddSingleton<AuthViewModel>();
        services.AddSingleton<TrayMenuViewModel>();
        services.AddScoped<ITokenCache, TokenCache>();
        services.AddHttpClient("arm", c => c.BaseAddress = new Uri("https://management.azure.com"));
        services.AddScoped<IArmService, ArmService>();
        services.AddSingleton<TaskbarIcon>(_ => TrayIconFactory.Create());
        return services.BuildServiceProvider();
    }
}
