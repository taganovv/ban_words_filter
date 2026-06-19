using Avalonia;
using System;

namespace BanWordsFilter;

class Program
{
    internal static bool IsDuplicateInstance { get; private set; }

    [STAThread]
    public static void Main(string[] args)
    {
        IsDuplicateInstance = !Services.SingleInstanceGuard.TryEnter();
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}
