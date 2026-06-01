using System.Text.Json.Serialization;

namespace StarCitizenTrader.Models;

/// Historical price snapshot from GET /marketplace_prices_history.
public class PriceHistory
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("id_item")]
    public int IdItem { get; set; }

    [JsonPropertyName("id_listing")]
    public int IdListing { get; set; }

    [JsonPropertyName("item_name")]
    public string ItemName { get; set; } = string.Empty;

    [JsonPropertyName("price")]
    public double Price { get; set; }

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "UEC";

    [JsonPropertyName("quality")]
    public int Quality { get; set; }

    [JsonPropertyName("quality_tier")]
    public int QualityTier { get; set; }

    [JsonPropertyName("game_version")]
    public string GameVersion { get; set; } = string.Empty;

    [JsonPropertyName("date_added")]
    public long DateAdded { get; set; }

    [JsonPropertyName("date_removed")]
    public long DateRemoved { get; set; }

    // Computed
    public DateTime DateAddedUtc => DateTimeOffset.FromUnixTimeSeconds(DateAdded).UtcDateTime;
    public bool IsActive => DateRemoved == 0;
    public string FormattedPrice => $"{Price:N0} {Currency}";
}

/// Average price data from GET /marketplace_prices_averages.
public class PriceAverage
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("id_item")]
    public int IdItem { get; set; }

    [JsonPropertyName("id_category")]
    public int IdCategory { get; set; }

    [JsonPropertyName("item_uuid")]
    public string? ItemUuid { get; set; }

    [JsonPropertyName("item_slug")]
    public string ItemSlug { get; set; } = string.Empty;

    [JsonPropertyName("item_name")]
    public string ItemName { get; set; } = string.Empty;

    [JsonPropertyName("quality_tier")]
    public int QualityTier { get; set; }

    [JsonPropertyName("operation")]
    public string Operation { get; set; } = string.Empty;

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "UEC";

    [JsonPropertyName("listings_count")]
    public int ListingsCount { get; set; }

    [JsonPropertyName("price_avg")]
    public double PriceAvg { get; set; }

    [JsonPropertyName("price_avg_week")]
    public double PriceAvgWeek { get; set; }

    [JsonPropertyName("price_avg_month")]
    public double PriceAvgMonth { get; set; }

    [JsonPropertyName("game_version")]
    public string GameVersion { get; set; } = string.Empty;

    [JsonPropertyName("date_added")]
    public long DateAdded { get; set; }

    [JsonPropertyName("date_modified")]
    public long DateModified { get; set; }

    // Computed
    public double WeeklyChange => PriceAvgWeek > 0 ? ((PriceAvg - PriceAvgWeek) / PriceAvgWeek) * 100 : 0;
    public double MonthlyChange => PriceAvgMonth > 0 ? ((PriceAvg - PriceAvgMonth) / PriceAvgMonth) * 100 : 0;
}
