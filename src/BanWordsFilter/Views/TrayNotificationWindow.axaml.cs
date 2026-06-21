using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace BanWordsFilter.Views;

public partial class TrayNotificationWindow : Window
{
    private const int VisibleDurationMs = 5000;
    private const int FadeDurationMs = 800;
    private const int FadeStepMs = 16;

    private readonly DispatcherTimer _visibleTimer = new();
    private CancellationTokenSource? _fadeCts;
    private bool _isPointerInside;
    private bool _isClosing;
    private Window? _ownerWindow;

    public TrayNotificationWindow()
    {
        InitializeComponent();

        _visibleTimer.Tick += OnVisibleTimerTick;
        PointerEntered += OnPointerEntered;
        PointerExited += OnPointerExited;
        Closed += OnWindowClosed;
    }

    public void ShowForOwner(Window? owner)
    {
        _ownerWindow = owner;
        Show();
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        Dispatcher.UIThread.Post(() =>
        {
            PositionBottomRight();
            StartVisibleTimer();
        });
    }

    private void PositionBottomRight()
    {
        var screen = _ownerWindow is not null
            ? Screens.ScreenFromWindow(_ownerWindow)
            : Screens.Primary;

        if (screen is null)
            return;

        var workingArea = screen.WorkingArea;
        var width = (int)Math.Ceiling(Bounds.Width);
        var height = (int)Math.Ceiling(Bounds.Height);

        Position = new PixelPoint(
            workingArea.X + workingArea.Width - width - 16,
            workingArea.Y + workingArea.Height - height - 16);
    }

    private void StartVisibleTimer()
    {
        StopVisibleTimer();
        _visibleTimer.Interval = TimeSpan.FromMilliseconds(VisibleDurationMs);
        _visibleTimer.Start();
    }

    private void StopVisibleTimer()
    {
        if (_visibleTimer.IsEnabled)
            _visibleTimer.Stop();
    }

    private void OnVisibleTimerTick(object? sender, EventArgs e)
    {
        StopVisibleTimer();
        if (!_isPointerInside)
            _ = StartFadeOutAsync();
    }

    private void OnPointerEntered(object? sender, PointerEventArgs e)
    {
        _isPointerInside = true;
        StopVisibleTimer();
        CancelFade();
        Opacity = 1;
    }

    private void OnPointerExited(object? sender, PointerEventArgs e)
    {
        _isPointerInside = false;
        if (!_isClosing)
            StartVisibleTimer();
    }

    private void CancelFade()
    {
        _fadeCts?.Cancel();
        _fadeCts?.Dispose();
        _fadeCts = null;
    }

    private async Task StartFadeOutAsync()
    {
        if (_isPointerInside || _isClosing)
            return;

        CancelFade();
        _fadeCts = new CancellationTokenSource();
        var token = _fadeCts.Token;

        try
        {
            var steps = Math.Max(1, FadeDurationMs / FadeStepMs);
            for (var step = steps; step >= 0; step--)
            {
                token.ThrowIfCancellationRequested();

                if (_isPointerInside)
                {
                    Opacity = 1;
                    StartVisibleTimer();
                    return;
                }

                Opacity = step / steps;
                await Task.Delay(FadeStepMs, token);
            }

            CloseImmediately();
        }
        catch (OperationCanceledException)
        {
            Opacity = 1;
        }
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
        => CloseImmediately();

    private void CloseImmediately()
    {
        if (_isClosing)
            return;

        _isClosing = true;
        StopVisibleTimer();
        CancelFade();
        Close();
    }

    private void OnWindowClosed(object? sender, EventArgs e)
    {
        StopVisibleTimer();
        CancelFade();
        PointerEntered -= OnPointerEntered;
        PointerExited -= OnPointerExited;
        Closed -= OnWindowClosed;
        _visibleTimer.Tick -= OnVisibleTimerTick;
    }
}
