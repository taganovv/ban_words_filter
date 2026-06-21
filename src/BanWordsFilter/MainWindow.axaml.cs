using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using BanWordsFilter.Services;
using BanWordsFilter.ViewModels;
using BanWordsFilter.Views;

namespace BanWordsFilter;

public partial class MainWindow : Window
{
    private readonly BotLifecycleService _services = new();
    private readonly MainViewModel _viewModel;
    private bool _forceClose;

    public MainWindow()
    {
        _viewModel = new MainViewModel(_services);
        _viewModel.ShowInstructionsRequested += ShowInstructions;
        _viewModel.ConfirmChatTestRequested += ConfirmChatTestAsync;
        _viewModel.ConfirmClearAllRequested += ConfirmClearAllAsync;
        _viewModel.ConfirmRemoveAllRegexRequested += ConfirmRemoveAllRegexAsync;
        DataContext = _viewModel;
        InitializeComponent();
        Loaded += OnLoaded;
        Closing += OnClosing;
        Closed += OnClosed;
    }

    public void ShowFromTray()
    {
        ShowInTaskbar = true;
        Show();
        Activate();
        if (WindowState == WindowState.Minimized)
            WindowState = WindowState.Normal;
    }

    public void ForceClose()
    {
        _forceClose = true;
        Close();
    }

    private void ShowInstructions()
    {
        var window = new InstructionsWindow { WindowStartupLocation = WindowStartupLocation.CenterOwner };
        window.ShowDialog(this);
    }

    private async Task<bool> ConfirmChatTestAsync()
    {
        var dialog = new ConfirmChatTestDialog { WindowStartupLocation = WindowStartupLocation.CenterOwner };
        return await dialog.ShowDialog<bool>(this);
    }

    private async Task<bool> ConfirmClearAllAsync()
    {
        var dialog = new ConfirmClearAllDialog { WindowStartupLocation = WindowStartupLocation.CenterOwner };
        return await dialog.ShowDialog<bool>(this);
    }

    private async Task<bool> ConfirmRemoveAllRegexAsync()
    {
        var dialog = new ConfirmRemoveAllRegexDialog { WindowStartupLocation = WindowStartupLocation.CenterOwner };
        return await dialog.ShowDialog<bool>(this);
    }

    private async void OnExitClick(object? sender, RoutedEventArgs e)
    {
        var dialog = new ConfirmExitDialog { WindowStartupLocation = WindowStartupLocation.CenterOwner };
        if (!await dialog.ShowDialog<bool>(this))
            return;

        if (_viewModel.IsRunning)
            await _viewModel.StopBotAsync();

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            TrayService.ExitApplication(desktop);
    }

    private async void OnLoaded(object? sender, RoutedEventArgs e)
    {
        try
        {
            await _viewModel.InitializeAsync();
        }
        catch (Exception ex)
        {
            _viewModel.ShowStartupError(ex.Message);
        }
    }

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_forceClose)
            return;

        e.Cancel = true;
        Hide();
        ShowInTaskbar = false;
        TrayNotificationService.ShowMinimizedToTray(this);
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _viewModel.ShowInstructionsRequested -= ShowInstructions;
        _viewModel.ConfirmChatTestRequested -= ConfirmChatTestAsync;
        _viewModel.ConfirmClearAllRequested -= ConfirmClearAllAsync;
        _viewModel.ConfirmRemoveAllRegexRequested -= ConfirmRemoveAllRegexAsync;
        _viewModel.Dispose();
        _services.Dispose();
    }
}
