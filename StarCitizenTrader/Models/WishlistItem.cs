namespace StarCitizenTrader.Models;

/// Local wishlist item stored in SQLite — tracks items the user wants to buy/sell.
public class WishlistItem
{
    public int Id { get; set; }
    public int IdItem { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string Operation { get; set; } = "buy";  // "buy" or "sell"
    public double? MaxPrice { get; set; }            // Max price willing to pay (buy) or min price to sell
    public double? MinPrice { get; set; }
    public string Currency { get; set; } = "UEC";
    public bool NotifyOnMatch { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public DateTime DateAdded { get; set; } = DateTime.UtcNow;
    public DateTime? LastMatchDate { get; set; }
    public int MatchCount { get; set; }
    public string? Notes { get; set; }

    // Display helpers
    public string PriceRangeDisplay
    {
        get
        {
            if (MaxPrice.HasValue && MinPrice.HasValue)
                return $"{MinPrice.Value:N0} – {MaxPrice.Value:N0} {Currency}";
            if (MaxPrice.HasValue)
                return $"≤ {MaxPrice.Value:N0} {Currency}";
            if (MinPrice.HasValue)
                return $"≥ {MinPrice.Value:N0} {Currency}";
            return "Any price";
        }
    }

    public string OperationDisplay => Operation.Equals("buy", StringComparison.OrdinalIgnoreCase)
        ? "🟢 Want to Buy" : "🔴 Want to Sell";

    public string StatusDisplay => IsActive ? "Active" : "Paused";
}

/// Tracks when a wishlist item was matched by a listing.
public class WishlistMatch
{
    public int Id { get; set; }
    public int WishlistItemId { get; set; }
    public int ListingId { get; set; }
    public string ListingTitle { get; set; } = string.Empty;
    public double Price { get; set; }
    public string Currency { get; set; } = "UEC";
    public string SellerUsername { get; set; } = string.Empty;
    public DateTime MatchDate { get; set; } = DateTime.UtcNow;
    public bool WasNotified { get; set; }
}
