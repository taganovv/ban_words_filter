using System;
using System.Collections.Generic;

namespace BanWordsFilter.Models;

public sealed class TokenInfo
{
    public string? Login { get; init; }
    public string? UserId { get; init; }
    public bool Valid { get; init; }
    public IReadOnlyList<string>? MissingScopes { get; init; }
}

public sealed class FilterResult
{
    public bool Banned { get; init; }
    public string? Action { get; init; }
    public FilterMatch? TopMatch { get; init; }
    public IReadOnlyList<FilterMatch> Matches { get; init; } = [];
}

public sealed class FilterMatch
{
    public string? Category { get; init; }
    public string? CategoryLabel { get; init; }
    public string? Pattern { get; init; }
    public string? Type { get; init; }
    public string? Action { get; init; }
    public string? Severity { get; init; }
}

public sealed class BotSettings
{
    public string TwitchToken { get; set; } = "";
    public string TwitchClientId { get; set; } = "";
    public string TwitchClientSecret { get; set; } = "";
    public string TwitchBotName { get; set; } = "";
    public string TwitchBotId { get; set; } = "";
    public string TwitchChannel { get; set; } = "";
}
