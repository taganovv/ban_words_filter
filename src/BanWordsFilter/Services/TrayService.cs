using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform;

namespace BanWordsFilter.Services;

public static class TrayService
{
    private static TrayIcon? _trayIcon;

    public static void Initialize(IClassicDesktopStyleApplicationLifetime desktop)
    {
        if (_trayIcon is not null)
            return;

        var menu = new NativeMenu();
        var openItem = new NativeMenuItem("Открыть");
        openItem.Click += (_, _) => RestoreMainWindow(desktop);
        menu.Items.Add(openItem);

        menu.Items.Add(new NativeMenuItemSeparator());

        var exitItem = new NativeMenuItem("Выход");
        exitItem.Click += (_, _) => ExitApplication(desktop);
        menu.Items.Add(exitItem);

        _trayIcon = new TrayIcon
        {
            Icon = new WindowIcon(AssetLoader.Open(new Uri("avares://BanWordsFilter/Assets/app-icon.png"))),
            ToolTipText = "Ban Words Filter",
            Menu = menu,
            IsVisible = true
        };

        _trayIcon.Clicked += (_, _) => RestoreMainWindow(desktop);
    }

    public static void RestoreMainWindow(IClassicDesktopStyleApplicationLifetime desktop)
    {
        if (desktop.MainWindow is not MainWindow mainWindow)
            return;

        mainWindow.ShowFromTray();
    }

    public static void ExitApplication(IClassicDesktopStyleApplicationLifetime desktop)
    {
        if (desktop.MainWindow is MainWindow mainWindow)
            mainWindow.ForceClose();

        desktop.Shutdown();
    }
}
