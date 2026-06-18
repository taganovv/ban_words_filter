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
    private readonly BannedWordsConfigService _wordLists = new();
    private readonly BanHistoryService _banHistory = new();
    private readonly MessageFilterService _filter;
    private readonly object _logLock = new();
    private readonly Queue<string> _logs = new();

    private TwitchBotService? _bot;
    private BannedWordsConfig _mergedConfig;
    private UserWordLists _userWordLists;

    public BotLifecycleService()
    {
        _mergedConfig = _wordLists.LoadMergedConfig();
        _userWordLists = _wordLists.LoadUserLists();
        _filter = new MessageFilterService(_mergedConfig);
    }

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
            _bot = new TwitchBotService(_filter, _banHistory, AppendLog);
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

    public Task<(IReadOnlyList<WordListEntry> Blacklist, IReadOnlyList<WordListEntry> Whitelist)> GetWordListsAsync()
    {
        RefreshMergedConfig();
        return Task.FromResult((
            _wordLists.GetBlacklistEntries(_mergedConfig, _userWordLists),
            _wordLists.GetWhitelistEntries(_mergedConfig, _userWordLists)));
    }

    public Task<(bool Ok, string Message)> AddWordAsync(string word, bool autoRegex, bool isBlacklist)
    {
        RefreshMergedConfig();

        var (ok, message) = isBlacklist
            ? _wordLists.AddBlacklistWord(_userWordLists, _mergedConfig, word, autoRegex)
            : _wordLists.AddWhitelistWord(_userWordLists, _mergedConfig, word);

        if (ok)
        {
            RefreshMergedConfig();
            AppendLog(isBlacklist
                ? $"[GUI] Добавлено в чёрный список: {word.Trim()}"
                : $"[GUI] Добавлено в белый список: {word.Trim()}");
        }

        return Task.FromResult((ok, message));
    }

    public Task<(bool Ok, string Message)> RemoveWordAsync(WordListEntry entry, bool isBlacklist)
    {
        var (ok, message) = isBlacklist
            ? _wordLists.RemoveBlacklistEntry(_userWordLists, entry)
            : _wordLists.RemoveWhitelistEntry(_userWordLists, entry);

        if (ok)
        {
            RefreshMergedConfig();
            AppendLog(isBlacklist
                ? $"[GUI] Удалено из чёрного списка: {entry.Text}"
                : $"[GUI] Удалено из белого списка: {entry.Text}");
        }

        return Task.FromResult((ok, message));
    }

    public Task<(bool Ok, string Message)> RemoveBlacklistRegexForSearchAsync(string searchQuery)
    {
        RefreshMergedConfig();

        var (ok, message, _) = _wordLists.RemoveBlacklistRegexForSearch(_userWordLists, _mergedConfig, searchQuery);
        if (ok)
        {
            RefreshMergedConfig();
            AppendLog($"[GUI] Удалён regex для слова: {searchQuery.Trim()}");
        }

        return Task.FromResult((ok, message));
    }

    private void RefreshMergedConfig()
    {
        _userWordLists = _wordLists.LoadUserLists();
        _mergedConfig = _wordLists.LoadMergedConfig();
        _filter.Reload(_mergedConfig);
    }

    public Task<IReadOnlyList<BanRecord>> GetTodayBansAsync()
        => Task.FromResult(_banHistory.GetTodayActiveBans());

    public async Task<(bool Ok, string Message)> UnbanUserAsync(BanRecord record)
    {
        if (record.Unbanned)
            return (false, "Пользователь уже разбанен");

        if (string.IsNullOrWhiteSpace(record.UserId))
            return (false, "User ID не сохранён — разбан невозможен");

        try
        {
            if (_bot?.IsConnected == true)
            {
                await _bot.UnbanUserAsync(record.UserId);
            }
            else
            {
                var settings = _settings.Load();
                var missing = _settings.ValidateRequired(settings);
                if (missing.Count > 0)
                    return (false, "Заполните настройки Twitch для разбана");

                await TwitchModerationHelper.UnbanUserAsync(settings, record.UserId);
            }

            _banHistory.MarkUnbanned(record.Id);
            AppendLog($"[GUI] Разбан: {record.Username}");
            return (true, $"Разбан: {record.Username}");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

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
        _wordLists.ClearUserLists();
        _banHistory.Clear();
        RefreshMergedConfig();

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
