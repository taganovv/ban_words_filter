using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;

namespace BanWordsFilter.Views;

public partial class InstructionsWindow : Window
{
    public InstructionsWindow()
    {
        InitializeComponent();
        BuildInstructions();
    }

    private void BuildInstructions()
    {
        StepsPanel.Children.Add(CreateIntro());

        AddStep(1, CreateText(
            "Зайдите на ",
            "dev.twitch.tv",
            " и авторизуйтесь под учётной записью Twitch. Кнопка «Открыть dev.twitch.tv» — внизу этого окна."));

        AddStep(2, CreateText(
            "После авторизации откройте консоль — кнопка ",
            "Your Console",
            " в правом верхнем углу."));

        AddStep(3, CreateText(
            "В консоли нажмите ",
            "Создать расширение",
            "."));

        AddStep(4, CreateText(
            "Укажите название расширения (любое, только для вас) и нажмите ",
            "Продолжить",
            "."));

        AddStep(5, CreateText(
            "Откроется окно с версиями — ничего заполнять не нужно. Нажмите ",
            "Настройки расширения",
            " в правом верхнем углу."));

        AddStep(6,
            CreateText(
                "В поле ",
                "OAuth Redirect URL",
                " замените "),
            CreateCodeCompare("https://localhost", "http://localhost"),
            CreateText(
                " и нажмите ",
                "Сохранить изменения",
                "."),
            CreateWarnCallout("Важно: используйте именно http, не https. Без этого токен не получится."));

        AddStep(7, CreateText(
            "В шапке расширения скопируйте ",
            "Идентификатор клиента",
            " и вставьте в поле ",
            "Client ID",
            " в программе."));

        AddStep(8, CreateText(
            "Найдите ",
            "Секретный код клиента Twitch API",
            ", нажмите ",
            "Создать секретный код",
            ", скопируйте значение и вставьте в поле ",
            "Client Secret",
            " в программе."));

        AddStep(9, CreateText(
            "В программе нажмите ",
            "Получить OAuth Token",
            ". Авторизуйтесь под своим аккаунтом Twitch и нажмите ",
            "Разрешить",
            ". Откроется вкладка ",
            "localhost",
            "."));

        AddStep(10,
            CreateText(
                "Вкладка ",
                "localhost",
                " может показать ошибку «Не удаётся получить доступ к сайту»."),
            CreateOkCallout("Это нормально! Страница не должна загружаться — нужна только ссылка из адресной строки."));

        AddStep(11, CreateText(
            "Скопируйте ",
            "всю ссылку",
            " из адресной строки вкладки localhost и вставьте в поле ",
            "OAuth Token",
            " в программе. Приложение само извлечёт токен."));

        AddStep(12, CreateText(
            "Нажмите ",
            "Проверить токен",
            ". Поля ",
            "Ваш логин Twitch",
            ", ",
            "Streamer User ID",
            " и ",
            "Модерируемый канал",
            " заполнятся автоматически."));

        AddStep(13, CreateText(
            "Если всё сделано правильно — программа готова к работе! Нажмите ",
            "Сохранить",
            ", затем ",
            "▶ Старт",
            "."));

        StepsPanel.Children.Add(CreateModeratorNote());
    }

    private Border CreateIntro()
        => new Border
        {
            Classes = { "instruction-intro" },
            Child = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                FontSize = 13,
                LineHeight = 21,
                Text = "Инструкция может показаться сложной, но настроить приложение иначе невозможно. "
                       + "Прошу отнестись с пониманием и прочитать внимательно — тогда у вас точно всё получится!"
            }
        };

    private Border CreateModeratorNote()
        => new Border
        {
            Classes = { "instruction-callout-info" },
            Child = CreateText(
                "Для модераторов: если вы модерируете чужой канал, измените поле ",
                "Модерируемый канал",
                " — укажите ник стримера, а не свой. Затем нажмите ",
                "Сохранить",
                ". Остальные шаги те же.")
        };

    private void AddStep(int number, params Control[] parts)
    {
        var content = new StackPanel { Spacing = 4 };
        foreach (var part in parts)
            content.Children.Add(part);

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*")
        };

        grid.Children.Add(new Border
        {
            Classes = { "instruction-badge" },
            VerticalAlignment = VerticalAlignment.Top,
            Child = new TextBlock
            {
                Text = number.ToString(),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White
            }
        });

        Grid.SetColumn(content, 1);
        content.Margin = new Thickness(12, 0, 0, 0);
        grid.Children.Add(content);

        StepsPanel.Children.Add(new Border
        {
            Classes = { "instruction-step" },
            Child = grid
        });
    }

    private static TextBlock CreateText(params string[] segments)
    {
        var block = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            FontSize = 13,
            LineHeight = 21
        };

        for (var i = 0; i < segments.Length; i++)
        {
            var segment = segments[i];
            block.Inlines!.Add(i % 2 == 1 ? Highlight(segment) : new Run { Text = segment });
        }

        return block;
    }

    private static TextBlock CreateCodeCompare(string wrong, string correct)
    {
        var block = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            FontSize = 13,
            LineHeight = 21,
            Margin = new Thickness(0, 4, 0, 0)
        };

        block.Inlines!.Add(new Run
        {
            Text = wrong,
            Foreground = ErrorBrush(),
            TextDecorations = TextDecorations.Strikethrough
        });
        block.Inlines.Add(new Run { Text = "  →  ", Foreground = MutedBrush() });
        block.Inlines.Add(new Run
        {
            Text = correct,
            Foreground = SuccessBrush(),
            FontWeight = FontWeight.SemiBold
        });

        return block;
    }

    private static Border CreateWarnCallout(string text)
        => new Border
        {
            Classes = { "instruction-callout-warn" },
            Child = new TextBlock
            {
                Text = text,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 13,
                LineHeight = 20,
                Foreground = AccentBrush(),
                FontWeight = FontWeight.SemiBold
            }
        };

    private static Border CreateOkCallout(string text)
        => new Border
        {
            Classes = { "instruction-callout-ok" },
            Child = new TextBlock
            {
                Text = text,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 13,
                LineHeight = 20,
                Foreground = SuccessBrush(),
                FontWeight = FontWeight.SemiBold
            }
        };

    private static Run Highlight(string text)
        => new Run
        {
            Text = text,
            Foreground = AccentBrush(),
            FontWeight = FontWeight.SemiBold
        };

    private static IBrush AccentBrush() => GetBrush("PurpleBrush");
    private static IBrush SuccessBrush() => GetBrush("GreenBrush");
    private static IBrush MutedBrush() => GetBrush("MutedBrush");
    private static IBrush ErrorBrush() => GetBrush("RedBrush");

    private static IBrush GetBrush(string key)
        => Application.Current?.FindResource(key) as IBrush ?? Brushes.White;

    private void OnOpenTwitchDevClick(object? sender, RoutedEventArgs e)
        => OpenUrl("https://dev.twitch.tv/");

    private void OnOpenTwitchClick(object? sender, RoutedEventArgs e)
        => OpenUrl(AppConstants.TwitchUrl);

    private void OnOpenGithubClick(object? sender, RoutedEventArgs e)
        => OpenUrl(AppConstants.GithubUrl);

    private void OnOpenDonateClick(object? sender, RoutedEventArgs e)
        => OpenUrl(AppConstants.DonateUrl);

    private static void OpenUrl(string url)
        => Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close();
}
