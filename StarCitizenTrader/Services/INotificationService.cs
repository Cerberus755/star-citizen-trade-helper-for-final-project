using StarCitizenTrader.Models;

namespace StarCitizenTrader.Services;

/// Interface for in-app and Windows toast notification delivery.
public interface INotificationService
{
    /// Fires when a new in-app notification is created.
    event EventHandler<AppNotification>? NotificationReceived;

    /// Send an in-app notification (stored in DB + shown in UI).
    Task SendInAppNotificationAsync(string title, string message, NotificationType type,
        int? relatedListingId = null, int? relatedWishlistItemId = null);

    /// Send a Windows toast notification (native OS popup).
    void SendToastNotification(string title, string message);

    /// Send both in-app and toast notification.
    Task SendNotificationAsync(string title, string message, NotificationType type,
        int? relatedListingId = null, int? relatedWishlistItemId = null);

    /// Get recent notifications from storage.
    Task<List<AppNotification>> GetRecentNotificationsAsync(int limit = 100);

    /// Mark a notification as read.
    Task MarkReadAsync(int id);

    /// Clear all notifications.
    Task ClearAllAsync();

    /// Count of unread notifications.
    int UnreadCount { get; }
}
