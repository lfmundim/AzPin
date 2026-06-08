using AzPin.Windows.Data;
using AzPin.Windows.MainWindow;
using AzPin.Windows.Services;
using AzPin.Windows.TrayIcon;
using AzPin.Windows.Utilities;
using AzPin.Windows.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using LaunchActivatedEventArgs = Microsoft.UI.Xaml.LaunchActivatedEventArgs;

namespace AzPin.Windows;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    private MainWindow.MainWindow? _mainWindow;

    public App()
    {
        InitializeComponent();
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        _mainWindow = new MainWindow.MainWindow();
        Services = ConfigureServices(_mainWindow);

        await using (var scope = Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AzPinDbContext>();
            await db.Database.EnsureCreatedAsync();
        }

        _mainWindow.InitializeTrayIcon(Services.GetRequiredService<TrayMenuViewModel>());
        _mainWindow.InitializeContent();
        // Window is created but NOT activated — tray-only until user clicks "Open AzPin..."
    }

    private static IServiceProvider ConfigureServices(MainWindow.MainWindow mainWindow)
    {
        var services = new ServiceCollection();

        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AzPin", "azpin.db");
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

        services.AddDbContextFactory<AzPinDbContext>(options =>
            options.UseSqlite($"Data Source={dbPath}"));

        services.AddSingleton<IShellRunner, ShellRunner>();
        services.AddSingleton<IAzCliService, AzCliService>();
        services.AddHttpClient("arm", c => c.BaseAddress = new Uri("https://management.azure.com"));
        services.AddScoped<ITokenCache, TokenCache>();
        services.AddScoped<IArmService, ArmService>();
        services.AddSingleton<IPinService, PinService>();

        services.AddSingleton<AuthViewModel>();
        services.AddSingleton<BrowseViewModel>();
        services.AddSingleton(sp => new TrayMenuViewModel(
            sp.GetRequiredService<AuthViewModel>(),
            sp.GetRequiredService<IPinService>(),
            quit: () => Current.Exit(),
            openMainWindow: () =>
            {
                mainWindow.AppWindow.IsShownInSwitchers = true;
                mainWindow.Activate();
            }));

        return services.BuildServiceProvider();
    }
}
