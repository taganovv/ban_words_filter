using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using BanWordsFilter.Services;
using BanWordsFilter.ViewModels;
using BanWordsFilter.Views;

namespace BanWordsFilter;

public partial class MainWindow : Window
{
    private readonly BotLifecycleService _services = new();
    private readonly MainViewModel _viewModel;

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
        Closed += OnClosed;
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
