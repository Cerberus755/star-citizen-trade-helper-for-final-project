using System.Text.Json.Serialization;

namespace StarCitizenTrader.Models;

/// Generic wrapper for all UEX Corp API responses.
public class ApiResponse<T>
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public T? Data { get; set; }

    [JsonPropertyName("http_code")]
    public int? HttpCode { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    public bool IsSuccess => Status == "ok";
    public bool IsRateLimited => Status == "requests_limit_reached";
    public bool IsError => Status == "error";
}
