using System.IO;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Serilog;
using StarCitizenTrader.Models;

namespace StarCitizenTrader.Services;

/// SQLite-backed local cache and persistence for the trading application.
public class DatabaseService : IDatabaseService
{
    private readonly string _connectionString;
    private SqliteConnection? _connection;

    public DatabaseService(string dbPath)
    {
        var fullPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "StarCitizenTrader", dbPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        _connectionString = $"Data Source={fullPath}";
    }

    private async Task<SqliteConnection> GetConnectionAsync()
    {
        if (_connection == null || _connection.State != System.Data.ConnectionState.Open)
        {
            _connection = new SqliteConnection(_connectionString);
            await _connection.OpenAsync();
        }
        return _connection;
    }

    public async Task InitializeAsync()
    {
        var conn = await GetConnectionAsync();
        var sql = @"
            CREATE TABLE IF NOT EXISTS listings_cache (
                id INTEGER PRIMARY KEY,
                data TEXT NOT NULL,
                cached_at TEXT NOT NULL DEFAULT (datetime('now'))
            );

            CREATE TABLE IF NOT EXISTS trends_cache (
                id_item INTEGER PRIMARY KEY,
                data TEXT NOT NULL,
                cached_at TEXT NOT NULL DEFAULT (datetime('now'))
            );

            CREATE TABLE IF NOT EXISTS price_history (
                id INTEGER PRIMARY KEY,
                id_item INTEGER NOT NULL,
                data TEXT NOT NULL,
                cached_at TEXT NOT NULL DEFAULT (datetime('now'))
            );

            CREATE TABLE IF NOT EXISTS wishlist (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                id_item INTEGER NOT NULL,
                item_name TEXT NOT NULL,
                operation TEXT NOT NULL DEFAULT 'buy',
                max_price REAL,
                min_price REAL,
                currency TEXT NOT NULL DEFAULT 'UEC',
                notify_on_match INTEGER NOT NULL DEFAULT 1,
                is_active INTEGER NOT NULL DEFAULT 1,
                date_added TEXT NOT NULL DEFAULT (datetime('now')),
                last_match_date TEXT,
                match_count INTEGER NOT NULL DEFAULT 0,
                notes TEXT
            );

            CREATE TABLE IF NOT EXISTS wishlist_matches (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                wishlist_item_id INTEGER NOT NULL,
                listing_id INTEGER NOT NULL,
                listing_title TEXT NOT NULL,
                price REAL NOT NULL,
                currency TEXT NOT NULL DEFAULT 'UEC',
                seller_username TEXT NOT NULL,
                match_date TEXT NOT NULL DEFAULT (datetime('now')),
                was_notified INTEGER NOT NULL DEFAULT 0,
                UNIQUE(wishlist_item_id, listing_id)
            );

            CREATE TABLE IF NOT EXISTS notifications (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                title TEXT NOT NULL,
                message TEXT NOT NULL,
                type INTEGER NOT NULL DEFAULT 0,
                timestamp TEXT NOT NULL DEFAULT (datetime('now')),
                is_read INTEGER NOT NULL DEFAULT 0,
                related_listing_id INTEGER,
                related_wishlist_item_id INTEGER
            );

            CREATE TABLE IF NOT EXISTS settings (
                key TEXT PRIMARY KEY,
                value TEXT NOT NULL
            );

            CREATE INDEX IF NOT EXISTS idx_price_history_item ON price_history(id_item);
            CREATE INDEX IF NOT EXISTS idx_wishlist_active ON wishlist(is_active);
            CREATE INDEX IF NOT EXISTS idx_notifications_read ON notifications(is_read);
        ";

        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync();
        Log.Information("Database initialized successfully");
    }

    // ─── Listings Cache ────────────────────────────────────────────

    public async Task SaveListingsAsync(IEnumerable<MarketplaceListing> listings)
    {
        var conn = await GetConnectionAsync();
        using var tx = conn.BeginTransaction();

        using (var del = conn.CreateCommand())
        {
            del.CommandText = "DELETE FROM listings_cache";
            del.Transaction = tx;
            await del.ExecuteNonQueryAsync();
        }

        foreach (var listing in listings)
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = "INSERT OR REPLACE INTO listings_cache (id, data, cached_at) VALUES (@id, @data, datetime('now'))";
            cmd.Parameters.AddWithValue("@id", listing.Id);
            cmd.Parameters.AddWithValue("@data", JsonSerializer.Serialize(listing));
            await cmd.ExecuteNonQueryAsync();
        }

