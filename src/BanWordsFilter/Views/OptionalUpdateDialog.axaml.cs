using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using BanWordsFilter.Models;

namespace BanWordsFilter.Views;

public partial class OptionalUpdateDialog : Window
{
    public enum OptionalUpdateAction
    {
        Cancel,
        Update
    }

    public OptionalUpdateAction Action { get; private set; } = OptionalUpdateAction.Cancel;

    public OptionalUpdateDialog(UpdateCheckResult update)
    {
        InitializeComponent();
        VersionTextBlock.Text =
            $"У вас версия {FormatVersion(update.CurrentVersion)}, доступна версия {FormatVersion(update.LatestVersion)}.";
    }

    private static string FormatVersion(Version? version)
        => version?.ToString(3) ?? "неизвестна";

    private void OnUpdateClick(object? sender, RoutedEventArgs e)
    {
        Action = OptionalUpdateAction.Update;
        Close();
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Action = OptionalUpdateAction.Cancel;
        Close();
    }
}
