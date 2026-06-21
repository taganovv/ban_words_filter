using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using BanWordsFilter.Models;

namespace BanWordsFilter.Services;

public sealed class BanHistoryService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private static string HistoryPath => Path.Combine(BotDirectory.DataDirectory(), "ban-history.json");

    public void Record(BanRecord record)
    {
        var records = LoadAll();
        records.Add(record);
        SaveAll(records);
    }

    public IReadOnlyList<BanRecord> GetTodayActiveBans()
    {
        var today = DateTime.Today;
        return LoadAll()
            .Where(record => record.BannedAt.Date == today && !record.Unbanned)
            .OrderByDescending(record => record.BannedAt)
            .ToList();
    }

    public bool MarkUnbanned(string id)
    {
        var records = LoadAll();
        var record = records.FirstOrDefault(item => item.Id == id);
        if (record is null || record.Unbanned)
            return false;

        record.Unbanned = true;
        record.UnbannedAt = DateTime.Now;
        SaveAll(records);
        return true;
    }

    public void Clear()
    {
        try { File.Delete(HistoryPath); } catch { }
    }

    private static List<BanRecord> LoadAll()
    {
        if (!File.Exists(HistoryPath))
            return [];

        try
        {
            var json = File.ReadAllText(HistoryPath);
            return JsonSerializer.Deserialize<List<BanRecord>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static void SaveAll(List<BanRecord> records)
    {
        Directory.CreateDirectory(BotDirectory.DataDirectory());
        var trimmed = records
            .Where(record => record.BannedAt.Date >= DateTime.Today.AddDays(-7))
            .ToList();
        File.WriteAllText(HistoryPath, JsonSerializer.Serialize(trimmed, JsonOptions));
    }
}
