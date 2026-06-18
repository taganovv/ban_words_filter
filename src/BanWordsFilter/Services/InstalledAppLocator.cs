using System;
using System.IO;
using Microsoft.Win32;

namespace BanWordsFilter.Services;

public static class InstalledAppLocator
{
    public const string AppExeName = "BanWordsFilter.exe";
    private const string RegistryKeyPath = @"Software\BanWordsFilter";
    private const string UninstallRegistryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\BanWordsFilter";
    private const string DefaultInstallRelativePath = @"Ban Words Filter\BanWordsFilter.exe";

    public static string GetExecutablePath()
    {
        if (OperatingSystem.IsWindows())
        {
            var installDir = GetInstallDirectory();
            if (installDir is not null)
            {
                var candidate = Path.Combine(installDir, AppExeName);
                if (File.Exists(candidate))
                    return candidate;
            }
        }

        return Path.Combine(AppContext.BaseDirectory, AppExeName);
    }

    public static string? GetInstallDirectory()
    {
        if (!OperatingSystem.IsWindows())
            return null;

        using var key = Registry.LocalMachine.OpenSubKey(RegistryKeyPath);
        var installDir = key?.GetValue("InstallDir") as string;
        return string.IsNullOrWhiteSpace(installDir) ? null : installDir;
    }

    public static string? GetUninstallerPath()
    {
        if (!OperatingSystem.IsWindows())
            return null;

        var installDir = GetInstallDirectory();
        if (installDir is not null)
        {
            var candidate = Path.Combine(installDir, "Uninstall.exe");
            if (File.Exists(candidate))
                return candidate;
        }

        using var key = Registry.LocalMachine.OpenSubKey(UninstallRegistryKeyPath);
        var uninstallString = key?.GetValue("UninstallString") as string;
        if (string.IsNullOrWhiteSpace(uninstallString))
            return null;

        return uninstallString.Trim().Trim('"');
    }

    public static string GetDefaultInstalledExecutablePath()
    {
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        return Path.Combine(programFiles, DefaultInstallRelativePath);
    }
}
