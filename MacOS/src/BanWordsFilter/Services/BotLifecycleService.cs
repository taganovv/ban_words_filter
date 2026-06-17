using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BanWordsFilter.Models;

namespace BanWordsFilter.Services;

public sealed class BotLifecycleService : IDisposable
{
    private const int MaxLogs = 500;

    private readonly SettingsService _settings = new();
    private readonly TokenValidationService _tokenValidation = new();
    private readonly MessageFilterService _filter = new();
    private readonly object _logLock = new();
    private readonly Queue<string> _logs = new();

    private TwitchBotService? _bot;

    public bool IsRunning => _bot?.IsConnected ?? false;

    public Task<Dictionary<string, string>> GetSettingsAsync()
        => Task.FromResult(_settings.LoadDictionary());

    public Task<(bool Ok, string Message)> SaveSettingsAsync(Dictionary<string, string> values)
    {
        _settings.Save(values);
        AppendLog("[GUI] Настройки сохранены");
        return Task.FromResult((true, "Сохранено"));
    }

    public async Task<(bool Ok, string Message, TokenInfo? Info)> ValidateTokenAsync(Dictionary<string, string> values)
    {
        try
        {
            var settings = SettingsService.FromDictionary(values);
            var info = await _tokenValidation.ValidateAsync(settings.TwitchToken, settings.TwitchClientId);
            return (true, "OK", info);
        }
        catch (Exception ex)
        {
            return (false, ex.Message, null);
        }
    }

    public async Task<(bool Ok, string Message)> StartAsync(Dictionary<string, string> values)
    {
        if (IsRunning)
            return (true, "Бот уже запущен");

        var settings = SettingsService.FromDictionary(values);
        var missing = _settings.ValidateRequired(settings);
        if (missing.Count > 0)
            return (false, $"Заполните поля: {string.Join(", ", missing)}");

        _settings.Save(settings);
        settings = _settings.Load();

        try
        {
            _bot?.Dispose();
            _bot = new TwitchBotService(_filter, AppendLog);
            await _bot.StartAsync(settings);
            AppendLog("[GUI] Бот запущен");
            return (true, "Бот запущен");
        }
        catch (Exception ex)
        {
            _bot?.Dispose();
            _bot = null;
            AppendLog($"[ERROR] {ex.Message}");
            return (false, ex.Message);
        }
    }

    public Task<(bool Ok, string Message)> StopAsync()
    {
        if (_bot is null)
            return Task.FromResult((true, "Бот уже остановлен"));

        _bot.Dispose();
        _bot = null;
        AppendLog("[GUI] Бот остановлен");
        return Task.FromResult((true, "Бот остановлен"));
    }

    public Task<(bool Running, string Status)> GetStatusAsync()
        => Task.FromResult((IsRunning, IsRunning ? "running" : "stopped"));

    public Task<IReadOnlyList<string>> GetLogsAsync()
    {
        lock (_logLock)
            return Task.FromResult<IReadOnlyList<string>>(_logs.ToList());
    }

    public Task<FilterResult?> TestFilterAsync(string text)
        => Task.FromResult<FilterResult?>(_filter.CheckMessage(text));

    public Task<(bool Ok, string Message)> SendTestChatAsync()
    {
        if (!IsRunning)
            return Task.FromResult((false, "Бот не запущен"));

        try
        {
            _bot!.SendChatMessage(AppConstants.ChatTestMessage);
            return Task.FromResult((true, "Сообщение отправлено в чат"));
        }
        catch (Exception ex)
        {
            return Task.FromResult((false, ex.Message));
        }
    }

    public async Task<(bool Ok, string Message)> ClearAllAsync()
    {
        await StopAsync();
        _settings.ClearUserData();

        lock (_logLock)
            _logs.Clear();

        AppendLog("[GUI] Локальные данные пользователя удалены");
        return (true, "Все данные удалены");
    }

    private void AppendLog(string line)
    {
        var entry = $"{DateTime.Now:HH:mm:ss} {line}";
        lock (_logLock)
        {
            _logs.Enqueue(entry);
            while (_logs.Count > MaxLogs)
                _logs.Dequeue();
        }
    }

    public void Dispose()
    {
        _bot?.Dispose();
        _bot = null;
    }
}
