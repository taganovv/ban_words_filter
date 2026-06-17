using Avalonia.Controls;
using Avalonia.Interactivity;

namespace BanWordsFilter.Views;

public partial class ConfirmClearAllDialog : Window
{
    public ConfirmClearAllDialog()
    {
        InitializeComponent();
    }

    private void OnConfirmClick(object? sender, RoutedEventArgs e) => Close(true);

    private void OnCancelClick(object? sender, RoutedEventArgs e) => Close(false);
}
