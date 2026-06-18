using System;
using System.Threading.Tasks;
using BanWordsFilter.Models;
using TwitchLib.Api;

namespace BanWordsFilter.Services;

internal static class TwitchModerationHelper
{
    public static async Task UnbanUserAsync(BotSettings settings, string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new InvalidOperationException("User ID не указан");

        var api = CreateApi(settings);
        var broadcasterId = await GetBroadcasterIdAsync(api, settings.TwitchChannel);
        await api.Helix.Moderation.UnbanUserAsync(broadcasterId, settings.TwitchBotId, userId);
    }

    public static async Task<string> GetBroadcasterIdAsync(TwitchAPI api, string channel)
    {
        var login = channel.Trim().TrimStart('#').ToLowerInvariant();
        var users = await api.Helix.Users.GetUsersAsync(logins: [login]);
        if (users.Users.Length == 0)
            throw new InvalidOperationException($"Канал #{login} не найден на Twitch.");

        return users.Users[0].Id;
    }

    public static TwitchAPI CreateApi(BotSettings settings)
    {
        var api = new TwitchAPI();
        api.Settings.ClientId = settings.TwitchClientId.Trim();
        api.Settings.AccessToken = SettingsService.NormalizeOAuthToken(settings.TwitchToken);
        api.Settings.Secret = settings.TwitchClientSecret.Trim();
        return api;
    }
}
