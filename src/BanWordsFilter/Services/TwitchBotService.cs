using System;
using System.Threading;
using System.Threading.Tasks;
using BanWordsFilter.Models;
using TwitchLib.Api;
using TwitchLib.Api.Helix.Models.Moderation.BanUser;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;

namespace BanWordsFilter.Services;

public sealed class TwitchBotService : IDisposable
{
    private readonly MessageFilterService _filter;
    private readonly BanHistoryService _banHistory;
    private readonly Action<string> _log;

    private TwitchClient? _client;
    private TwitchAPI? _api;
    private BotSettings? _settings;
    private string _broadcasterId = "";
    private string _channel = "";

    public TwitchBotService(MessageFilterService filter, BanHistoryService banHistory, Action<string> log)
    {
        _filter = filter;
        _banHistory = banHistory;
        _log = log;
    }

    public bool IsConnected => _client?.IsConnected ?? false;

    public async Task StartAsync(BotSettings settings, CancellationToken cancellationToken = default)
    {
        _settings = settings;
        var token = SettingsService.NormalizeOAuthToken(settings.TwitchToken);
        _channel = settings.TwitchChannel.Trim().TrimStart('#').ToLowerInvariant();
        var botName = settings.TwitchBotName.Trim().ToLowerInvariant();

        _api = new TwitchAPI();
        _api.Settings.ClientId = settings.TwitchClientId.Trim();
        _api.Settings.AccessToken = token;
        _api.Settings.Secret = settings.TwitchClientSecret.Trim();

        var users = await _api.Helix.Users.GetUsersAsync(logins: [_channel]);
        if (users.Users.Length == 0)
            throw new InvalidOperationException($"Канал #{_channel} не найден на Twitch.");

        _broadcasterId = users.Users[0].Id;

        var credentials = new ConnectionCredentials(botName, token);
        _client = new TwitchClient();
        _client.OnConnected += (_, _) =>
            _log($"Filter online as streamer: {botName} | channel: #{_channel}");
        _client.OnMessageReceived += OnMessageReceived;
        _client.OnLog += (_, e) => _log(e.Data);
        _client.Initialize(credentials, _channel);

        await Task.Run(() => _client.Connect(), cancellationToken);
    }

    public void SendChatMessage(string message)
    {
        if (_client is null || !_client.IsConnected)
            throw new InvalidOperationException("Бот не подключён к чату.");

        _client.SendMessage(_channel, message);
        _log($"Chat: {message}");
    }

    public void Stop()
    {
        if (_client is { IsConnected: true })
            _client.Disconnect();
    }

    private async void OnMessageReceived(object? sender, OnMessageReceivedArgs e)
    {
        try
        {
            if (_settings is null || _api is null)
                return;

            var message = e.ChatMessage;
            if (message.IsModerator || message.IsBroadcaster)
                return;

            if (string.Equals(message.Username, _settings.TwitchBotName, StringComparison.OrdinalIgnoreCase))
                return;

            var content = message.Message ?? "";
            var result = _filter.CheckMessage(content);
            if (!result.Banned || result.TopMatch is null)
                return;

            var match = result.TopMatch;
            var reason = _filter.FormatModerationReason(match);
            var userId = message.UserId;

            if (string.IsNullOrWhiteSpace(userId))
            {
                _log($"Cannot moderate {message.Username}: user id unknown");
                return;
            }

            if (match.Action == "timeout_or_ban" && _settings.TimeoutSecondsOrDefault() > 0)
            {
                await _api.Helix.Moderation.BanUserAsync(
                    _broadcasterId,
                    _settings.TwitchBotId,
                    new BanUserRequest
                    {
                        UserId = userId,
                        Reason = reason,
                        Duration = _settings.TimeoutSecondsOrDefault(),
                    });
                RecordBan(message.Username, userId, reason, match, "timeout");
                _log($"TIMEOUT {_settings.TimeoutSecondsOrDefault()}s: {message.Username} | matched: {match.Pattern}");
            }
            else
            {
                await _api.Helix.Moderation.BanUserAsync(
                    _broadcasterId,
                    _settings.TwitchBotId,
                    new BanUserRequest
                    {
                        UserId = userId,
                        Reason = reason,
                    });
                RecordBan(message.Username, userId, reason, match, "ban");
                _log($"BAN: {message.Username} | matched: {match.Pattern}");
            }
        }
        catch (Exception ex)
        {
            _log($"[ERROR] Message handler: {ex.Message}");
        }
    }

    public async Task UnbanUserAsync(string userId)
    {
        if (_api is null || _settings is null || string.IsNullOrWhiteSpace(_broadcasterId))
            throw new InvalidOperationException("Бот не подключён");

        await _api.Helix.Moderation.UnbanUserAsync(_broadcasterId, _settings.TwitchBotId, userId);
    }

    private void RecordBan(string username, string userId, string reason, FilterMatch match, string action)
    {
        _banHistory.Record(new BanRecord
        {
            Username = username,
            UserId = userId,
            Reason = reason,
            Pattern = match.Pattern ?? "",
            Category = match.CategoryLabel ?? match.Category ?? "",
            Action = action,
            BannedAt = DateTime.Now,
        });
    }

    public void Dispose()
    {
        Stop();
        _client = null;
        _api = null;
    }
}
