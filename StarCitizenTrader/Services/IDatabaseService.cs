using StarCitizenTrader.Models;

namespace StarCitizenTrader.Services;

/// Interface for local SQLite data persistence.
public interface IDatabaseService : IDisposable
{
    Task InitializeAsync();

    // Listings cache
    Task SaveListingsAsync(IEnumerable<MarketplaceListing> listings);
    Task<List<MarketplaceListing>> GetCachedListingsAsync();
    Task<DateTime?> GetListingsCacheTimeAsync();

    // Trends cache
    Task SaveTrendsAsync(IEnumerable<MarketplaceTrend> trends);
    Task<List<MarketplaceTrend>> GetCachedTrendsAsync();

    // Price history
    Task SavePriceHistoryAsync(IEnumerable<PriceHistory> history);
    Task<List<PriceHistory>> GetPriceHistoryAsync(int idItem);

    // Wishlist
    Task<List<WishlistItem>> GetWishlistAsync();
    Task<int> AddWishlistItemAsync(WishlistItem item);
    Task UpdateWishlistItemAsync(WishlistItem item);
    Task DeleteWishlistItemAsync(int id);

    // Wishlist matches
    Task SaveWishlistMatchAsync(WishlistMatch match);
    Task<List<WishlistMatch>> GetRecentMatchesAsync(int limit = 50);
    Task<bool> HasMatchBeenNotifiedAsync(int wishlistItemId, int listingId);

    // Notifications
    Task SaveNotificationAsync(AppNotification notification);
    Task<List<AppNotification>> GetNotificationsAsync(int limit = 100);
    Task MarkNotificationReadAsync(int id);
    Task ClearNotificationsAsync();

    // Settings
    Task SaveSettingAsync(string key, string value);
    Task<string?> GetSettingAsync(string key);
}
