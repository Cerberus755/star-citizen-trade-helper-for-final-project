using System.Text.Json.Serialization;

namespace StarCitizenTrader.Models;

/// Represents trending marketplace data from GET /marketplace_trends.
public class MarketplaceTrend
{
    [JsonPropertyName("id_item")]
    public int IdItem { get; set; }

    [JsonPropertyName("item_name")]
    public string ItemName { get; set; } = string.Empty;

    [JsonPropertyName("item_slug")]
    public string ItemSlug { get; set; } = string.Empty;

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "UEC";

    [JsonPropertyName("price_avg_sell")]
    public double? PriceAvgSell { get; set; }

    [JsonPropertyName("price_avg_month_sell")]
    public double? PriceAvgMonthSell { get; set; }

    [JsonPropertyName("price_min_sell")]
    public double? PriceMinSell { get; set; }

    [JsonPropertyName("price_max_sell")]
    public double? PriceMaxSell { get; set; }

    [JsonPropertyName("listings_count_sell")]
    public int? ListingsCountSell { get; set; }

    [JsonPropertyName("price_avg_buy")]
    public double? PriceAvgBuy { get; set; }

    [JsonPropertyName("price_avg_month_buy")]
    public double? PriceAvgMonthBuy { get; set; }

    [JsonPropertyName("price_min_buy")]
    public double? PriceMinBuy { get; set; }

    [JsonPropertyName("price_max_buy")]
    public double? PriceMaxBuy { get; set; }

    [JsonPropertyName("listings_count_buy")]
    public int? ListingsCountBuy { get; set; }

    [JsonPropertyName("total_listings_count")]
    public int TotalListingsCount { get; set; }

    [JsonPropertyName("negotiations_count")]
    public int NegotiationsCount { get; set; }

    [JsonPropertyName("negotiations_open")]
    public int NegotiationsOpen { get; set; }

    [JsonPropertyName("negotiations_success")]
    public int NegotiationsSuccess { get; set; }

    [JsonPropertyName("link_prices")]
    public string? LinkPrices { get; set; }

    [JsonPropertyName("link_prices_history")]
    public string? LinkPricesHistory { get; set; }

    // Computed
    public string FormattedAvgSell => PriceAvgSell.HasValue ? $"{PriceAvgSell.Value:N0} {Currency}" : "N/A";
    public string FormattedAvgBuy => PriceAvgBuy.HasValue ? $"{PriceAvgBuy.Value:N0} {Currency}" : "N/A";
    public int TotalListings => TotalListingsCount;

    public double? PriceSpread =>
        PriceAvgSell.HasValue && PriceAvgBuy.HasValue
            ? PriceAvgSell.Value - PriceAvgBuy.Value
            : null;

    public double? MonthlyTrend =>
        PriceAvgSell.HasValue && PriceAvgMonthSell.HasValue && PriceAvgMonthSell.Value > 0
            ? ((PriceAvgSell.Value - PriceAvgMonthSell.Value) / PriceAvgMonthSell.Value) * 100
            : null;

    public string TrendIndicator
    {
        get
        {
            if (!MonthlyTrend.HasValue) return "—";
            return MonthlyTrend.Value switch
            {
                > 5 => "📈 Rising",
                < -5 => "📉 Falling",
                _ => "➡️ Stable"
            };
        }
    }
}
