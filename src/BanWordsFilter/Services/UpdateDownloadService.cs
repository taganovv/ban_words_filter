using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using BanWordsFilter.Models;

namespace BanWordsFilter.Services;

public sealed class UpdateDownloadService
{
    public async Task<string> DownloadInstallerAsync(
        string downloadUrl,
        IProgress<DownloadProgress> progress,
        CancellationToken cancellationToken = default)
    {
        var destinationDirectory = Path.Combine(Path.GetTempPath(), "BanWordsFilter");
        Directory.CreateDirectory(destinationDirectory);

        var destinationPath = Path.Combine(destinationDirectory, "Ban_Words_Filter_Setup.exe");
        if (File.Exists(destinationPath))
            File.Delete(destinationPath);

        using var client = CreateDownloadClient();
        using var response = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength;
        await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);

        var buffer = new byte[81920];
        long received = 0;
        int read;

        while ((read = await contentStream.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            received += read;
            progress.Report(new DownloadProgress(received, totalBytes));
        }

        return destinationPath;
    }

    private static HttpClient CreateDownloadClient()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.0";
        var client = new HttpClient { Timeout = TimeSpan.FromMinutes(30) };
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("BanWordsFilter", version));
        return client;
    }
}
