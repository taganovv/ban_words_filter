using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BanWordsFilter.Models;

public sealed class UserWordLists
{
    [JsonPropertyName("blacklist")]
    public UserListSection Blacklist { get; set; } = new();

    [JsonPropertyName("whitelist")]
    public UserListSection Whitelist { get; set; } = new();

    [JsonPropertyName("removed")]
    public RemovedWordLists Removed { get; set; } = new();

    [JsonPropertyName("blacklist_regex_links")]
    public Dictionary<string, string> BlacklistRegexLinks { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class UserListSection
{
    [JsonPropertyName("exact")]
    public List<string> Exact { get; set; } = [];

    [JsonPropertyName("regex")]
    public List<string> Regex { get; set; } = [];
}

public sealed class RemovedWordLists
{
    [JsonPropertyName("blacklist_exact")]
    public List<string> BlacklistExact { get; set; } = [];

    [JsonPropertyName("blacklist_regex")]
    public List<string> BlacklistRegex { get; set; } = [];

    [JsonPropertyName("whitelist_exact")]
    public List<string> WhitelistExact { get; set; } = [];

    [JsonPropertyName("blacklist_regex_all")]
    public bool BlacklistRegexAll { get; set; }
}

public sealed class WordListEntry
{
    public string Text { get; set; } = "";
    public string Type { get; set; } = "exact";
    public string Category { get; set; } = "";
    public bool IsUserAdded { get; set; }
    public string? LinkedWord { get; set; }
}
