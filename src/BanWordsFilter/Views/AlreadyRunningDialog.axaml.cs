using Avalonia.Controls;
using Avalonia.Interactivity;

namespace BanWordsFilter.Views;

public partial class AlreadyRunningDialog : Window
{
    public AlreadyRunningDialog()
    {
        InitializeComponent();
    }

    private void OnOkClick(object? sender, RoutedEventArgs e) => Close();
}
