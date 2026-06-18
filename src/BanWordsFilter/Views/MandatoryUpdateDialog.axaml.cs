using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using BanWordsFilter.Models;

namespace BanWordsFilter.Views;

public partial class MandatoryUpdateDialog : Window
{
    public enum MandatoryUpdateAction
    {
        Exit,
        Update
    }

    public MandatoryUpdateAction Action { get; private set; } = MandatoryUpdateAction.Exit;

    public MandatoryUpdateDialog(UpdateCheckResult update)
    {
        InitializeComponent();
        VersionTextBlock.Text =
            $"У вас версия {FormatVersion(update.CurrentVersion)}, актуальная версия — {FormatVersion(update.LatestVersion)}.";
    }

    private static string FormatVersion(Version? version)
        => version?.ToString(3) ?? "неизвестна";

    private void OnUpdateClick(object? sender, RoutedEventArgs e)
    {
        Action = MandatoryUpdateAction.Update;
        Close();
    }

    private void OnExitClick(object? sender, RoutedEventArgs e)
    {
        Action = MandatoryUpdateAction.Exit;
        Close();
    }
}
