using System;
using System.Text.Json.Serialization;

namespace BanWordsFilter.Models;

public sealed class BanRecord
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    [JsonPropertyName("username")]
    public string Username { get; set; } = "";

    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = "";

    [JsonPropertyName("reason")]
    public string Reason { get; set; } = "";

    [JsonPropertyName("pattern")]
    public string Pattern { get; set; } = "";

    [JsonPropertyName("category")]
    public string Category { get; set; } = "";

    [JsonPropertyName("action")]
    public string Action { get; set; } = "ban";

    [JsonPropertyName("banned_at")]
    public DateTime BannedAt { get; set; } = DateTime.Now;

    [JsonPropertyName("unbanned")]
    public bool Unbanned { get; set; }

    [JsonPropertyName("unbanned_at")]
    public DateTime? UnbannedAt { get; set; }

    public string BannedAtText => BannedAt.ToString("HH:mm:ss");

    public string ActionLabel => Action == "timeout" ? "Timeout" : "Ban";
}
