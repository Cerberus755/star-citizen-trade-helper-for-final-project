using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Serilog;
using StarCitizenTrader.Helpers;
using StarCitizenTrader.Models;

namespace StarCitizenTrader.Services;

/// HTTP client wrapper for the UEX Corp API 2.0 with rate limiting and error handling.
public class UexApiService : IUexApiService, IDisposable
{
    private readonly HttpClient _http;
    private readonly RateLimiter _rateLimiter;
    private readonly JsonSerializerOptions _jsonOptions;
    private string _baseUrl;
    private string _bearerToken;
    private string _secretKey;
    private string _clientVersion;

    public bool HasCredentials => !string.IsNullOrWhiteSpace(_bearerToken);
    public bool HasFullAuth => HasCredentials && !string.IsNullOrWhiteSpace(_secretKey);

    public UexApiService(AppSettings settings)
    {
        _baseUrl = settings.UexApi.BaseUrl.TrimEnd('/');
        _bearerToken = settings.UexApi.BearerToken;
        _secretKey = settings.UexApi.SecretKey;
        _clientVersion = settings.UexApi.ClientVersion;
        _rateLimiter = new RateLimiter(settings.UexApi.RateLimitPerMinute);

        _http = new HttpClient();
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _http.Timeout = TimeSpan.FromSeconds(30);
        ApplyHeaders();

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
        };
    }

    public void UpdateCredentials(string bearerToken, string secretKey)
    {
        _bearerToken = bearerToken;
        _secretKey = secretKey;
        ApplyHeaders();
        Log.Information("API credentials updated");
    }

    private void ApplyHeaders()
    {
        _http.DefaultRequestHeaders.Remove("Authorization");
        _http.DefaultRequestHeaders.Remove("secret-key");
        _http.DefaultRequestHeaders.Remove("X-Client-Version");

        if (!string.IsNullOrWhiteSpace(_bearerToken))
            _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {_bearerToken}");
        if (!string.IsNullOrWhiteSpace(_secretKey))
            _http.DefaultRequestHeaders.Add("secret-key", _secretKey);
        if (!string.IsNullOrWhiteSpace(_clientVersion))
            _http.DefaultRequestHeaders.Add("X-Client-Version", _clientVersion);
    }

    // ─── Core HTTP Methods ─────────────────────────────────────────

    private async Task<ApiResponse<List<T>>> GetListAsync<T>(string endpoint, Dictionary<string, string>? queryParams = null, CancellationToken ct = default)
    {
        await _rateLimiter.WaitForSlotAsync(ct);

        var url = BuildUrl(endpoint, queryParams);
        Log.Debug("GET {Url}", url);

        try
        {
            var response = await _http.GetAsync(url, ct);
            var json = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                Log.Warning("API returned {StatusCode} for {Url}: {Body}", response.StatusCode, url, json);
                return new ApiResponse<List<T>> { Status = "error", Message = $"HTTP {(int)response.StatusCode}" };
            }

            var result = JsonSerializer.Deserialize<ApiResponse<List<T>>>(json, _jsonOptions);
            if (result == null)
                return new ApiResponse<List<T>> { Status = "error", Message = "Failed to deserialize response" };

            if (result.IsRateLimited)
            {
                Log.Warning("Rate limit reached, backing off...");
                await Task.Delay(TimeSpan.FromSeconds(30), ct);
            }

            return result;
        }
        catch (TaskCanceledException) { throw; }
        catch (Exception ex)
        {
            Log.Error(ex, "API request failed: {Url}", url);
            return new ApiResponse<List<T>> { Status = "error", Message = ex.Message };
        }
    }

    private async Task<ApiResponse<T>> PostAsync<T>(string endpoint, object body, CancellationToken ct = default)
    {
        await _rateLimiter.WaitForSlotAsync(ct);

        var url = $"{_baseUrl}/{endpoint}/";
        var jsonBody = JsonSerializer.Serialize(body, _jsonOptions);
        Log.Debug("POST {Url} Body={Body}", url, jsonBody);

        try
        {
            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            var response = await _http.PostAsync(url, content, ct);
            var json = await response.Content.ReadAsStringAsync(ct);

            var result = JsonSerializer.Deserialize<ApiResponse<T>>(json, _jsonOptions);
            return result ?? new ApiResponse<T> { Status = "error", Message = "Deserialization failed" };
        }
        catch (TaskCanceledException) { throw; }
        catch (Exception ex)
        {
            Log.Error(ex, "POST request failed: {Url}", url);
            return new ApiResponse<T> { Status = "error", Message = ex.Message };
        }
    }

    private string BuildUrl(string endpoint, Dictionary<string, string>? queryParams)
    {
        var url = $"{_baseUrl}/{endpoint}/";
        if (queryParams != null && queryParams.Count > 0)
        {
            var qs = string.Join("&", queryParams
                .Where(kv => !string.IsNullOrEmpty(kv.Value))
                .Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));
            if (!string.IsNullOrEmpty(qs))
                url += $"?{qs}";
        }
        return url;
    }

    // ─── Marketplace Listings ──────────────────────────────────────

    public async Task<List<MarketplaceListing>> GetListingsAsync(int? idItem = null, string? operation = null, string? username = null, CancellationToken ct = default)
    {
        var p = new Dictionary<string, string>();
        if (idItem.HasValue) p["id_item"] = idItem.Value.ToString();
        if (!string.IsNullOrEmpty(operation)) p["operation"] = operation;
        if (!string.IsNullOrEmpty(username)) p["username"] = username;

        var result = await GetListAsync<MarketplaceListing>("marketplace_listings", p, ct);
        return result.Data ?? new List<MarketplaceListing>();
    }

    public async Task<MarketplaceListing?> GetListingByIdAsync(int id, CancellationToken ct = default)
    {
        var result = await GetListAsync<MarketplaceListing>("marketplace_listings",
            new Dictionary<string, string> { ["id"] = id.ToString() }, ct);
        return result.Data?.FirstOrDefault();
    }

    // ─── Marketplace Trends ────────────────────────────────────────

    public async Task<List<MarketplaceTrend>> GetTrendsAsync(int? idCategory = null, string? currency = null, CancellationToken ct = default)
    {
        var p = new Dictionary<string, string>();
        if (idCategory.HasValue) p["id_category"] = idCategory.Value.ToString();
        if (!string.IsNullOrEmpty(currency)) p["currency"] = currency;

        var result = await GetListAsync<MarketplaceTrend>("marketplace_trends", p, ct);
        return result.Data ?? new List<MarketplaceTrend>();
    }

    // ─── Pricing ───────────────────────────────────────────────────

    public async Task<List<PriceAverage>> GetPriceAveragesAsync(int idItem, string? operation = null, CancellationToken ct = default)
    {
        var p = new Dictionary<string, string> { ["id_item"] = idItem.ToString() };
        if (!string.IsNullOrEmpty(operation)) p["operation"] = operation;

        var result = await GetListAsync<PriceAverage>("marketplace_prices_averages", p, ct);
        return result.Data ?? new List<PriceAverage>();
    }

    public async Task<List<PriceHistory>> GetPriceHistoryAsync(int idItem, string? dateStart = null, string? dateEnd = null, CancellationToken ct = default)
    {
        var p = new Dictionary<string, string> { ["id_item"] = idItem.ToString() };
        if (!string.IsNullOrEmpty(dateStart)) p["date_start"] = dateStart;
        if (!string.IsNullOrEmpty(dateEnd)) p["date_end"] = dateEnd;

        var result = await GetListAsync<PriceHistory>("marketplace_prices_history", p, ct);
        return result.Data ?? new List<PriceHistory>();
    }

    // ─── Negotiations ──────────────────────────────────────────────

    public async Task<List<Negotiation>> GetNegotiationsAsync(int? idListing = null, CancellationToken ct = default)
    {
        var p = new Dictionary<string, string>();
        if (idListing.HasValue) p["id_listing"] = idListing.Value.ToString();

        var result = await GetListAsync<Negotiation>("marketplace_negotiations", p, ct);
        return result.Data ?? new List<Negotiation>();
    }

    public async Task<List<NegotiationMessage>> GetNegotiationMessagesAsync(string hash, CancellationToken ct = default)
    {
        var result = await GetListAsync<NegotiationMessage>("marketplace_negotiations_messages",
            new Dictionary<string, string> { ["hash"] = hash }, ct);
        return result.Data ?? new List<NegotiationMessage>();
    }

    public async Task<int?> SendNegotiationMessageAsync(string hash, string message, CancellationToken ct = default)
    {
        var body = new { hash, message, is_production = 1 };
        var result = await PostAsync<Dictionary<string, int>>("marketplace_negotiations_messages", body, ct);
        if (result.IsSuccess && result.Data != null && result.Data.ContainsKey("id_message"))
            return result.Data["id_message"];
        return null;
    }

    // ─── Reference Data ────────────────────────────────────────────

    public async Task<List<Category>> GetCategoriesAsync(string? type = null, CancellationToken ct = default)
    {
        var p = new Dictionary<string, string>();
        if (!string.IsNullOrEmpty(type)) p["type"] = type;

        var result = await GetListAsync<Category>("categories", p, ct);
        return result.Data ?? new List<Category>();
    }

    public async Task<List<GameItem>> GetItemsAsync(int idCategory, CancellationToken ct = default)
    {
        var result = await GetListAsync<GameItem>("items",
            new Dictionary<string, string> { ["id_category"] = idCategory.ToString() }, ct);
        return result.Data ?? new List<GameItem>();
    }

    // ─── Connection Test ───────────────────────────────────────────

    public async Task<bool> TestConnectionAsync(CancellationToken ct = default)
    {
        try
        {
            var result = await GetListAsync<MarketplaceTrend>("marketplace_trends", null, ct);
            return result.IsSuccess;
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        _http.Dispose();
        _rateLimiter.Dispose();
    }
}
