using Serilog;
using StarCitizenTrader.Models;

namespace StarCitizenTrader.Services;

/// Background polling service that fetches marketplace data and triggers
/// wishlist match notifications.
public class MarketplaceMonitorService : IMarketplaceMonitorService
{
    private readonly IUexApiService _api;
    private readonly IDatabaseService _db;
    private readonly INotificationService _notifications;
    private readonly PollingSettings _polling;
    private readonly NotificationSettings _notifSettings;

    private CancellationTokenSource? _cts;
    private Task? _pollingTask;
    private DateTime? _lastListingsRefresh;
    private DateTime? _lastTrendsRefresh;

    public event EventHandler? DataRefreshed;
    public bool IsRunning => _cts != null && !_cts.IsCancellationRequested;
    public DateTime? LastRefreshTime { get; private set; }

    public MarketplaceMonitorService(
        IUexApiService api,
        IDatabaseService db,
        INotificationService notifications,
        PollingSettings polling,
        NotificationSettings notifSettings)
    {
        _api = api;
        _db = db;
        _notifications = notifications;
        _polling = polling;
        _notifSettings = notifSettings;
    }

    public void Start()
    {
        if (IsRunning) return;

        _cts = new CancellationTokenSource();
        _pollingTask = RunPollingLoopAsync(_cts.Token);
        Log.Information("Marketplace monitor started");
    }

    public void Stop()
    {
        _cts?.Cancel();
        _cts = null;
        Log.Information("Marketplace monitor stopped");
    }

    public async Task RefreshNowAsync(CancellationToken ct = default)
    {
        await RefreshListingsAsync(ct);
        await RefreshTrendsAsync(ct);
        await CheckWishlistMatchesAsync(ct);
        LastRefreshTime = DateTime.UtcNow;
        DataRefreshed?.Invoke(this, EventArgs.Empty);
    }

    private async Task RunPollingLoopAsync(CancellationToken ct)
    {
        // Initial load
        try
        {
            await RefreshNowAsync(ct);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Initial data load failed");
        }

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(Math.Min(
                    _polling.ListingsIntervalSeconds,
                    _polling.WishlistCheckIntervalSeconds)), ct);

                var now = DateTime.UtcNow;

                // Refresh listings
                if (!_lastListingsRefresh.HasValue ||
                    (now - _lastListingsRefresh.Value).TotalSeconds >= _polling.ListingsIntervalSeconds)
                {
                    await RefreshListingsAsync(ct);
                    _lastListingsRefresh = now;
                }

                // Refresh trends (less frequently)
                if (!_lastTrendsRefresh.HasValue ||
                    (now - _lastTrendsRefresh.Value).TotalSeconds >= _polling.TrendsIntervalSeconds)
                {
                    await RefreshTrendsAsync(ct);
                    _lastTrendsRefresh = now;
                }

                // Check wishlist
                await CheckWishlistMatchesAsync(ct);

                LastRefreshTime = DateTime.UtcNow;
                DataRefreshed?.Invoke(this, EventArgs.Empty);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in polling loop");
                try { await Task.Delay(TimeSpan.FromSeconds(30), ct); } catch { break; }
            }
        }
    }

    private async Task RefreshListingsAsync(CancellationToken ct)
    {
        Log.Debug("Refreshing marketplace listings...");
        var listings = await _api.GetListingsAsync(ct: ct);
        if (listings.Count > 0)
        {
            await _db.SaveListingsAsync(listings);
            Log.Information("Cached {Count} marketplace listings", listings.Count);
        }
    }

    private async Task RefreshTrendsAsync(CancellationToken ct)
    {
        Log.Debug("Refreshing marketplace trends...");
        var trends = await _api.GetTrendsAsync(ct: ct);
        if (trends.Count > 0)
        {
            await _db.SaveTrendsAsync(trends);
            Log.Information("Cached {Count} marketplace trends", trends.Count);
        }
    }

    private async Task CheckWishlistMatchesAsync(CancellationToken ct)
    {
        if (!_notifSettings.EnableWishlistAlerts) return;

        var wishlist = await _db.GetWishlistAsync();
        var activeItems = wishlist.Where(w => w.IsActive && w.NotifyOnMatch).ToList();

        if (activeItems.Count == 0) return;

        var listings = await _db.GetCachedListingsAsync();
        Log.Debug("Checking {WishlistCount} wishlist items against {ListingCount} listings",
            activeItems.Count, listings.Count);

        foreach (var wish in activeItems)
        {
            var matches = listings.Where(l => IsWishlistMatch(wish, l)).ToList();

            foreach (var listing in matches)
            {
                if (await _db.HasMatchBeenNotifiedAsync(wish.Id, listing.Id))
                    continue;

                // Record match
                var match = new WishlistMatch
                {
                    WishlistItemId = wish.Id,
                    ListingId = listing.Id,
                    ListingTitle = listing.Title,
                    Price = listing.Price,
                    Currency = listing.Currency,
                    SellerUsername = listing.UserUsername,
                    WasNotified = true
                };
                await _db.SaveWishlistMatchAsync(match);

                // Update wishlist item
                wish.MatchCount++;
                wish.LastMatchDate = DateTime.UtcNow;
                await _db.UpdateWishlistItemAsync(wish);

                // Send notification
                var priceInfo = $"{listing.Price:N0} {listing.Currency}";
                if (listing.Unit != null) priceInfo += $"/{listing.Unit}";

                await _notifications.SendNotificationAsync(
                    $"⭐ Wishlist Match: {wish.ItemName}",
                    $"\"{listing.Title}\" by {listing.UserUsername} — {priceInfo}",
                    NotificationType.WishlistMatch,
                    listing.Id,
                    wish.Id);

                Log.Information("Wishlist match found: {ItemName} → Listing #{ListingId}", wish.ItemName, listing.Id);
            }
        }
    }

    private static bool IsWishlistMatch(WishlistItem wish, MarketplaceListing listing)
    {
        // Item must match
        if (wish.IdItem > 0 && listing.IdItem != wish.IdItem)
            return false;

        // If no item ID, match by name (fuzzy)
        if (wish.IdItem <= 0 && !listing.Title.Contains(wish.ItemName, StringComparison.OrdinalIgnoreCase))
            return false;

        // Operation: if user wants to buy, look for sell listings and vice versa
        if (wish.Operation.Equals("buy", StringComparison.OrdinalIgnoreCase) && !listing.IsSellOrder)
            return false;
        if (wish.Operation.Equals("sell", StringComparison.OrdinalIgnoreCase) && !listing.IsBuyOrder)
            return false;

        // Sold out items don't match
        if (listing.IsSoldOut == 1) return false;

        // Price range check
        if (wish.Operation.Equals("buy", StringComparison.OrdinalIgnoreCase))
        {
            // Buying: listing price should be at or below max price
            if (wish.MaxPrice.HasValue && listing.Price > wish.MaxPrice.Value)
                return false;
        }
        else
        {
            // Selling: listing price should be at or above min price
            if (wish.MinPrice.HasValue && listing.Price < wish.MinPrice.Value)
                return false;
        }

        return true;
    }

    public void Dispose()
    {
        Stop();
        _cts?.Dispose();
    }
}
