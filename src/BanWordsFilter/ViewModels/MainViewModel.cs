using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Threading;
using BanWordsFilter.Models;
using BanWordsFilter.Services;

namespace BanWordsFilter.ViewModels;

public sealed partial class MainViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly BotLifecycleService _services;
    private readonly DispatcherTimer _pollTimer;
    private int _selectedTab;
    private int _wordListSubTab;
    private bool _isRunning;
    private string _statusText = "Остановлен";
    private string _logText = "(пусто)";
    private string _testInput = "";
    private string _testResult = "";
    private string _newWordInput = "";
    private string _wordListSearch = "";
    private bool _useAutomaticRegex;
    private string _toastMessage = "";
    private bool _toastIsError;
    private bool _toastVisible;
    private List<WordListEntry> _allBlacklistEntries = [];
    private List<WordListEntry> _allWhitelistEntries = [];
    private readonly NormalizationConfig _normalization = BannedWordsConfigService.LoadEmbeddedConfig().Normalization;

    public MainViewModel(BotLifecycleService services)
    {
        _services = services;
        Fields = new SettingFieldsCollection();
        BlacklistEntries = [];
        WhitelistEntries = [];

        InitBansCommands();
        SelectSettingsCommand = new RelayCommand(() => SelectedTab = 0);
        SelectTestCommand = new RelayCommand(() => SelectedTab = 1);
        SelectLogCommand = new RelayCommand(() => SelectedTab = 2);
        SelectWordListsCommand = new RelayCommand(async () =>
        {
            SelectedTab = 3;
            await LoadWordListsAsync();
        });
        SelectBlacklistCommand = new RelayCommand(() => WordListSubTab = 0);
        SelectWhitelistCommand = new RelayCommand(() => WordListSubTab = 1);
        SaveCommand = new RelayCommand(async () => await SaveAsync());
        ValidateCommand = new RelayCommand(async () => await ValidateTokenAsync());
        OAuthCommand = new RelayCommand(OpenOAuth);
        ToggleBotCommand = new RelayCommand(async () =>
        {
            if (IsRunning)
                await StopBotAsync();
            else
                await StartBotAsync();
        });
        TestFilterCommand = new RelayCommand(async () => await TestFilterAsync());
        ChatTestCommand = new RelayCommand(async () => await RunChatTestAsync());
        ClearAllCommand = new RelayCommand(async () => await RunClearAllAsync());
        AddWordCommand = new RelayCommand(async () => await AddWordAsync());
        RemoveBlacklistEntryCommand = new RelayCommand<WordListEntry>(async entry => await RemoveWordAsync(entry, true));
        RemoveWhitelistEntryCommand = new RelayCommand<WordListEntry>(async entry => await RemoveWordAsync(entry, false));
        RemoveAllBlacklistRegexCommand = new RelayCommand(async () => await RemoveSearchBlacklistRegexAsync());
        ClearWordListSearchCommand = new RelayCommand(ClearWordListSearch);
        InstructionsCommand = new RelayCommand(() => ShowInstructionsRequested?.Invoke());
        OpenGithubCommand = new RelayCommand(OpenGithub);
        OpenTwitchCommand = new RelayCommand(OpenTwitch);
        OpenDonateCommand = new RelayCommand(OpenDonate);

        _pollTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _pollTimer.Tick += async (_, _) => await PollAsync();
        _pollTimer.Start();
    }

    public SettingFieldsCollection Fields { get; }
    public ObservableCollection<WordListEntry> BlacklistEntries { get; }
    public ObservableCollection<WordListEntry> WhitelistEntries { get; }

    public ICommand SelectSettingsCommand { get; }
    public ICommand SelectTestCommand { get; }
    public ICommand SelectLogCommand { get; }
    public ICommand SelectWordListsCommand { get; }
    public ICommand SelectBlacklistCommand { get; }
    public ICommand SelectWhitelistCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand ValidateCommand { get; }
    public ICommand OAuthCommand { get; }
    public ICommand ToggleBotCommand { get; }
    public ICommand TestFilterCommand { get; }
    public ICommand ChatTestCommand { get; }
    public ICommand ClearAllCommand { get; }
    public ICommand AddWordCommand { get; }
    public ICommand RemoveBlacklistEntryCommand { get; }
    public ICommand RemoveWhitelistEntryCommand { get; }
    public ICommand RemoveAllBlacklistRegexCommand { get; }
    public ICommand ClearWordListSearchCommand { get; }
    public ICommand InstructionsCommand { get; }
    public ICommand OpenGithubCommand { get; }
    public ICommand OpenTwitchCommand { get; }
    public ICommand OpenDonateCommand { get; }

    public event Action? ShowInstructionsRequested;
    public event Func<Task<bool>>? ConfirmChatTestRequested;
    public event Func<Task<bool>>? ConfirmClearAllRequested;
    public event Func<Task<bool>>? ConfirmRemoveAllRegexRequested;

    public int SelectedTab
    {
        get => _selectedTab;
        set => SetField(ref _selectedTab, value);
    }

    public bool IsRunning
    {
        get => _isRunning;
        private set
        {
            if (!SetField(ref _isRunning, value))
                return;

            OnPropertyChanged(nameof(BotToggleLabel));
        }
    }

    public string BotToggleLabel => IsRunning ? "■  Стоп" : "▶  Старт";

    public string StatusText
    {
        get => _statusText;
        private set => SetField(ref _statusText, value);
    }

    public string LogText
    {
        get => _logText;
        private set => SetField(ref _logText, value);
    }

    public string TestInput
    {
        get => _testInput;
        set => SetField(ref _testInput, value);
    }

    public string TestResult
    {
        get => _testResult;
        private set => SetField(ref _testResult, value);
    }

    public int WordListSubTab
    {
        get => _wordListSubTab;
        set
        {
            if (!SetField(ref _wordListSubTab, value))
                return;

            if (value == 1 && UseAutomaticRegex)
                UseAutomaticRegex = false;

            OnPropertyChanged(nameof(IsBlacklistSelected));
            OnPropertyChanged(nameof(RegexCheckboxEnabled));
            OnPropertyChanged(nameof(RegexCheckboxTooltip));
            OnPropertyChanged(nameof(ShowRegexHint));
        }
    }

    public string NewWordInput
    {
        get => _newWordInput;
        set => SetField(ref _newWordInput, value);
    }

    public string WordListSearch
    {
        get => _wordListSearch;
        set
        {
            if (!SetField(ref _wordListSearch, value))
                return;

            ApplyWordListFilter();
        }
    }

    public string BlacklistCountText => FormatWordListCount(_allBlacklistEntries.Count, BlacklistEntries.Count);

    public string WhitelistCountText => FormatWordListCount(_allWhitelistEntries.Count, WhitelistEntries.Count);

    public int SearchRelatedRegexCount => BlacklistEntries.Count(entry => entry.Type == "regex");

    public bool CanRemoveSearchRegex =>
        IsBlacklistSelected && !string.IsNullOrWhiteSpace(WordListSearch) && SearchRelatedRegexCount > 0;

    public string RemoveSearchRegexButtonText
    {
        get
        {
            var query = WordListSearch.Trim();
            if (string.IsNullOrEmpty(query))
                return "Удалить regex для слова";

            return SearchRelatedRegexCount > 0
                ? $"Удалить regex для «{query}» ({SearchRelatedRegexCount})"
                : $"Удалить regex для «{query}»";
        }
    }

    public bool UseAutomaticRegex
    {
        get => _useAutomaticRegex;
        set => SetField(ref _useAutomaticRegex, value);
    }

    public bool IsBlacklistSelected => WordListSubTab == 0;

    public bool RegexCheckboxEnabled => IsBlacklistSelected;

    public bool ShowRegexHint => IsBlacklistSelected;

    public string RegexCheckboxTooltip => IsBlacklistSelected
        ? ""
        : "Regex недоступен для белого списка";

    public string RegexHintText =>
        "Regex — шаблон для поиска слова с обходными написаниями (p1d0r, p!!!dor и т.п.). " +
        "При включении «Автоматический regex» создаётся паттерн, который находит слово с заменой букв и символами.";

    public string ToastMessage
    {
        get => _toastMessage;
        private set => SetField(ref _toastMessage, value);
    }

    public bool ToastIsError
    {
        get => _toastIsError;
        private set => SetField(ref _toastIsError, value);
    }

    public bool ToastVisible
    {
        get => _toastVisible;
        private set => SetField(ref _toastVisible, value);
    }

    public async Task InitializeAsync()
    {
        try
        {
            Fields.Load(await _services.GetSettingsAsync());
            await UpdateStatusAsync();
            await LoadWordListsAsync();
        }
        catch (Exception ex)
        {
            ShowToast(ex.Message, true);
        }
    }

    public async Task SaveAsync()
    {
        try
        {
            var (_, message) = await _services.SaveSettingsAsync(Fields.ToDictionary());
            ShowToast(message);
        }
        catch (Exception ex)
        {
            ShowToast(ex.Message, true);
        }
    }

    public async Task ValidateTokenAsync()
    {
        try
        {
            var (ok, message, info) = await _services.ValidateTokenAsync(Fields.ToDictionary());
            if (!ok || info is null)
            {
                ShowToast(message, true);
                return;
            }

            SetFieldValue("TWITCH_BOT_NAME", info.Login ?? "");
            SetFieldValue("TWITCH_CHANNEL", info.Login ?? "");
            SetFieldValue("TWITCH_BOT_ID", info.UserId ?? "");

            var msg = info.Valid
                ? $"Токен OK: {info.Login} ({info.UserId})"
                : $"Не хватает scopes: {string.Join(", ", info.MissingScopes ?? [])}";
            ShowToast(msg, !info.Valid);
        }
        catch (Exception ex)
        {
            ShowToast(ex.Message, true);
        }
    }

    public void OpenOAuth()
    {
        var clientId = GetFieldValue("TWITCH_CLIENT_ID");
        if (string.IsNullOrEmpty(clientId))
        {
            ShowToast("Сначала вставьте Client ID из dev.twitch.tv", true);
            return;
        }

        var url = "https://id.twitch.tv/oauth2/authorize"
            + "?client_id=" + Uri.EscapeDataString(clientId)
            + "&redirect_uri=" + Uri.EscapeDataString(AppConstants.OAuthRedirectUri)
            + "&response_type=token"
            + "&scope=" + Uri.EscapeDataString(AppConstants.OAuthScopes);

        Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        ShowToast("Получите новый токен — нужен scope user:bot для бана");
    }

    public static void OpenGithub()
        => OpenUrl(AppConstants.GithubUrl);

    public static void OpenTwitch()
        => OpenUrl(AppConstants.TwitchUrl);

    public static void OpenDonate()
        => OpenUrl(AppConstants.DonateUrl);

    public async Task StartBotAsync()
    {
        try
        {
            var (ok, message) = await _services.StartAsync(Fields.ToDictionary());
            ShowToast(message, !ok);
            await UpdateStatusAsync();
        }
        catch (Exception ex)
        {
            ShowToast(ex.Message, true);
        }
    }

    public async Task StopBotAsync()
    {
        try
        {
            var (ok, message) = await _services.StopAsync();
            ShowToast(message, !ok);
            await UpdateStatusAsync();
        }
        catch (Exception ex)
        {
            ShowToast(ex.Message, true);
        }
    }

    public async Task RunChatTestAsync()
    {
        if (!IsRunning)
        {
            ShowToast("Сначала запустите фильтр (▶ Старт)", true);
            return;
        }

        if (ConfirmChatTestRequested is null)
            return;

        if (!await ConfirmChatTestRequested())
            return;

        try
        {
            var (ok, message) = await _services.SendTestChatAsync();
            ShowToast(message, !ok);
        }
        catch (Exception ex)
        {
            ShowToast(ex.Message, true);
        }
    }

    public async Task RunClearAllAsync()
    {
        if (ConfirmClearAllRequested is null)
            return;

        if (!await ConfirmClearAllRequested())
            return;

        try
        {
            var (ok, message) = await _services.ClearAllAsync();
            Fields.Clear();
            TestInput = "";
            TestResult = "";
            NewWordInput = "";
            UseAutomaticRegex = false;
            LogText = "(пусто)";
            TodayBans.Clear();
            NotifyBansChanged();
            await LoadWordListsAsync();
            await UpdateStatusAsync();
            ShowToast(message, !ok);
        }
        catch (Exception ex)
        {
            ShowToast(ex.Message, true);
        }
    }

    public async Task TestFilterAsync()
    {
        try
        {
            var result = await _services.TestFilterAsync(TestInput);
            if (result is null)
            {
                TestResult = "Не удалось проверить сообщение";
                return;
            }

            var lines = new System.Collections.Generic.List<string>
            {
                $"Сообщение: \"{TestInput}\"",
                $"Забанить: {(result.Banned ? "ДА" : "НЕТ")}",
            };
            if (!string.IsNullOrEmpty(result.Action))
                lines.Add($"Действие: {result.Action}");
            if (result.Matches is { Count: > 0 })
            {
                lines.Add("");
                lines.Add("Совпадения:");
                foreach (var match in result.Matches.Take(5))
                    lines.Add($"  - [{match.CategoryLabel ?? match.Category}] {match.Pattern} ({match.Type})");
            }

            TestResult = string.Join('\n', lines);
        }
        catch (Exception ex)
        {
            TestResult = ex.Message;
        }
    }

    public async Task LoadWordListsAsync()
    {
        try
        {
            var (blacklist, whitelist) = await _services.GetWordListsAsync();
            _allBlacklistEntries = blacklist.ToList();
            _allWhitelistEntries = whitelist.ToList();
            ApplyWordListFilter();
        }
        catch (Exception ex)
        {
            ShowToast(ex.Message, true);
        }
    }

    public async Task RemoveSearchBlacklistRegexAsync()
    {
        var query = WordListSearch.Trim();
        if (string.IsNullOrEmpty(query))
        {
            ShowToast("Введите слово в поле поиска", true);
            return;
        }

        if (SearchRelatedRegexCount == 0)
        {
            ShowToast($"Regex для «{query}» не найден", true);
            return;
        }

        if (ConfirmRemoveAllRegexRequested is null)
            return;

        if (!await ConfirmRemoveAllRegexRequested())
            return;

        try
        {
            var (ok, message) = await _services.RemoveBlacklistRegexForSearchAsync(query);
            ShowToast(message, !ok);
            if (!ok)
                return;

            await LoadWordListsAsync();
        }
        catch (Exception ex)
        {
            ShowToast(ex.Message, true);
        }
    }

    private void ClearWordListSearch()
    {
        WordListSearch = "";
    }

    private void ApplyWordListFilter()
    {
        var query = WordListSearch.Trim();

        BlacklistEntries.Clear();
        foreach (var entry in BannedWordsConfigService.FilterBlacklistEntries(_allBlacklistEntries, query, _normalization))
            BlacklistEntries.Add(entry);

        WhitelistEntries.Clear();
        foreach (var entry in FilterWordListEntries(_allWhitelistEntries, query))
            WhitelistEntries.Add(entry);

        OnPropertyChanged(nameof(BlacklistCountText));
        OnPropertyChanged(nameof(WhitelistCountText));
        OnPropertyChanged(nameof(SearchRelatedRegexCount));
        OnPropertyChanged(nameof(CanRemoveSearchRegex));
        OnPropertyChanged(nameof(RemoveSearchRegexButtonText));
    }

    private static IEnumerable<WordListEntry> FilterWordListEntries(IEnumerable<WordListEntry> source, string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return source;

        return source.Where(entry =>
            entry.Text.Contains(query, StringComparison.OrdinalIgnoreCase)
            || entry.Category.Contains(query, StringComparison.OrdinalIgnoreCase)
            || entry.Type.Contains(query, StringComparison.OrdinalIgnoreCase));
    }

    private static string FormatWordListCount(int total, int visible)
        => total == visible ? $"Всего: {total}" : $"Показано: {visible} из {total}";

    public async Task AddWordAsync()
    {
        try
        {
            var isBlacklist = IsBlacklistSelected;
            var (ok, message) = await _services.AddWordAsync(NewWordInput, UseAutomaticRegex && isBlacklist, isBlacklist);
            ShowToast(message, !ok);
            if (!ok)
                return;

            NewWordInput = "";
            await LoadWordListsAsync();
        }
        catch (Exception ex)
        {
            ShowToast(ex.Message, true);
        }
    }

    public async Task RemoveWordAsync(WordListEntry? entry, bool isBlacklist)
    {
        if (entry is null)
            return;

        try
        {
            var (ok, message) = await _services.RemoveWordAsync(entry, isBlacklist);
            ShowToast(message, !ok);
            if (!ok)
                return;

            await LoadWordListsAsync();
        }
        catch (Exception ex)
        {
            ShowToast(ex.Message, true);
        }
    }

    private async Task PollAsync()
    {
        try
        {
            var logs = await _services.GetLogsAsync();
            LogText = logs.Count == 0 ? "(пусто)" : string.Join('\n', logs);
            await UpdateStatusAsync();
            if (SelectedTab == 4)
                await LoadBansAsync();
        }
        catch
        {
        }
    }

    private async Task UpdateStatusAsync()
    {
        var (running, _) = await _services.GetStatusAsync();
        IsRunning = running;
        StatusText = running ? "Работает" : "Остановлен";
    }

    private static void OpenUrl(string url)
    {
        Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
    }

    private string GetFieldValue(string key)
        => Fields.First(f => f.Key == key).Value.Trim();

    private void SetFieldValue(string key, string value)
        => Fields.First(f => f.Key == key).Value = value;

    private async void ShowToast(string message, bool isError = false)
    {
        ToastMessage = message;
        ToastIsError = isError;
        ToastVisible = true;

        await Task.Delay(4000);
        ToastVisible = false;
    }

    public void ShowStartupError(string message)
        => ShowToast(message, true);

    public event PropertyChangedEventHandler? PropertyChanged;

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (Equals(field, value))
            return false;
        field = value;
        OnPropertyChanged(name);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public void Dispose() => _pollTimer.Stop();
}
