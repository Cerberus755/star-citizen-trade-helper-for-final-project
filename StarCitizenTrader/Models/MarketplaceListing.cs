using System.Text.Json.Serialization;

namespace StarCitizenTrader.Models;

/// Represents a single marketplace listing from GET /marketplace_listings.
public class MarketplaceListing
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("id_category")]
    public int IdCategory { get; set; }

    [JsonPropertyName("id_item")]
    public int IdItem { get; set; }

    [JsonPropertyName("id_star_system")]
    public int IdStarSystem { get; set; }

    [JsonPropertyName("id_terminal")]
    public int IdTerminal { get; set; }

    [JsonPropertyName("id_organization")]
    public int IdOrganization { get; set; }

    [JsonPropertyName("operation")]
    public string Operation { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("slug")]
    public string Slug { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("unit")]
    public string? Unit { get; set; }

    [JsonPropertyName("price")]
    public double Price { get; set; }

    [JsonPropertyName("price_old")]
    public double PriceOld { get; set; }

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "UEC";

    [JsonPropertyName("language")]
    public string Language { get; set; } = "en_US";

    [JsonPropertyName("location")]
    public string? Location { get; set; }

    [JsonPropertyName("source")]
    public string? Source { get; set; }

    [JsonPropertyName("availability")]
    public string? Availability { get; set; }

    [JsonPropertyName("durability")]
    public string? Durability { get; set; }

    [JsonPropertyName("quality")]
    public string? Quality { get; set; }

    [JsonPropertyName("in_stock")]
    public int InStock { get; set; }

    [JsonPropertyName("is_sold_out")]
    public int IsSoldOut { get; set; }

    [JsonPropertyName("user_name")]
    public string UserName { get; set; } = string.Empty;

    [JsonPropertyName("user_username")]
    public string UserUsername { get; set; } = string.Empty;

    [JsonPropertyName("user_avatar")]
    public string? UserAvatar { get; set; }

    [JsonPropertyName("votes")]
    public int Votes { get; set; }

    [JsonPropertyName("photos")]
    public string? Photos { get; set; }

    [JsonPropertyName("video_url")]
    public string? VideoUrl { get; set; }

    [JsonPropertyName("hours_expiration")]
    public int HoursExpiration { get; set; }

    [JsonPropertyName("date_added")]
    public long DateAdded { get; set; }

    [JsonPropertyName("date_approved")]
    public long DateApproved { get; set; }

    [JsonPropertyName("date_expiration")]
    public long DateExpiration { get; set; }

    // Computed properties
    public DateTime DateAddedUtc => DateTimeOffset.FromUnixTimeSeconds(DateAdded).UtcDateTime;
    public DateTime DateExpirationUtc => DateTimeOffset.FromUnixTimeSeconds(DateExpiration).UtcDateTime;
    public bool IsBuyOrder => Operation.Equals("buy", StringComparison.OrdinalIgnoreCase);
    public bool IsSellOrder => Operation.Equals("sell", StringComparison.OrdinalIgnoreCase);
    public bool HasPriceChanged => Math.Abs(PriceOld) > 0.001 && Math.Abs(Price - PriceOld) > 0.001;
    public double PriceChangePercent => PriceOld > 0 ? ((Price - PriceOld) / PriceOld) * 100 : 0;

    public string FormattedPrice => $"{Price:N0} {Currency}";
    public string FormattedPricePerUnit => Unit != null ? $"{Price:N0} {Currency}/{Unit}" : FormattedPrice;
    public string OperationDisplay => IsBuyOrder ? "🟢 BUY" : "🔴 SELL";
    public string StockDisplay => IsSoldOut == 1 ? "SOLD OUT" : $"{InStock}";
}
