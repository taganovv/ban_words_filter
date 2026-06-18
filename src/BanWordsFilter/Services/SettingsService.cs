using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using BanWordsFilter.Models;

namespace BanWordsFilter.Services;

public sealed class SettingsService
{
    public static readonly IReadOnlyList<string> FieldKeys =
    [
        "TWITCH_TOKEN",
        "TWITCH_CLIENT_ID",
        "TWITCH_CLIENT_SECRET",
        "TWITCH_BOT_NAME",
        "TWITCH_BOT_ID",
        "TWITCH_CHANNEL",
    ];

    private static string ConfigPath => Path.Combine(BotDirectory.DataDirectory(), "config.json");

    public Dictionary<string, string> LoadDictionary()
    {
        var settings = Load();
        return FieldKeys.ToDictionary(key => key, key => GetFieldValue(settings, key));
    }

    public BotSettings Load()
    {
        if (!File.Exists(ConfigPath))
            return new BotSettings();

        var json = File.ReadAllText(ConfigPath);
        var data = JsonSerializer.Deserialize<Dictionary<string, string>>(json)
            ?? new Dictionary<string, string>();

        return FromDictionary(data);
    }

    public void Save(Dictionary<string, string> values)
    {
        Save(FromDictionary(values));
    }

    public void Save(BotSettings settings)
    {
        settings.TwitchToken = FormatOAuthToken(settings.TwitchToken);
        settings.TwitchBotName = settings.TwitchBotName.Trim().ToLowerInvariant();
        settings.TwitchChannel = settings.TwitchChannel.Trim().TrimStart('#').ToLowerInvariant();
        settings.TimeoutSeconds = "0";

        Directory.CreateDirectory(BotDirectory.DataDirectory());
        var data = ToDictionary(settings);
        File.WriteAllText(ConfigPath, JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
    }

    public IReadOnlyList<string> ValidateRequired(BotSettings settings)
    {
        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(NormalizeOAuthToken(settings.TwitchToken)))
            missing.Add("TWITCH_TOKEN");
        if (string.IsNullOrWhiteSpace(settings.TwitchClientId))
            missing.Add("TWITCH_CLIENT_ID");
        if (string.IsNullOrWhiteSpace(settings.TwitchClientSecret))
            missing.Add("TWITCH_CLIENT_SECRET");
        if (string.IsNullOrWhiteSpace(settings.TwitchBotName))
            missing.Add("TWITCH_BOT_NAME");
        if (string.IsNullOrWhiteSpace(settings.TwitchBotId))
            missing.Add("TWITCH_BOT_ID");
        if (string.IsNullOrWhiteSpace(settings.TwitchChannel))
            missing.Add("TWITCH_CHANNEL");
        return missing;
    }

    public void ClearUserData()
    {
        var dataDir = BotDirectory.DataDirectory();
        foreach (var file in new[] { "config.json", "user-word-lists.json", "ban-history.json", "startup.log", "install.log", ".deps_ok" })
        {
            try { File.Delete(Path.Combine(dataDir, file)); } catch { }
        }

        var venv = Path.Combine(dataDir, ".venv");
        if (Directory.Exists(venv))
        {
            try { Directory.Delete(venv, true); } catch { }
        }
    }

    public static string NormalizeOAuthToken(string raw)
    {
        var token = (raw ?? "").Trim();
        if (token.Length == 0)
            return "";

        var match = Regex.Match(token, @"access_token=([^&\s#]+)");
        if (match.Success)
            token = Uri.UnescapeDataString(match.Groups[1].Value);

        if (token.StartsWith("oauth:", StringComparison.OrdinalIgnoreCase))
            token = token[6..];

        return token.Trim();
    }

    public static string FormatOAuthToken(string raw)
    {
        var token = NormalizeOAuthToken(raw);
        return token.Length == 0 ? "" : $"oauth:{token}";
    }

    public static BotSettings FromDictionary(Dictionary<string, string> values)
        => new()
        {
            TwitchToken = values.GetValueOrDefault("TWITCH_TOKEN", ""),
            TwitchClientId = values.GetValueOrDefault("TWITCH_CLIENT_ID", ""),
            TwitchClientSecret = values.GetValueOrDefault("TWITCH_CLIENT_SECRET", ""),
            TwitchBotName = values.GetValueOrDefault("TWITCH_BOT_NAME", ""),
            TwitchBotId = values.GetValueOrDefault("TWITCH_BOT_ID", ""),
            TwitchChannel = values.GetValueOrDefault("TWITCH_CHANNEL", ""),
            TimeoutSeconds = "0",
        };

    public static Dictionary<string, string> ToDictionary(BotSettings settings)
        => new()
        {
            ["TWITCH_TOKEN"] = settings.TwitchToken,
            ["TWITCH_CLIENT_ID"] = settings.TwitchClientId,
            ["TWITCH_CLIENT_SECRET"] = settings.TwitchClientSecret,
            ["TWITCH_BOT_NAME"] = settings.TwitchBotName,
            ["TWITCH_BOT_ID"] = settings.TwitchBotId,
            ["TWITCH_CHANNEL"] = settings.TwitchChannel,
        };

    private static string GetFieldValue(BotSettings settings, string key)
        => ToDictionary(settings).GetValueOrDefault(key, "");
}
