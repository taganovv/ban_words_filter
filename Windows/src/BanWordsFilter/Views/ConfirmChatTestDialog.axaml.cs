using Avalonia.Controls;
using Avalonia.Interactivity;

namespace BanWordsFilter.Views;

public partial class ConfirmChatTestDialog : Window
{
    public ConfirmChatTestDialog()
    {
        InitializeComponent();
        MessageText.Text = AppConstants.ChatTestMessage;
    }

    private void OnConfirmClick(object? sender, RoutedEventArgs e)
    {
        Close(true);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
