using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace BanWordsFilter.Services;

public sealed class UpdateApplyService
{
    public void ScheduleSeamlessUpdate(string installerPath)
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Автообновление поддерживается только в Windows.");

        if (!File.Exists(installerPath))
            throw new FileNotFoundException("Файл установщика не найден.", installerPath);

        var workDirectory = Path.Combine(Path.GetTempPath(), "BanWordsFilter");
        Directory.CreateDirectory(workDirectory);

        var scriptPath = Path.Combine(workDirectory, "apply-update.ps1");
        var logPath = Path.Combine(workDirectory, "apply-update.log");
        File.WriteAllText(scriptPath, BuildUpdaterScript(), Encoding.UTF8);

        var fallbackExe = InstalledAppLocator.GetExecutablePath();
        var defaultExe = InstalledAppLocator.GetDefaultInstalledExecutablePath();
        var uninstallerPath = InstalledAppLocator.GetUninstallerPath() ?? string.Empty;

        var arguments = new StringBuilder()
            .Append("-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden ")
            .Append("-File \"").Append(scriptPath).Append('"')
            .Append(" -ProcessId ").Append(Environment.ProcessId)
            .Append(" -InstallerPath \"").Append(installerPath).Append('"')
            .Append(" -FallbackExe \"").Append(fallbackExe).Append('"')
            .Append(" -DefaultExe \"").Append(defaultExe).Append('"')
            .Append(" -UninstallerPath \"").Append(uninstallerPath).Append('"')
            .Append(" -LogPath \"").Append(logPath).Append('"')
            .ToString();

        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        if (Process.Start(startInfo) is null)
            throw new InvalidOperationException("Не удалось запустить процесс обновления.");
    }

    private static string BuildUpdaterScript()
    {
        return """
            param(
              [Parameter(Mandatory = $true)][int]$ProcessId,
              [Parameter(Mandatory = $true)][string]$InstallerPath,
              [Parameter(Mandatory = $true)][string]$FallbackExe,
              [Parameter(Mandatory = $true)][string]$DefaultExe,
              [string]$UninstallerPath,
              [Parameter(Mandatory = $true)][string]$LogPath
            )

            $ErrorActionPreference = 'Continue'

            function Write-Log {
              param([string]$Message)
              $line = "[{0}] {1}" -f (Get-Date -Format "yyyy-MM-dd HH:mm:ss"), $Message
              Add-Content -LiteralPath $LogPath -Value $line -Encoding UTF8
            }

            function Get-InstalledExe {
              $registryKey = 'HKLM:\Software\BanWordsFilter'
              if (Test-Path $registryKey) {
                $installDir = (Get-ItemProperty -Path $registryKey -ErrorAction SilentlyContinue).InstallDir
                if ($installDir) {
                  $candidate = Join-Path $installDir 'BanWordsFilter.exe'
                  if (Test-Path -LiteralPath $candidate) { return $candidate }
                }
              }

              if ($FallbackExe -and (Test-Path -LiteralPath $FallbackExe)) { return $FallbackExe }
              if ($DefaultExe -and (Test-Path -LiteralPath $DefaultExe)) { return $DefaultExe }
              return $null
            }

            function Invoke-Elevated {
              param(
                [Parameter(Mandatory = $true)][string]$FilePath,
                [string]$ArgumentList = ""
              )

              $process = Start-Process -FilePath $FilePath -ArgumentList $ArgumentList -Verb RunAs -Wait -PassThru
              if ($null -eq $process) {
                throw "Не удалось запустить: $FilePath"
              }

              if ($process.ExitCode -ne 0) {
                throw "Процесс завершился с кодом $($process.ExitCode): $FilePath"
              }
            }

            try {
              Write-Log "Ожидание завершения приложения (PID=$ProcessId)"
              Wait-Process -Id $ProcessId -ErrorAction SilentlyContinue
              Start-Sleep -Seconds 1

              if ($UninstallerPath -and (Test-Path -LiteralPath $UninstallerPath)) {
                Write-Log "Удаление предыдущей версии: $UninstallerPath"
                Invoke-Elevated -FilePath $UninstallerPath -ArgumentList '/S'
                Start-Sleep -Seconds 1
              }

              Write-Log "Установка новой версии: $InstallerPath"
              Invoke-Elevated -FilePath $InstallerPath -ArgumentList '/S'
              Start-Sleep -Seconds 1

              $exePath = Get-InstalledExe
              if (-not $exePath) {
                throw "Не удалось найти установленный BanWordsFilter.exe"
              }

              Write-Log "Запуск новой версии: $exePath"
              Start-Process -FilePath $exePath
              Write-Log "Обновление завершено успешно"
            }
            catch {
              Write-Log ("Ошибка обновления: " + $_.Exception.Message)
              exit 1
            }
            finally {
              Remove-Item -LiteralPath $InstallerPath -Force -ErrorAction SilentlyContinue
            }
            """;
    }
}
