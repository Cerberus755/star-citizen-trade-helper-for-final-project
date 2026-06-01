using System.IO;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using StarCitizenTrader.Models;
using StarCitizenTrader.Services;
using StarCitizenTrader.ViewModels;
using StarCitizenTrader.Views;

namespace StarCitizenTrader;

/// Application entry point — configures DI, logging, and launches the main window.
public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // ─── Logging ───────────────────────────────────────────
        var logDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "StarCitizenTrader", "logs");
        Directory.CreateDirectory(logDir);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(Path.Combine(logDir, "app-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.Console()
            .CreateLogger();

        Log.Information("═══ Star Citizen Trader starting ═══");

        // ─── Configuration ─────────────────────────────────────
        var config = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();

        var settings = new AppSettings();
        config.Bind(settings);

        // ─── DI Container ──────────────────────────────────────
        var services = new ServiceCollection();

        // Settings (singleton)
        services.AddSingleton(settings);
        services.AddSingleton(settings.Polling);
        services.AddSingleton(settings.Notifications);

        // Services
        services.AddSingleton<IUexApiService>(sp => new UexApiService(sp.GetRequiredService<AppSettings>()));
        services.AddSingleton<IDatabaseService>(sp => new DatabaseService(sp.GetRequiredService<AppSettings>().DatabasePath));
        services.AddSingleton<INotificationService>(sp => new NotificationService(
            sp.GetRequiredService<IDatabaseService>(),
            sp.GetRequiredService<NotificationSettings>()));
        services.AddSingleton<IMarketplaceMonitorService>(sp => new MarketplaceMonitorService(
            sp.GetRequiredService<IUexApiService>(),
            sp.GetRequiredService<IDatabaseService>(),
            sp.GetRequiredService<INotificationService>(),
            sp.GetRequiredService<PollingSettings>(),
            sp.GetRequiredService<NotificationSettings>()));

        // ViewModels
        services.AddSingleton<DashboardViewModel>();
        services.AddSingleton<ListingsViewModel>();
        services.AddSingleton<WishlistViewModel>();
        services.AddSingleton<NegotiationsViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<MainViewModel>();

        // Views
        services.AddSingleton<MainWindow>();

        _serviceProvider = services.BuildServiceProvider();

        // ─── Initialize Database ───────────────────────────────
        try
        {
            var db = _serviceProvider.GetRequiredService<IDatabaseService>();
            await db.InitializeAsync();
            Log.Information("Database initialized");

            // Load saved credentials from DB into the API service
            var settingsVm = _serviceProvider.GetRequiredService<SettingsViewModel>();
            await settingsVm.LoadSavedCredentialsAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to initialize database");
        }

        // ─── Show Main Window ──────────────────────────────────
        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();

        Log.Information("Application started successfully");
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("═══ Star Citizen Trader shutting down ═══");

        if (_serviceProvider != null)
        {
            var monitor = _serviceProvider.GetService<IMarketplaceMonitorService>();
            monitor?.Dispose();

            var db = _serviceProvider.GetService<IDatabaseService>();
            db?.Dispose();

            var api = _serviceProvider.GetService<IUexApiService>();
            (api as IDisposable)?.Dispose();
        }

        _serviceProvider?.Dispose();
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
