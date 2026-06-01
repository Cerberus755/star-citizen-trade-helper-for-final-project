using System.Text.Json.Serialization;

namespace StarCitizenTrader.Models;

/// Category reference data from GET /categories.
public class Category
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("section")]
    public string? Section { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("is_game_related")]
    public int IsGameRelated { get; set; }

    public override string ToString() => Name;
}

/// Item reference data from GET /items.
public class GameItem
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("id_category")]
    public int IdCategory { get; set; }

    [JsonPropertyName("id_company")]
    public int? IdCompany { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("slug")]
    public string Slug { get; set; } = string.Empty;

    [JsonPropertyName("uuid")]
    public string? Uuid { get; set; }

    [JsonPropertyName("size")]
    public string? Size { get; set; }

    [JsonPropertyName("color")]
    public string? Color { get; set; }

    [JsonPropertyName("quality")]
    public int? Quality { get; set; }

    [JsonPropertyName("is_commodity")]
    public int IsCommodity { get; set; }

    [JsonPropertyName("game_version")]
    public string? GameVersion { get; set; }

    public override string ToString() => Name;
}

/// Star system reference data.
public class StarSystem
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    public override string ToString() => Name;
}
