using System.Windows.Input;
using Serilog;
using StarCitizenTrader.Helpers;
using StarCitizenTrader.Models;
using StarCitizenTrader.Services;

namespace StarCitizenTrader.ViewModels;

/// Settings view — API credentials, polling intervals, notification preferences.
public class SettingsViewModel : ViewModelBase
{
    private readonly IUexApiService _api;
    private readonly IDatabaseService _db;
    private readonly AppSettings _settings;

    private string _bearerToken = string.Empty;
    private string _secretKey = string.Empty;
    private string _connectionStatus = "Not tested";
    private bool _isTesting;
    private bool _enableAutoRefresh;
    private int _listingsInterval;
    private int _trendsInterval;
    private int _wishlistInterval;
    private bool _enableInAppNotifs;
    private bool _enableToastNotifs;
    private bool _enableWishlistAlerts;
    private string _saveStatus = string.Empty;

    public string BearerToken { get => _bearerToken; set => SetProperty(ref _bearerToken, value); }
    public string SecretKey { get => _secretKey; set => SetProperty(ref _secretKey, value); }
    public string ConnectionStatus { get => _connectionStatus; set => SetProperty(ref _connectionStatus, value); }
    public bool IsTesting { get => _isTesting; set => SetProperty(ref _isTesting, value); }

    public bool EnableAutoRefresh { get => _enableAutoRefresh; set => SetProperty(ref _enableAutoRefresh, value); }
    public int ListingsInterval { get => _listingsInterval; set => SetProperty(ref _listingsInterval, value); }
    public int TrendsInterval { get => _trendsInterval; set => SetProperty(ref _trendsInterval, value); }
    public int WishlistInterval { get => _wishlistInterval; set => SetProperty(ref _wishlistInterval, value); }

    public bool EnableInAppNotifs { get => _enableInAppNotifs; set => SetProperty(ref _enableInAppNotifs, value); }
    public bool EnableToastNotifs { get => _enableToastNotifs; set => SetProperty(ref _enableToastNotifs, value); }
    public bool EnableWishlistAlerts { get => _enableWishlistAlerts; set => SetProperty(ref _enableWishlistAlerts, value); }

    public string SaveStatus { get => _saveStatus; set => SetProperty(ref _saveStatus, value); }

    public ICommand TestConnectionCommand { get; }
    public ICommand SaveSettingsCommand { get; }
    public ICommand ResetDefaultsCommand { get; }

    public SettingsViewModel(IUexApiService api, IDatabaseService db, AppSettings settings)
    {
        _api = api;
        _db = db;
        _settings = settings;

        // Load current settings
        BearerToken = settings.UexApi.BearerToken;
        SecretKey = settings.UexApi.SecretKey;
        EnableAutoRefresh = settings.Polling.EnableAutoRefresh;
        ListingsInterval = settings.Polling.ListingsIntervalSeconds;
        TrendsInterval = settings.Polling.TrendsIntervalSeconds;
        WishlistInterval = settings.Polling.WishlistCheckIntervalSeconds;
        EnableInAppNotifs = settings.Notifications.EnableInAppNotifications;
        EnableToastNotifs = settings.Notifications.EnableToastNotifications;
        EnableWishlistAlerts = settings.Notifications.EnableWishlistAlerts;

        TestConnectionCommand = new AsyncRelayCommand(TestConnectionAsync);
        SaveSettingsCommand = new AsyncRelayCommand(SaveSettingsAsync);
        ResetDefaultsCommand = new RelayCommand(ResetDefaults);
    }

    private async Task TestConnectionAsync()
    {
        IsTesting = true;
        ConnectionStatus = "Testing...";

        try
        {
            // Temporarily apply credentials for testing
            _api.UpdateCredentials(BearerToken, SecretKey);

            var connected = await _api.TestConnectionAsync();
            ConnectionStatus = connected
                ? "✅ Connected successfully!"
                : "❌ Connection failed. Check your API key.";
        }
        catch (Exception ex)
        {
            ConnectionStatus = $"❌ Error: {ex.Message}";
            Log.Error(ex, "Connection test failed");
        }
        finally
        {
            IsTesting = false;
        }
    }

    private async Task SaveSettingsAsync()
    {
        try
        {
            // Persist to DB
            await _db.SaveSettingAsync("bearer_token", BearerToken);
            await _db.SaveSettingAsync("secret_key", SecretKey);
            await _db.SaveSettingAsync("enable_auto_refresh", EnableAutoRefresh.ToString());
            await _db.SaveSettingAsync("listings_interval", ListingsInterval.ToString());
            await _db.SaveSettingAsync("trends_interval", TrendsInterval.ToString());
            await _db.SaveSettingAsync("wishlist_interval", WishlistInterval.ToString());
            await _db.SaveSettingAsync("enable_in_app_notifs", EnableInAppNotifs.ToString());
            await _db.SaveSettingAsync("enable_toast_notifs", EnableToastNotifs.ToString());
            await _db.SaveSettingAsync("enable_wishlist_alerts", EnableWishlistAlerts.ToString());

            // Apply to running service
            _api.UpdateCredentials(BearerToken, SecretKey);

            // Update in-memory settings
            _settings.UexApi.BearerToken = BearerToken;
            _settings.UexApi.SecretKey = SecretKey;
            _settings.Polling.EnableAutoRefresh = EnableAutoRefresh;
            _settings.Polling.ListingsIntervalSeconds = ListingsInterval;
            _settings.Polling.TrendsIntervalSeconds = TrendsInterval;
            _settings.Polling.WishlistCheckIntervalSeconds = WishlistInterval;
            _settings.Notifications.EnableInAppNotifications = EnableInAppNotifs;
            _settings.Notifications.EnableToastNotifications = EnableToastNotifs;
            _settings.Notifications.EnableWishlistAlerts = EnableWishlistAlerts;

            SaveStatus = "✅ Settings saved successfully!";
            Log.Information("Settings saved");
        }
        catch (Exception ex)
        {
            SaveStatus = $"❌ Save failed: {ex.Message}";
            Log.Error(ex, "Failed to save settings");
        }
    }

    private void ResetDefaults()
    {
        EnableAutoRefresh = true;
        ListingsInterval = 120;
        TrendsInterval = 3600;
        WishlistInterval = 180;
        EnableInAppNotifs = true;
        EnableToastNotifs = true;
        EnableWishlistAlerts = true;
        SaveStatus = "Defaults restored (save to apply)";
    }

    public async Task LoadSavedCredentialsAsync()
    {
        var token = await _db.GetSettingAsync("bearer_token");
        var secret = await _db.GetSettingAsync("secret_key");

        if (!string.IsNullOrEmpty(token)) BearerToken = token;
        if (!string.IsNullOrEmpty(secret)) SecretKey = secret;

        if (!string.IsNullOrEmpty(token) || !string.IsNullOrEmpty(secret))
            _api.UpdateCredentials(BearerToken, SecretKey);
    }
}
