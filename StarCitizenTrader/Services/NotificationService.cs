using Microsoft.Toolkit.Uwp.Notifications;
using Serilog;
using StarCitizenTrader.Models;

namespace StarCitizenTrader.Services;

/// Handles both in-app notifications and Windows 10/11 toast notifications.
public class NotificationService : INotificationService
{
    private readonly IDatabaseService _db;
    private readonly NotificationSettings _settings;
    private int _unreadCount;

    public event EventHandler<AppNotification>? NotificationReceived;
    public int UnreadCount => _unreadCount;

    public NotificationService(IDatabaseService db, NotificationSettings settings)
    {
        _db = db;
        _settings = settings;
    }

    public async Task SendInAppNotificationAsync(string title, string message, NotificationType type,
        int? relatedListingId = null, int? relatedWishlistItemId = null)
    {
        if (!_settings.EnableInAppNotifications) return;

        var notification = new AppNotification
        {
            Title = title,
            Message = message,
            Type = type,
            RelatedListingId = relatedListingId,
            RelatedWishlistItemId = relatedWishlistItemId,
            Timestamp = DateTime.UtcNow
        };

        try
        {
            await _db.SaveNotificationAsync(notification);
            _unreadCount++;
            NotificationReceived?.Invoke(this, notification);
            Log.Debug("In-app notification sent: {Title}", title);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to save in-app notification");
        }
    }

    public void SendToastNotification(string title, string message)
    {
        if (!_settings.EnableToastNotifications) return;

        try
        {
            new ToastContentBuilder()
                .AddText(title)
                .AddText(message)
                .AddAttributionText("Star Citizen Trader")
                .Show();

            Log.Debug("Toast notification sent: {Title}", title);
        }
        catch (Exception ex)
        {
            // Toast notifications may fail on systems without support
            Log.Warning(ex, "Failed to send toast notification (may not be supported on this system)");
        }
    }

    public async Task SendNotificationAsync(string title, string message, NotificationType type,
        int? relatedListingId = null, int? relatedWishlistItemId = null)
    {
        await SendInAppNotificationAsync(title, message, type, relatedListingId, relatedWishlistItemId);
        SendToastNotification(title, message);
    }

    public async Task<List<AppNotification>> GetRecentNotificationsAsync(int limit = 100)
    {
        var notifications = await _db.GetNotificationsAsync(limit);
        _unreadCount = notifications.Count(n => !n.IsRead);
        return notifications;
    }

    public async Task MarkReadAsync(int id)
    {
        await _db.MarkNotificationReadAsync(id);
        _unreadCount = Math.Max(0, _unreadCount - 1);
    }

    public async Task ClearAllAsync()
    {
        await _db.ClearNotificationsAsync();
        _unreadCount = 0;
    }
}
