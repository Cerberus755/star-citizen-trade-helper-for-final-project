namespace StarCitizenTrader.Models;

/// Application configuration bound from appsettings.json.
public class AppSettings
{
    public UexApiSettings UexApi { get; set; } = new();
    public PollingSettings Polling { get; set; } = new();
    public NotificationSettings Notifications { get; set; } = new();
    public string DatabasePath { get; set; } = "star_citizen_trader.db";
}

public class UexApiSettings
{
    public string BaseUrl { get; set; } = "https://api.uexcorp.uk/2.0/";
    public string BearerToken { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string ClientVersion { get; set; } = "1.0.0";
    public int RateLimitPerMinute { get; set; } = 120;
}

public class PollingSettings
{
    public int ListingsIntervalSeconds { get; set; } = 120;
    public int TrendsIntervalSeconds { get; set; } = 3600;
    public int WishlistCheckIntervalSeconds { get; set; } = 180;
    public bool EnableAutoRefresh { get; set; } = true;
}

public class NotificationSettings
{
    public bool EnableInAppNotifications { get; set; } = true;
    public bool EnableToastNotifications { get; set; } = true;
    public bool EnableWishlistAlerts { get; set; } = true;
    public bool PlaySound { get; set; } = false;
}

/// In-app notification displayed in the notification panel.
public class AppNotification
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; } = NotificationType.Info;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; }
    public int? RelatedListingId { get; set; }
    public int? RelatedWishlistItemId { get; set; }

    public string TypeIcon => Type switch
    {
        NotificationType.WishlistMatch => "⭐",
        NotificationType.PriceAlert => "💰",
        NotificationType.NewListing => "📦",
        NotificationType.NegotiationUpdate => "💬",
        NotificationType.Warning => "⚠️",
        NotificationType.Error => "❌",
        _ => "ℹ️"
    };

    public string TimeAgo
    {
        get
        {
            var diff = DateTime.UtcNow - Timestamp;
            if (diff.TotalMinutes < 1) return "just now";
            if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m ago";
            if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h ago";
            return $"{(int)diff.TotalDays}d ago";
        }
    }
}

public enum NotificationType
{
    Info,
    WishlistMatch,
    PriceAlert,
    NewListing,
    NegotiationUpdate,
    Warning,
    Error
}
