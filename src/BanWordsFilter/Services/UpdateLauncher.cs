using System.Diagnostics;
using BanWordsFilter.Models;

namespace BanWordsFilter.Services;

public static class UpdateLauncher
{
    public static void OpenUpdate(UpdateCheckResult? update)
    {
        var url = update?.InstallerDownloadUrl
                  ?? update?.ReleasePageUrl
                  ?? AppConstants.GithubReleasesUrl;

        Process.Start(new ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        });
    }
}
