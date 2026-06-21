using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BanWordsFilter.Models;

namespace BanWordsFilter.Services;

public sealed class UpdateCheckService
{
    private const int MandatoryUpdateThreshold = 2;
    private static readonly HttpClient Http = CreateHttpClient();

    public Version CurrentVersion { get; } = GetCurrentVersion();

    public async Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken cancellationToken = default)
    {
        var releases = await FetchReleasesAsync(cancellationToken);
        if (releases.Count == 0)
            return UpToDate();

        var latest = releases[0];
        if (CurrentVersion >= latest.Version)
            return UpToDate();

        var newerReleaseCount = releases.Count(r => r.Version > CurrentVersion);
        var requirement = newerReleaseCount > MandatoryUpdateThreshold
            ? UpdateRequirement.Mandatory
            : UpdateRequirement.Optional;

        return new UpdateCheckResult
        {
            Requirement = requirement,
            CurrentVersion = CurrentVersion,
            LatestVersion = latest.Version,
            InstallerDownloadUrl = latest.InstallerDownloadUrl
        };
    }

    private static UpdateCheckResult UpToDate()
        => new() { Requirement = UpdateRequirement.None };

    private async Task<IReadOnlyList<ReleaseInfo>> FetchReleasesAsync(CancellationToken cancellationToken)
    {
        using var response = await Http.GetAsync(AppConstants.GithubReleasesApiUrl, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        var releases = new List<ReleaseInfo>();

        foreach (var releaseElement in document.RootElement.EnumerateArray())
        {
            if (releaseElement.TryGetProperty("draft", out var draft) && draft.GetBoolean())
                continue;

            if (releaseElement.TryGetProperty("prerelease", out var prerelease) && prerelease.GetBoolean())
                continue;

            if (!releaseElement.TryGetProperty("tag_name", out var tagNameElement))
                continue;

            var tagName = tagNameElement.GetString();
            if (string.IsNullOrWhiteSpace(tagName))
                continue;

            if (!TryParseVersion(tagName, out var version))
                continue;

            var installerDownloadUrl = FindInstallerDownloadUrl(releaseElement);

            releases.Add(new ReleaseInfo(version, installerDownloadUrl));
        }

        releases.Sort((a, b) => b.Version.CompareTo(a.Version));
        return releases;
    }

    private static string? FindInstallerDownloadUrl(JsonElement releaseElement)
    {
        if (!releaseElement.TryGetProperty("assets", out var assetsElement))
            return null;

        foreach (var asset in assetsElement.EnumerateArray())
        {
            if (!asset.TryGetProperty("name", out var nameElement))
                continue;

            var name = nameElement.GetString();
            if (name is null || !name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                continue;

            if (asset.TryGetProperty("browser_download_url", out var downloadUrlElement))
                return downloadUrlElement.GetString();
        }

        return null;
    }

    internal static bool TryParseVersion(string tagName, out Version version)
    {
        var normalized = tagName.Trim();
        if (normalized.StartsWith('v') || normalized.StartsWith('V'))
            normalized = normalized[1..];

        return Version.TryParse(normalized, out version!);
    }

    private static Version GetCurrentVersion()
    {
        var assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version;
        return assemblyVersion ?? new Version(0, 0, 0);
    }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("BanWordsFilter", GetCurrentVersion().ToString(3)));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return client;
    }

    private sealed record ReleaseInfo(Version Version, string? InstallerDownloadUrl);
}
