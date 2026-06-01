using System.Text.Json.Serialization;

namespace StarCitizenTrader.Models;

/// Negotiation deal from GET /marketplace_negotiations.
public class Negotiation
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("id_listing")]
    public int IdListing { get; set; }

    [JsonPropertyName("hash")]
    public string Hash { get; set; } = string.Empty;

    [JsonPropertyName("price")]
    public double Price { get; set; }

    [JsonPropertyName("unit")]
    public string Unit { get; set; } = string.Empty;

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "UEC";

    [JsonPropertyName("deal_value")]
    public double? DealValue { get; set; }

    [JsonPropertyName("deal_value_currency")]
    public string? DealValueCurrency { get; set; }

    [JsonPropertyName("listing_title")]
    public string ListingTitle { get; set; } = string.Empty;

    [JsonPropertyName("listing_slug")]
    public string ListingSlug { get; set; } = string.Empty;

    [JsonPropertyName("advertiser_name")]
    public string AdvertiserName { get; set; } = string.Empty;

    [JsonPropertyName("advertiser_username")]
    public string AdvertiserUsername { get; set; } = string.Empty;

    [JsonPropertyName("advertiser_avatar")]
    public string? AdvertiserAvatar { get; set; }

    [JsonPropertyName("client_name")]
    public string ClientName { get; set; } = string.Empty;

    [JsonPropertyName("client_username")]
    public string ClientUsername { get; set; } = string.Empty;

    [JsonPropertyName("client_avatar")]
    public string? ClientAvatar { get; set; }

    [JsonPropertyName("is_listing_advertiser")]
    public int IsListingAdvertiser { get; set; }

    [JsonPropertyName("date_added")]
    public long DateAdded { get; set; }

    [JsonPropertyName("date_modified")]
    public long DateModified { get; set; }

    [JsonPropertyName("date_closed")]
    public long DateClosed { get; set; }

    [JsonPropertyName("date_closed_client")]
    public long DateClosedClient { get; set; }

    // Computed
    public DateTime DateAddedUtc => DateTimeOffset.FromUnixTimeSeconds(DateAdded).UtcDateTime;
    public bool IsClosed => DateClosed > 0;
    public bool IsAdvertiser => IsListingAdvertiser == 1;
    public string CounterpartyName => IsAdvertiser ? ClientName : AdvertiserName;
    public string CounterpartyUsername => IsAdvertiser ? ClientUsername : AdvertiserUsername;
    public string FormattedPrice => $"{Price:N0} {Currency}/{Unit}";

    public string StatusDisplay => IsClosed ? "✅ Closed" : "🔄 Open";
}

/// A single message within a negotiation from GET /marketplace_negotiations_messages.
public class NegotiationMessage
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("id_listing")]
    public int IdListing { get; set; }

    [JsonPropertyName("id_negotiation")]
    public int IdNegotiation { get; set; }

    [JsonPropertyName("event")]
    public string? Event { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("listing_title")]
    public string ListingTitle { get; set; } = string.Empty;

    [JsonPropertyName("listing_slug")]
    public string ListingSlug { get; set; } = string.Empty;

    [JsonPropertyName("negotiation_hash")]
    public string NegotiationHash { get; set; } = string.Empty;

    [JsonPropertyName("user_name")]
    public string UserName { get; set; } = string.Empty;

    [JsonPropertyName("user_username")]
    public string UserUsername { get; set; } = string.Empty;

    [JsonPropertyName("user_avatar")]
    public string? UserAvatar { get; set; }

    [JsonPropertyName("api_name")]
    public string ApiName { get; set; } = string.Empty;

    [JsonPropertyName("date_added")]
    public long DateAdded { get; set; }

    [JsonPropertyName("date_read")]
    public long DateRead { get; set; }

    // Computed
    public DateTime DateAddedUtc => DateTimeOffset.FromUnixTimeSeconds(DateAdded).UtcDateTime;
    public bool IsRead => DateRead > 0;
    public string TimeDisplay => DateAddedUtc.ToString("MMM dd, HH:mm");
}
