using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using BanWordsFilter.Models;
using BanWordsFilter.Services;
using BanWordsFilter.Views;

namespace BanWordsFilter;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (Program.IsDuplicateInstance)
            {
                desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                var dialog = new AlreadyRunningDialog
                {
                    WindowStartupLocation = WindowStartupLocation.CenterScreen
                };
                dialog.Closed += (_, _) => desktop.Shutdown();
                dialog.Show();
                base.OnFrameworkInitializationCompleted();
                return;
            }

            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            _ = RunStartupAsync(desktop);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static async Task RunStartupAsync(IClassicDesktopStyleApplicationLifetime desktop)
    {
        UpdateCheckResult updateResult;

        var checkingWindow = new UpdateCheckingWindow();
        void BlockCloseDuringCheck(object? sender, WindowClosingEventArgs e) => e.Cancel = true;
        checkingWindow.Closing += BlockCloseDuringCheck;
        checkingWindow.Show();

        try
        {
            var updateService = new UpdateCheckService();
            updateResult = await updateService.CheckForUpdatesAsync();
        }
        catch
        {
            updateResult = new UpdateCheckResult { Requirement = UpdateRequirement.None };
        }
        finally
        {
            checkingWindow.Closing -= BlockCloseDuringCheck;
            checkingWindow.Close();
        }

        if (updateResult.Requirement == UpdateRequirement.Mandatory)
        {
            var dialog = new UpdateDialog(updateResult, mandatory: true)
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };
            var waitTask = WaitForCloseAsync(dialog);
            dialog.Show();
            await waitTask;

            desktop.Shutdown();
            return;
        }

        if (updateResult.Requirement == UpdateRequirement.Optional)
        {
            var dialog = new UpdateDialog(updateResult, mandatory: false)
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };
            var waitTask = WaitForCloseAsync(dialog);
            dialog.Show();
            await waitTask;

            if (dialog.Outcome is UpdateDialog.UpdateDialogOutcome.UpdateCompleted)
            {
                desktop.Shutdown();
                return;
            }
        }

        var mainWindow = new MainWindow();
        desktop.MainWindow = mainWindow;
        mainWindow.Show();
        desktop.ShutdownMode = ShutdownMode.OnMainWindowClose;
    }

    private static Task WaitForCloseAsync(Window dialog)
    {
        var completion = new TaskCompletionSource();
        dialog.Closed += (_, _) => completion.TrySetResult();
        return completion.Task;
    }
}
