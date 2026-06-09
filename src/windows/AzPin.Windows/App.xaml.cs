using AzPin.Windows.Data;
using AzPin.Windows.MainWindow;
using AzPin.Windows.Services;
using AzPin.Windows.TrayIcon;
using AzPin.Windows.Utilities;
using AzPin.Windows.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Graphics;
using LaunchActivatedEventArgs = Microsoft.UI.Xaml.LaunchActivatedEventArgs;

namespace AzPin.Windows;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    private MainWindow.MainWindow? _mainWindow;

    public App()
    {
        InitializeComponent();
        UnhandledException += (_, e) => { e.Handled = true; LogAndExit(e.Exception); };
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            LogAndExit(e.ExceptionObject as Exception ?? new Exception(e.ExceptionObject?.ToString()));
        TaskScheduler.UnobservedTaskException += (_, e) => { e.SetObserved(); LogAndExit(e.Exception); };
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            _mainWindow = new MainWindow.MainWindow();
            Services = ConfigureServices(_mainWindow);

            await using (var scope = Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AzPinDbContext>();
                await db.Database.EnsureCreatedAsync();
                // Forward-compat: ensure HiddenSubscriptions table exists for DBs created
                // before this table was added (EnsureCreatedAsync won't add missing tables).
                await db.Database.ExecuteSqlRawAsync("""
                    CREATE TABLE IF NOT EXISTS HiddenSubscriptions (
                        SubscriptionId TEXT NOT NULL CONSTRAINT PK_HiddenSubscriptions PRIMARY KEY
                    )
                    """);
            }

            _mainWindow.InitializeContent();

            // Position off-screen before activating so the window is never visible on startup.
            // Activate() is required to fire FrameworkElement.Loaded so the TaskbarIcon registers
            // its tray icon with the shell. After that, the window can be hidden/shown freely.
            _mainWindow.AppWindow.Move(new PointInt32(-32000, -32000));
            _mainWindow.Activate();

            _mainWindow.InitializeTrayIcon(Services.GetRequiredService<TrayMenuViewModel>());

            _mainWindow.AppWindow.IsShownInSwitchers = false;
        }
        catch (Exception ex)
        {
            var logPath = Path.Combine(Path.GetTempPath(), "azpin-crash.log");
            try { File.WriteAllText(logPath, $"{DateTime.Now:O}\n{ex}"); } catch { }

            var xamlRoot = _mainWindow?.Content?.XamlRoot;
            if (xamlRoot is not null)
            {
                var dialog = new Microsoft.UI.Xaml.Controls.ContentDialog
                {
                    Title = "AzPin failed to start",
                    Content = $"{ex.Message}\n\nFull details: {logPath}",
                    CloseButtonText = "Quit",
                    XamlRoot = xamlRoot
                };
                try { await dialog.ShowAsync(); } catch { }
            }
            else
            {
                MessageBox(IntPtr.Zero,
                    $"AzPin failed to start.\n\n{ex.Message}\n\nFull details written to:\n{logPath}",
                    "AzPin", 0x10);
            }
            Exit();
        }
    }

    private static void LogAndExit(Exception? ex)
    {
        var logPath = Path.Combine(Path.GetTempPath(), "azpin-crash.log");
        try { File.WriteAllText(logPath, $"{DateTime.Now:O}\n{ex}"); } catch { }
        MessageBox(IntPtr.Zero, $"AzPin crashed.\n\n{ex?.Message}\n\nFull details:\n{logPath}", "AzPin", 0x10);
        Current.Exit();
    }

    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    private static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);

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
        services.AddTransient<ITokenCache, TokenCache>();
        services.AddTransient<IArmService, ArmService>();
        services.AddSingleton<IPinService, PinService>();
        services.AddSingleton<ISubscriptionSettingsService, SubscriptionSettingsService>();

        services.AddSingleton<AuthViewModel>();
        services.AddSingleton<BrowseViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton(sp => new TrayMenuViewModel(
            sp.GetRequiredService<AuthViewModel>(),
            sp.GetRequiredService<IPinService>(),
            sp.GetRequiredService<IArmService>(),
            quit: () => Current.Exit(),
            openMainWindow: () =>
            {
                var workArea = DisplayArea.GetFromWindowId(
                    mainWindow.AppWindow.Id,
                    DisplayAreaFallback.Nearest).WorkArea;
                int x = workArea.X + (workArea.Width - 960) / 2;
                int y = workArea.Y + (workArea.Height - 640) / 2;
                mainWindow.AppWindow.MoveAndResize(new RectInt32(x, y, 960, 640));
                mainWindow.AppWindow.IsShownInSwitchers = true;
                mainWindow.Activate();
            }));

        return services.BuildServiceProvider();
    }
}
