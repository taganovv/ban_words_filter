using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using BanWordsFilter.Models;
using BanWordsFilter.Services;

namespace BanWordsFilter.Views;

public partial class UpdateDialog : Window
{
    public enum UpdateDialogOutcome
    {
        ContinueToApp,
        ExitApp,
        UpdateCompleted
    }

    private readonly UpdateCheckResult _update;
    private readonly bool _mandatory;
    private readonly UpdateDownloadService _downloadService = new();
    private readonly UpdateApplyService _applyService = new();
    private CancellationTokenSource? _updateCts;
    private bool _isUpdating;
    private bool _updateBlockedClose;

    public UpdateDialogOutcome Outcome { get; private set; } = UpdateDialogOutcome.ContinueToApp;

    public UpdateDialog(UpdateCheckResult update, bool mandatory)
    {
        _update = update;
        _mandatory = mandatory;
        if (mandatory)
            Outcome = UpdateDialogOutcome.ExitApp;

        InitializeComponent();
        ConfigurePrompt();
        Closing += OnClosing;
    }

    private void ConfigurePrompt()
    {
        Title = _mandatory ? "Требуется обновление" : "Доступно обновление";
        TitleTextBlock.Text = _mandatory ? "Обновление обязательно" : "Доступна новая версия";

        MessageTextBlock.Text = _mandatory
            ? "Ваша версия программы сильно устарела и мы прекратили её поддержку! Пожалуйста, обновитесь до актуальной версии."
            : "Рекомендуем обновиться до последней версии, чтобы получить исправления и улучшения.";

        VersionTextBlock.Text =
            $"У вас версия {FormatVersion(_update.CurrentVersion)}, доступна версия {FormatVersion(_update.LatestVersion)}.";

        SecondaryButton.Content = _mandatory ? "Выход" : "Отмена";
        PrimaryButton.Content = "Обновить сейчас";
        ProgressPanel.IsVisible = false;
    }

    private async void OnPrimaryClick(object? sender, RoutedEventArgs e)
    {
        if (_isUpdating)
            return;

        if (string.IsNullOrWhiteSpace(_update.InstallerDownloadUrl))
        {
            ShowError("Ссылка на установщик не найдена. Попробуйте позже.");
            return;
        }

        await StartUpdateAsync();
    }

    private void OnSecondaryClick(object? sender, RoutedEventArgs e)
    {
        if (_isUpdating)
            return;

        Outcome = _mandatory ? UpdateDialogOutcome.ExitApp : UpdateDialogOutcome.ContinueToApp;
        Close();
    }

    private async Task StartUpdateAsync()
    {
        _isUpdating = true;
        _updateBlockedClose = true;
        _updateCts = new CancellationTokenSource();

        MessageTextBlock.IsVisible = false;
        VersionTextBlock.IsVisible = false;
        ProgressPanel.IsVisible = true;
        ButtonsPanel.IsVisible = false;

        DownloadProgressBar.IsIndeterminate = false;
        DownloadProgressBar.Value = 0;
        SetProgressStep(1, "Загрузка обновления...", "0%");

        try
        {
            var progress = new Progress<DownloadProgress>(UpdateDownloadProgress);
            var installerPath = await _downloadService.DownloadInstallerAsync(
                _update.InstallerDownloadUrl!,
                progress,
                _updateCts.Token);

            SetProgressStep(2, "Удаление старой версии...", "Подготовка...");
            DownloadProgressBar.IsIndeterminate = true;
            await Task.Delay(400, _updateCts.Token);

            SetProgressStep(3, "Установка новой версии...", "Скоро приложение перезапустится");
            await Task.Delay(400, _updateCts.Token);

            SetProgressStep(4, "Запуск новой версии...", "Приложение откроется автоматически");
            _applyService.ScheduleSeamlessUpdate(installerPath);

            Outcome = UpdateDialogOutcome.UpdateCompleted;
            _updateBlockedClose = false;
            await Task.Delay(900, _updateCts.Token);
            Close();
        }
        catch (OperationCanceledException)
        {
            ShowError("Обновление отменено.");
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private void SetProgressStep(int step, string status, string detail)
    {
        ProgressStatusText.Text = $"Шаг {step} из 4: {status}";
        ProgressDetailText.Text = detail;
    }

    private void UpdateDownloadProgress(DownloadProgress progress)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (progress.Percent is double percent)
            {
                DownloadProgressBar.IsIndeterminate = false;
                DownloadProgressBar.Value = percent;
                ProgressDetailText.Text =
                    $"{percent:0}% · {FormatBytes(progress.BytesReceived)} из {FormatBytes(progress.TotalBytes!.Value)}";
            }
            else
            {
                DownloadProgressBar.IsIndeterminate = true;
                ProgressDetailText.Text = FormatBytes(progress.BytesReceived);
            }
        });
    }

    private void ShowError(string message)
    {
        _isUpdating = false;
        _updateBlockedClose = false;
        _updateCts?.Dispose();
        _updateCts = null;

        MessageTextBlock.IsVisible = true;
        MessageTextBlock.Text = message;
        VersionTextBlock.IsVisible = true;
        ProgressPanel.IsVisible = false;
        ButtonsPanel.IsVisible = true;

        PrimaryButton.Content = "Повторить";
        SecondaryButton.Content = _mandatory ? "Выход" : "Отмена";
    }

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_updateBlockedClose)
            e.Cancel = true;
    }

    protected override void OnClosed(EventArgs e)
    {
        if (_mandatory && Outcome == UpdateDialogOutcome.ContinueToApp)
            Outcome = UpdateDialogOutcome.ExitApp;

        _updateCts?.Cancel();
        _updateCts?.Dispose();
        Closing -= OnClosing;
        base.OnClosed(e);
    }

    private static string FormatVersion(Version? version)
        => version?.ToString(3) ?? "неизвестна";

    private static string FormatBytes(long bytes)
    {
        if (bytes < 1024 * 1024)
            return $"{bytes / 1024.0:0.0} КБ";

        return $"{bytes / (1024.0 * 1024.0):0.0} МБ";
    }
}