        tx.Commit();
    }

    public async Task<List<MarketplaceListing>> GetCachedListingsAsync()
    {
        var conn = await GetConnectionAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT data FROM listings_cache ORDER BY id DESC";

        var results = new List<MarketplaceListing>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var item = JsonSerializer.Deserialize<MarketplaceListing>(reader.GetString(0));
            if (item != null) results.Add(item);
        }
        return results;
    }

    public async Task<DateTime?> GetListingsCacheTimeAsync()
    {
        var conn = await GetConnectionAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT MAX(cached_at) FROM listings_cache";
        var result = await cmd.ExecuteScalarAsync();
        if (result is string dateStr && DateTime.TryParse(dateStr, out var dt))
            return dt;
        return null;
    }

    // ─── Trends Cache ──────────────────────────────────────────────

    public async Task SaveTrendsAsync(IEnumerable<MarketplaceTrend> trends)
    {
        var conn = await GetConnectionAsync();
        using var tx = conn.BeginTransaction();

        using (var del = conn.CreateCommand())
        {
            del.CommandText = "DELETE FROM trends_cache";
            del.Transaction = tx;
            await del.ExecuteNonQueryAsync();
        }

        foreach (var trend in trends)
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = "INSERT OR REPLACE INTO trends_cache (id_item, data, cached_at) VALUES (@id, @data, datetime('now'))";
            cmd.Parameters.AddWithValue("@id", trend.IdItem);
            cmd.Parameters.AddWithValue("@data", JsonSerializer.Serialize(trend));
            await cmd.ExecuteNonQueryAsync();
        }

        tx.Commit();
    }

    public async Task<List<MarketplaceTrend>> GetCachedTrendsAsync()
    {
        var conn = await GetConnectionAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT data FROM trends_cache ORDER BY id_item";

        var results = new List<MarketplaceTrend>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var item = JsonSerializer.Deserialize<MarketplaceTrend>(reader.GetString(0));
            if (item != null) results.Add(item);
        }
        return results;
    }

    // ─── Price History ─────────────────────────────────────────────

    public async Task SavePriceHistoryAsync(IEnumerable<PriceHistory> history)
    {
        var conn = await GetConnectionAsync();
        foreach (var item in history)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT OR REPLACE INTO price_history (id, id_item, data, cached_at) VALUES (@id, @idItem, @data, datetime('now'))";
            cmd.Parameters.AddWithValue("@id", item.Id);
            cmd.Parameters.AddWithValue("@idItem", item.IdItem);
            cmd.Parameters.AddWithValue("@data", JsonSerializer.Serialize(item));
            await cmd.ExecuteNonQueryAsync();
        }
    }

    public async Task<List<PriceHistory>> GetPriceHistoryAsync(int idItem)
    {
        var conn = await GetConnectionAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT data FROM price_history WHERE id_item = @idItem ORDER BY id DESC";
        cmd.Parameters.AddWithValue("@idItem", idItem);

        var results = new List<PriceHistory>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var item = JsonSerializer.Deserialize<PriceHistory>(reader.GetString(0));
            if (item != null) results.Add(item);
        }
        return results;
    }

    // ─── Wishlist ──────────────────────────────────────────────────

    public async Task<List<WishlistItem>> GetWishlistAsync()
    {
        var conn = await GetConnectionAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM wishlist ORDER BY date_added DESC";

        var results = new List<WishlistItem>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(new WishlistItem
            {
                Id = reader.GetInt32(reader.GetOrdinal("id")),
                IdItem = reader.GetInt32(reader.GetOrdinal("id_item")),
                ItemName = reader.GetString(reader.GetOrdinal("item_name")),
                Operation = reader.GetString(reader.GetOrdinal("operation")),
                MaxPrice = reader.IsDBNull(reader.GetOrdinal("max_price")) ? null : reader.GetDouble(reader.GetOrdinal("max_price")),
                MinPrice = reader.IsDBNull(reader.GetOrdinal("min_price")) ? null : reader.GetDouble(reader.GetOrdinal("min_price")),
                Currency = reader.GetString(reader.GetOrdinal("currency")),
                NotifyOnMatch = reader.GetInt32(reader.GetOrdinal("notify_on_match")) == 1,
                IsActive = reader.GetInt32(reader.GetOrdinal("is_active")) == 1,
                DateAdded = DateTime.Parse(reader.GetString(reader.GetOrdinal("date_added"))),
                LastMatchDate = reader.IsDBNull(reader.GetOrdinal("last_match_date")) ? null : DateTime.Parse(reader.GetString(reader.GetOrdinal("last_match_date"))),
                MatchCount = reader.GetInt32(reader.GetOrdinal("match_count")),
                Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? null : reader.GetString(reader.GetOrdinal("notes"))
            });
        }
        return results;
    }

    public async Task<int> AddWishlistItemAsync(WishlistItem item)
    {
        var conn = await GetConnectionAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"INSERT INTO wishlist (id_item, item_name, operation, max_price, min_price, currency, notify_on_match, is_active, notes)
                           VALUES (@idItem, @name, @op, @max, @min, @currency, @notify, @active, @notes);
                           SELECT last_insert_rowid();";
        cmd.Parameters.AddWithValue("@idItem", item.IdItem);
        cmd.Parameters.AddWithValue("@name", item.ItemName);
        cmd.Parameters.AddWithValue("@op", item.Operation);
        cmd.Parameters.AddWithValue("@max", item.MaxPrice.HasValue ? (object)item.MaxPrice.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@min", item.MinPrice.HasValue ? (object)item.MinPrice.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@currency", item.Currency);
        cmd.Parameters.AddWithValue("@notify", item.NotifyOnMatch ? 1 : 0);
        cmd.Parameters.AddWithValue("@active", item.IsActive ? 1 : 0);
        cmd.Parameters.AddWithValue("@notes", item.Notes != null ? (object)item.Notes : DBNull.Value);

        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    public async Task UpdateWishlistItemAsync(WishlistItem item)
    {
        var conn = await GetConnectionAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"UPDATE wishlist SET id_item=@idItem, item_name=@name, operation=@op,
                           max_price=@max, min_price=@min, currency=@currency, notify_on_match=@notify,
                           is_active=@active, last_match_date=@lastMatch, match_count=@matchCount, notes=@notes
                           WHERE id=@id";
        cmd.Parameters.AddWithValue("@id", item.Id);
        cmd.Parameters.AddWithValue("@idItem", item.IdItem);
        cmd.Parameters.AddWithValue("@name", item.ItemName);
        cmd.Parameters.AddWithValue("@op", item.Operation);
        cmd.Parameters.AddWithValue("@max", item.MaxPrice.HasValue ? (object)item.MaxPrice.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@min", item.MinPrice.HasValue ? (object)item.MinPrice.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@currency", item.Currency);
        cmd.Parameters.AddWithValue("@notify", item.NotifyOnMatch ? 1 : 0);
        cmd.Parameters.AddWithValue("@active", item.IsActive ? 1 : 0);
        cmd.Parameters.AddWithValue("@lastMatch", item.LastMatchDate.HasValue ? (object)item.LastMatchDate.Value.ToString("o") : DBNull.Value);
        cmd.Parameters.AddWithValue("@matchCount", item.MatchCount);
        cmd.Parameters.AddWithValue("@notes", item.Notes != null ? (object)item.Notes : DBNull.Value);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteWishlistItemAsync(int id)
    {
        var conn = await GetConnectionAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM wishlist WHERE id=@id";
        cmd.Parameters.AddWithValue("@id", id);
        await cmd.ExecuteNonQueryAsync();
    }

    // ─── Wishlist Matches ──────────────────────────────────────────

    public async Task SaveWishlistMatchAsync(WishlistMatch match)
    {
        var conn = await GetConnectionAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"INSERT OR IGNORE INTO wishlist_matches
            (wishlist_item_id, listing_id, listing_title, price, currency, seller_username, was_notified)
            VALUES (@wid, @lid, @title, @price, @currency, @seller, @notified)";
        cmd.Parameters.AddWithValue("@wid", match.WishlistItemId);
        cmd.Parameters.AddWithValue("@lid", match.ListingId);
        cmd.Parameters.AddWithValue("@title", match.ListingTitle);
        cmd.Parameters.AddWithValue("@price", match.Price);
        cmd.Parameters.AddWithValue("@currency", match.Currency);
        cmd.Parameters.AddWithValue("@seller", match.SellerUsername);
        cmd.Parameters.AddWithValue("@notified", match.WasNotified ? 1 : 0);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<List<WishlistMatch>> GetRecentMatchesAsync(int limit = 50)
    {
        var conn = await GetConnectionAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM wishlist_matches ORDER BY match_date DESC LIMIT @limit";
        cmd.Parameters.AddWithValue("@limit", limit);

        var results = new List<WishlistMatch>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(new WishlistMatch
            {
                Id = reader.GetInt32(reader.GetOrdinal("id")),
                WishlistItemId = reader.GetInt32(reader.GetOrdinal("wishlist_item_id")),
                ListingId = reader.GetInt32(reader.GetOrdinal("listing_id")),
                ListingTitle = reader.GetString(reader.GetOrdinal("listing_title")),
                Price = reader.GetDouble(reader.GetOrdinal("price")),
                Currency = reader.GetString(reader.GetOrdinal("currency")),
                SellerUsername = reader.GetString(reader.GetOrdinal("seller_username")),
                MatchDate = DateTime.Parse(reader.GetString(reader.GetOrdinal("match_date"))),
                WasNotified = reader.GetInt32(reader.GetOrdinal("was_notified")) == 1
            });
        }
        return results;
    }

    public async Task<bool> HasMatchBeenNotifiedAsync(int wishlistItemId, int listingId)
    {
        var conn = await GetConnectionAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM wishlist_matches WHERE wishlist_item_id=@wid AND listing_id=@lid";
        cmd.Parameters.AddWithValue("@wid", wishlistItemId);
        cmd.Parameters.AddWithValue("@lid", listingId);
        var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        return count > 0;
    }

    // ─── Notifications ─────────────────────────────────────────────

    public async Task SaveNotificationAsync(AppNotification notification)
    {
        var conn = await GetConnectionAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"INSERT INTO notifications (title, message, type, is_read, related_listing_id, related_wishlist_item_id)
                           VALUES (@title, @msg, @type, 0, @lid, @wid)";
        cmd.Parameters.AddWithValue("@title", notification.Title);
        cmd.Parameters.AddWithValue("@msg", notification.Message);
        cmd.Parameters.AddWithValue("@type", (int)notification.Type);
        cmd.Parameters.AddWithValue("@lid", notification.RelatedListingId.HasValue ? (object)notification.RelatedListingId.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@wid", notification.RelatedWishlistItemId.HasValue ? (object)notification.RelatedWishlistItemId.Value : DBNull.Value);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<List<AppNotification>> GetNotificationsAsync(int limit = 100)
    {
        var conn = await GetConnectionAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM notifications ORDER BY timestamp DESC LIMIT @limit";
        cmd.Parameters.AddWithValue("@limit", limit);

        var results = new List<AppNotification>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(new AppNotification
            {
                Id = reader.GetInt32(reader.GetOrdinal("id")),
                Title = reader.GetString(reader.GetOrdinal("title")),
                Message = reader.GetString(reader.GetOrdinal("message")),
                Type = (NotificationType)reader.GetInt32(reader.GetOrdinal("type")),
                Timestamp = DateTime.Parse(reader.GetString(reader.GetOrdinal("timestamp"))),
                IsRead = reader.GetInt32(reader.GetOrdinal("is_read")) == 1,
                RelatedListingId = reader.IsDBNull(reader.GetOrdinal("related_listing_id")) ? null : reader.GetInt32(reader.GetOrdinal("related_listing_id")),
                RelatedWishlistItemId = reader.IsDBNull(reader.GetOrdinal("related_wishlist_item_id")) ? null : reader.GetInt32(reader.GetOrdinal("related_wishlist_item_id"))
            });
        }
        return results;
    }

    public async Task MarkNotificationReadAsync(int id)
    {
        var conn = await GetConnectionAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE notifications SET is_read=1 WHERE id=@id";
        cmd.Parameters.AddWithValue("@id", id);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task ClearNotificationsAsync()
    {
        var conn = await GetConnectionAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM notifications";
        await cmd.ExecuteNonQueryAsync();
    }

    // ─── Settings ──────────────────────────────────────────────────

    public async Task SaveSettingAsync(string key, string value)
    {
        var conn = await GetConnectionAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT OR REPLACE INTO settings (key, value) VALUES (@key, @val)";
        cmd.Parameters.AddWithValue("@key", key);
        cmd.Parameters.AddWithValue("@val", value);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<string?> GetSettingAsync(string key)
    {
        var conn = await GetConnectionAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT value FROM settings WHERE key=@key";
        cmd.Parameters.AddWithValue("@key", key);
        var result = await cmd.ExecuteScalarAsync();
        return result as string;
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }
}
