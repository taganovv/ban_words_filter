using Avalonia.Controls;
using Avalonia.Interactivity;

namespace BanWordsFilter.Views;

public partial class ConfirmExitDialog : Window
{
    public ConfirmExitDialog()
    {
        InitializeComponent();
    }

    private void OnConfirmClick(object? sender, RoutedEventArgs e) => Close(true);

    private void OnCancelClick(object? sender, RoutedEventArgs e) => Close(false);
}
