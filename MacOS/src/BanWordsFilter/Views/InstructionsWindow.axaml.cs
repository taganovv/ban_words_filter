using System.Diagnostics;
using System.IO;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace BanWordsFilter.Views;

public partial class InstructionsWindow : Window
{
    public InstructionsWindow()
    {
        InitializeComponent();
        InstructionsText.Text = LoadInstructions();
    }

    private static string LoadInstructions()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "BanWordsFilter.Resources.SetupInstructions.txt";
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is not null)
        {
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        var filePath = Path.Combine(
            Path.GetDirectoryName(assembly.Location) ?? "",
            "Resources",
            "SetupInstructions.txt");
        if (File.Exists(filePath))
            return File.ReadAllText(filePath);

        return "Инструкция не найдена. Обратитесь к разработчику.";
    }

    private void OnOpenTwitchDevClick(object? sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "https://dev.twitch.tv/console",
            UseShellExecute = true,
        });
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close();
}
