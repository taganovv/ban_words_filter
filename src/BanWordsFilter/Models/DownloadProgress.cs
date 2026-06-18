using System;

namespace BanWordsFilter.Models;

public readonly record struct DownloadProgress(long BytesReceived, long? TotalBytes)
{
    public double? Percent => TotalBytes is > 0
        ? Math.Clamp(BytesReceived * 100.0 / TotalBytes.Value, 0, 100)
        : null;
}
