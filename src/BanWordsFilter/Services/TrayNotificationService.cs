using Avalonia.Controls;
using BanWordsFilter.Views;

namespace BanWordsFilter.Services;

public static class TrayNotificationService
{
    private static TrayNotificationWindow? _activeNotification;

    public static void ShowMinimizedToTray(Window? owner)
    {
        _activeNotification?.Close();

        var notification = new TrayNotificationWindow();
        _activeNotification = notification;
        notification.Closed += (_, _) =>
        {
            if (ReferenceEquals(_activeNotification, notification))
                _activeNotification = null;
        };
        notification.ShowForOwner(owner);
    }
}
