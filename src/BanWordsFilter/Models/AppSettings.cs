using System.Collections.Generic;

namespace BanWordsFilter.Models;

public static class AppSettings
{
    public static readonly IReadOnlyList<string> Fields =
    [
        "TWITCH_TOKEN",
        "TWITCH_CLIENT_ID",
        "TWITCH_CLIENT_SECRET",
        "TWITCH_BOT_NAME",
        "TWITCH_BOT_ID",
        "TWITCH_CHANNEL",
    ];

    public static readonly IReadOnlyDictionary<string, string> Labels = new Dictionary<string, string>
    {
        ["TWITCH_TOKEN"] = "OAuth Token",
        ["TWITCH_CLIENT_ID"] = "Client ID",
        ["TWITCH_CLIENT_SECRET"] = "Client Secret",
        ["TWITCH_BOT_NAME"] = "Ваш логин Twitch",
        ["TWITCH_BOT_ID"] = "Streamer User ID",
        ["TWITCH_CHANNEL"] = "Модерируемый канал",
    };

    public static readonly HashSet<string> SecretFields =
    [
        "TWITCH_TOKEN",
        "TWITCH_CLIENT_ID",
        "TWITCH_CLIENT_SECRET",
    ];
}
