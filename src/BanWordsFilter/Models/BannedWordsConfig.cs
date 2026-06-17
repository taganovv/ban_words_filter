using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BanWordsFilter.Models;

public sealed class BannedWordsConfig
{
    [JsonPropertyName("meta")]
    public BannedWordsMeta Meta { get; set; } = new();

    [JsonPropertyName("normalization")]
    public NormalizationConfig Normalization { get; set; } = new();

    [JsonPropertyName("categories")]
    public Dictionary<string, CategoryConfig> Categories { get; set; } = new();

    [JsonPropertyName("whitelist")]
    public WhitelistConfig Whitelist { get; set; } = new();
}

public sealed class BannedWordsMeta
{
    [JsonPropertyName("action")]
    public string Action { get; set; } = "ban";
}

public sealed class NormalizationConfig
{
    [JsonPropertyName("strip_zero_width")]
    public bool StripZeroWidth { get; set; } = true;

    [JsonPropertyName("collapse_repeated_chars_max")]
    public int CollapseRepeatedCharsMax { get; set; } = 3;

    [JsonPropertyName("replace_leetspeak")]
    public Dictionary<string, string> ReplaceLeetspeak { get; set; } = new();

    [JsonPropertyName("homoglyph_map")]
    public Dictionary<string, string> HomoglyphMap { get; set; } = new();

    [JsonPropertyName("remove_chars")]
    public List<string> RemoveChars { get; set; } = [];
}

public sealed class CategoryConfig
{
    [JsonPropertyName("label")]
    public string? Label { get; set; }

    [JsonPropertyName("severity")]
    public string? Severity { get; set; }

    [JsonPropertyName("action")]
    public string? Action { get; set; }

    [JsonPropertyName("exact")]
    public List<string> Exact { get; set; } = [];

    [JsonPropertyName("regex")]
    public List<string> Regex { get; set; } = [];
}

public sealed class WhitelistConfig
{
    [JsonPropertyName("exact")]
    public List<string> Exact { get; set; } = [];
}
