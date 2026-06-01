using StarCitizenTrader.Models;

namespace StarCitizenTrader.Services;

/// Interface for communicating with the UEX Corp API 2.0 marketplace endpoints.
public interface IUexApiService
{
    // Marketplace Listings
    Task<List<MarketplaceListing>> GetListingsAsync(int? idItem = null, string? operation = null, string? username = null, CancellationToken ct = default);
    Task<MarketplaceListing?> GetListingByIdAsync(int id, CancellationToken ct = default);

    // Marketplace Trends
    Task<List<MarketplaceTrend>> GetTrendsAsync(int? idCategory = null, string? currency = null, CancellationToken ct = default);

    // Pricing
    Task<List<PriceAverage>> GetPriceAveragesAsync(int idItem, string? operation = null, CancellationToken ct = default);
    Task<List<PriceHistory>> GetPriceHistoryAsync(int idItem, string? dateStart = null, string? dateEnd = null, CancellationToken ct = default);

    // Negotiations (requires auth)
    Task<List<Negotiation>> GetNegotiationsAsync(int? idListing = null, CancellationToken ct = default);
    Task<List<NegotiationMessage>> GetNegotiationMessagesAsync(string hash, CancellationToken ct = default);
    Task<int?> SendNegotiationMessageAsync(string hash, string message, CancellationToken ct = default);

    // Reference Data
    Task<List<Category>> GetCategoriesAsync(string? type = null, CancellationToken ct = default);
    Task<List<GameItem>> GetItemsAsync(int idCategory, CancellationToken ct = default);

    // Connection test
    Task<bool> TestConnectionAsync(CancellationToken ct = default);

    // Configuration
    void UpdateCredentials(string bearerToken, string secretKey);
    bool HasCredentials { get; }
    bool HasFullAuth { get; }
}
