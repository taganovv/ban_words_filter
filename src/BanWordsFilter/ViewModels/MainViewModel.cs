using System;
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

public sealed class MainViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly BotLifecycleService _services;
    private readonly DispatcherTimer _pollTimer;
    private int _selectedTab;
    private bool _isRunning;
    private string _statusText = "Остановлен";
    private string _logText = "(пусто)";
    private string _testInput = "";
    private string _testResult = "";
    private string _toastMessage = "";
    private bool _toastIsError;
    private bool _toastVisible;

    public MainViewModel(BotLifecycleService services)
    {
        _services = services;
        Fields = new SettingFieldsCollection();

        SelectSettingsCommand = new RelayCommand(() => SelectedTab = 0);
        SelectTestCommand = new RelayCommand(() => SelectedTab = 1);
        SelectLogCommand = new RelayCommand(() => SelectedTab = 2);
        SaveCommand = new RelayCommand(async () => await SaveAsync());
        ValidateCommand = new RelayCommand(async () => await ValidateTokenAsync());
        OAuthCommand = new RelayCommand(OpenOAuth);
        StartCommand = new RelayCommand(async () => await StartBotAsync());
        StopCommand = new RelayCommand(async () => await StopBotAsync());
        TestFilterCommand = new RelayCommand(async () => await TestFilterAsync());
        ChatTestCommand = new RelayCommand(async () => await RunChatTestAsync());
        ClearAllCommand = new RelayCommand(async () => await RunClearAllAsync());
        InstructionsCommand = new RelayCommand(() => ShowInstructionsRequested?.Invoke());
        OpenGithubCommand = new RelayCommand(OpenGithub);
        OpenTwitchCommand = new RelayCommand(OpenTwitch);

        _pollTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _pollTimer.Tick += async (_, _) => await PollAsync();
        _pollTimer.Start();
    }

    public SettingFieldsCollection Fields { get; }

    public ICommand SelectSettingsCommand { get; }
    public ICommand SelectTestCommand { get; }
    public ICommand SelectLogCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand ValidateCommand { get; }
    public ICommand OAuthCommand { get; }
    public ICommand StartCommand { get; }
    public ICommand StopCommand { get; }
    public ICommand TestFilterCommand { get; }
    public ICommand ChatTestCommand { get; }
    public ICommand ClearAllCommand { get; }
    public ICommand InstructionsCommand { get; }
    public ICommand OpenGithubCommand { get; }
    public ICommand OpenTwitchCommand { get; }

    public event Action? ShowInstructionsRequested;
    public event Func<Task<bool>>? ConfirmChatTestRequested;
    public event Func<Task<bool>>? ConfirmClearAllRequested;

    public int SelectedTab
    {
        get => _selectedTab;
        set => SetField(ref _selectedTab, value);
    }

    public bool IsRunning
    {
        get => _isRunning;
        private set => SetField(ref _isRunning, value);
    }

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
            LogText = "(пусто)";
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

    private async Task PollAsync()
    {
        try
        {
            var logs = await _services.GetLogsAsync();
            LogText = logs.Count == 0 ? "(пусто)" : string.Join('\n', logs);
            await UpdateStatusAsync();
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

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (Equals(field, value))
            return;
        field = value;
        OnPropertyChanged(name);
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public void Dispose() => _pollTimer.Stop();
}
