using System;
using System.IO;

namespace BanWordsFilter.Services;

public static class BotDirectory
{
    public static string DataDirectory()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Ban Words Filter");
        Directory.CreateDirectory(dir);
        return dir;
    }
}
