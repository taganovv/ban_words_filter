using System;
using System.Threading;

namespace BanWordsFilter.Services;

public static class SingleInstanceGuard
{
    private const string MutexName = @"Global\BanWordsFilter.SingleInstance";
    private static Mutex? _mutex;

    public static bool TryEnter()
    {
        if (!OperatingSystem.IsWindows())
            return true;

        try
        {
            _mutex = new Mutex(initiallyOwned: true, name: MutexName, out var createdNew);
            return createdNew;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }
}
