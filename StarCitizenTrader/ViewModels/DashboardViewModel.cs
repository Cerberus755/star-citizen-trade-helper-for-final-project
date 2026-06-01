using System.Collections.ObjectModel;
using System.Windows.Input;
using Serilog;
using StarCitizenTrader.Helpers;
using StarCitizenTrader.Models;
using StarCitizenTrader.Services;

namespace StarCitizenTrader.ViewModels;

/// Dashboard view — shows summary stats, trending items, and recent activity.
public class DashboardViewModel : ViewModelBase
{
    private readonly IUexApiService _api;
    private readonly IDatabaseService _db;
    private readonly IMarketplaceMonitorService _monitor;

    private bool _isLoading;
    private int _totalListings;
    private int _totalTrends;
    private int _activeWishlistItems;
    private string _lastUpdateText = "Never";

    public ObservableCollection<MarketplaceTrend> TopTrends { get; } = new();
    public ObservableCollection<MarketplaceListing> RecentListings { get; } = new();
    public ObservableCollection<WishlistMatch> RecentMatches { get; } = new();

    public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }
    public int TotalListings { get => _totalListings; set => SetProperty(ref _totalListings, value); }
    public int TotalTrends { get => _totalTrends; set => SetProperty(ref _totalTrends, value); }
    public int ActiveWishlistItems { get => _activeWishlistItems; set => SetProperty(ref _activeWishlistItems, value); }
    public string LastUpdateText { get => _lastUpdateText; set => SetProperty(ref _lastUpdateText, value); }

    public ICommand RefreshCommand { get; }

    public DashboardViewModel(IUexApiService api, IDatabaseService db, IMarketplaceMonitorService monitor)
    {
        _api = api;
        _db = db;
        _monitor = monitor;
        RefreshCommand = new AsyncRelayCommand(LoadDataAsync);

        _monitor.DataRefreshed += async (_, _) => await LoadDataAsync();
    }

    public async Task LoadDataAsync()
    {
        IsLoading = true;
        try
        {
            // Load trends
            var trends = await _db.GetCachedTrendsAsync();
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                TopTrends.Clear();
                foreach (var t in trends.OrderByDescending(t => t.NegotiationsCount).Take(20))
                    TopTrends.Add(t);
                TotalTrends = trends.Count;
            });

            // Load recent listings
            var listings = await _db.GetCachedListingsAsync();
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                RecentListings.Clear();
                foreach (var l in listings.OrderByDescending(l => l.DateAdded).Take(10))
                    RecentListings.Add(l);
                TotalListings = listings.Count;
            });

            // Load wishlist stats
            var wishlist = await _db.GetWishlistAsync();
            ActiveWishlistItems = wishlist.Count(w => w.IsActive);

            // Load recent matches
            var matches = await _db.GetRecentMatchesAsync(10);
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                RecentMatches.Clear();
                foreach (var m in matches) RecentMatches.Add(m);
            });

            LastUpdateText = DateTime.Now.ToString("HH:mm:ss");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load dashboard data");
        }
        finally
        {
            IsLoading = false;
        }
    }
}
