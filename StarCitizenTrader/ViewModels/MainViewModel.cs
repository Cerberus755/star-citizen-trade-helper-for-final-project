using System.Collections.ObjectModel;
using System.Windows.Input;
using StarCitizenTrader.Helpers;
using StarCitizenTrader.Models;
using StarCitizenTrader.Services;

namespace StarCitizenTrader.ViewModels;

/// Main shell ViewModel — manages navigation, notification badge, and child views.
public class MainViewModel : ViewModelBase
{
    private readonly INotificationService _notifications;
    private readonly IMarketplaceMonitorService _monitor;

    private ViewModelBase _currentView = null!;
    private string _currentViewName = "Dashboard";
    private int _unreadNotifications;
    private string _statusMessage = "Ready";
    private bool _isConnected;

    public DashboardViewModel DashboardVm { get; }
    public ListingsViewModel ListingsVm { get; }
    public WishlistViewModel WishlistVm { get; }
    public NegotiationsViewModel NegotiationsVm { get; }
    public SettingsViewModel SettingsVm { get; }

    public ObservableCollection<AppNotification> RecentNotifications { get; } = new();

    public ViewModelBase CurrentView
    {
        get => _currentView;
        set => SetProperty(ref _currentView, value);
    }

    public string CurrentViewName
    {
        get => _currentViewName;
        set => SetProperty(ref _currentViewName, value);
    }

    public int UnreadNotifications
    {
        get => _unreadNotifications;
        set => SetProperty(ref _unreadNotifications, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public bool IsConnected
    {
        get => _isConnected;
        set => SetProperty(ref _isConnected, value);
    }

    // Navigation commands
    public ICommand NavigateDashboardCommand { get; }
    public ICommand NavigateListingsCommand { get; }
    public ICommand NavigateWishlistCommand { get; }
    public ICommand NavigateNegotiationsCommand { get; }
    public ICommand NavigateSettingsCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand ClearNotificationsCommand { get; }

    public MainViewModel(
        DashboardViewModel dashboardVm,
        ListingsViewModel listingsVm,
        WishlistViewModel wishlistVm,
        NegotiationsViewModel negotiationsVm,
        SettingsViewModel settingsVm,
        INotificationService notifications,
        IMarketplaceMonitorService monitor)
    {
        DashboardVm = dashboardVm;
        ListingsVm = listingsVm;
        WishlistVm = wishlistVm;
        NegotiationsVm = negotiationsVm;
        SettingsVm = settingsVm;
        _notifications = notifications;
        _monitor = monitor;

        // Start on Dashboard
        _currentView = DashboardVm;

        NavigateDashboardCommand = new RelayCommand(() => NavigateTo(DashboardVm, "Dashboard"));
        NavigateListingsCommand = new RelayCommand(() => NavigateTo(ListingsVm, "Active Listings"));
        NavigateWishlistCommand = new RelayCommand(() => NavigateTo(WishlistVm, "Wishlist"));
        NavigateNegotiationsCommand = new RelayCommand(() => NavigateTo(NegotiationsVm, "Negotiations"));
        NavigateSettingsCommand = new RelayCommand(() => NavigateTo(SettingsVm, "Settings"));
        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        ClearNotificationsCommand = new AsyncRelayCommand(ClearNotificationsAsync);

        // Subscribe to notification events
        _notifications.NotificationReceived += OnNotificationReceived;
        _monitor.DataRefreshed += OnDataRefreshed;
    }

    private void NavigateTo(ViewModelBase vm, string name)
    {
        CurrentView = vm;
        CurrentViewName = name;
    }

    private async Task RefreshAsync()
    {
        StatusMessage = "Refreshing...";
        await _monitor.RefreshNowAsync();
        StatusMessage = $"Last refresh: {DateTime.Now:HH:mm:ss}";
    }

    private async Task ClearNotificationsAsync()
    {
        await _notifications.ClearAllAsync();
        RecentNotifications.Clear();
        UnreadNotifications = 0;
    }

    private void OnNotificationReceived(object? sender, AppNotification notification)
    {
        System.Windows.Application.Current?.Dispatcher.Invoke(() =>
        {
            RecentNotifications.Insert(0, notification);
            if (RecentNotifications.Count > 50)
                RecentNotifications.RemoveAt(RecentNotifications.Count - 1);
            UnreadNotifications = _notifications.UnreadCount;
        });
    }

    private void OnDataRefreshed(object? sender, EventArgs e)
    {
        System.Windows.Application.Current?.Dispatcher.Invoke(() =>
        {
            StatusMessage = $"Data refreshed at {DateTime.Now:HH:mm:ss}";
            IsConnected = true;
        });
    }

    public async Task InitializeAsync()
    {
        var existing = await _notifications.GetRecentNotificationsAsync(50);
        foreach (var n in existing)
            RecentNotifications.Add(n);
        UnreadNotifications = _notifications.UnreadCount;

        _monitor.Start();
        StatusMessage = "Connected — monitoring marketplace";
        IsConnected = true;
    }
}
