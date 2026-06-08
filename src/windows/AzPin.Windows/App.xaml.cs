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
        try
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

            // Activate once so the visual tree (and TaskbarIcon XamlRoot) is live,
            // then immediately hide so the app starts tray-only.
            _mainWindow.Activate();
            _mainWindow.AppWindow.Hide();
            _mainWindow.AppWindow.IsShownInSwitchers = false;
        }
        catch (Exception ex)
        {
            // Write crash log first — dialog may fail if XamlRoot is not yet live.
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
                // XamlRoot not available (crash before Activate) — use Win32 MessageBox.
                MessageBox(IntPtr.Zero,
                    $"AzPin failed to start.\n\n{ex.Message}\n\nFull details written to:\n{logPath}",
                    "AzPin", 0x10 /* MB_ICONERROR */);
            }
            Exit();
        }
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
        // Transient: these hold per-operation state (token, HttpClient).  Using Scoped
        // would create captive dependencies inside Singleton view-models.
        services.AddTransient<ITokenCache, TokenCache>();
        services.AddTransient<IArmService, ArmService>();
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
