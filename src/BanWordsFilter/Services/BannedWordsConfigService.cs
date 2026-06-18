using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using BanWordsFilter.Models;

namespace BanWordsFilter.Services;

public sealed class BannedWordsConfigService
{
    public const string UserCustomCategory = "user_custom";

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private static string UserListsPath => Path.Combine(BotDirectory.DataDirectory(), "user-word-lists.json");

    public BannedWordsConfig LoadMergedConfig()
    {
        var embedded = LoadEmbeddedConfig();
        var userLists = LoadUserLists();
        return Merge(embedded, userLists);
    }

    public UserWordLists LoadUserLists()
    {
        if (!File.Exists(UserListsPath))
            return new UserWordLists();

        try
        {
            var json = File.ReadAllText(UserListsPath);
            return JsonSerializer.Deserialize<UserWordLists>(json) ?? new UserWordLists();
        }
        catch
        {
            return new UserWordLists();
        }
    }

    public void SaveUserLists(UserWordLists lists)
    {
        Directory.CreateDirectory(BotDirectory.DataDirectory());
        File.WriteAllText(UserListsPath, JsonSerializer.Serialize(lists, JsonOptions));
    }

    public void ClearUserLists()
    {
        try { File.Delete(UserListsPath); } catch { }
    }

    public IReadOnlyList<WordListEntry> GetBlacklistEntries(BannedWordsConfig config, UserWordLists userLists)
    {
        var removedExact = userLists.Removed.BlacklistExact.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var removedRegex = userLists.Removed.BlacklistRegex.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var entries = new List<WordListEntry>();

        foreach (var (categoryName, category) in config.Categories)
        {
            if (categoryName == UserCustomCategory)
                continue;

            var label = string.IsNullOrWhiteSpace(category.Label)
                ? categoryName.Replace('_', ' ')
                : category.Label!;

            foreach (var word in category.Exact)
            {
                if (removedExact.Contains(word))
                    continue;

                entries.Add(new WordListEntry
                {
                    Text = word,
                    Type = "exact",
                    Category = label,
                    IsUserAdded = false,
                });
            }

            if (userLists.Removed.BlacklistRegexAll)
                continue;

            foreach (var pattern in category.Regex)
            {
                if (removedRegex.Contains(pattern))
                    continue;

                entries.Add(new WordListEntry
                {
                    Text = pattern,
                    Type = "regex",
                    Category = label,
                    IsUserAdded = false,
                });
            }
        }

        foreach (var word in userLists.Blacklist.Exact)
        {
            entries.Add(new WordListEntry
            {
                Text = word,
                Type = "exact",
                Category = "пользовательские",
                IsUserAdded = true,
            });
        }

        foreach (var pattern in userLists.Blacklist.Regex)
        {
            var linkedWord = userLists.BlacklistRegexLinks
                .FirstOrDefault(pair => pair.Value.Equals(pattern, StringComparison.OrdinalIgnoreCase))
                .Key;

            entries.Add(new WordListEntry
            {
                Text = pattern,
                Type = "regex",
                Category = "пользовательские",
                IsUserAdded = true,
                LinkedWord = string.IsNullOrEmpty(linkedWord) ? null : linkedWord,
            });
        }

        return entries
            .OrderBy(entry => entry.Category, StringComparer.OrdinalIgnoreCase)
            .ThenBy(entry => entry.Type, StringComparer.OrdinalIgnoreCase)
            .ThenBy(entry => entry.Text, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public IReadOnlyList<WordListEntry> GetWhitelistEntries(BannedWordsConfig config, UserWordLists userLists)
    {
        var removed = userLists.Removed.WhitelistExact.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var entries = new List<WordListEntry>();

        foreach (var word in config.Whitelist.Exact)
        {
            if (removed.Contains(word))
                continue;

            entries.Add(new WordListEntry
            {
                Text = word,
                Type = "exact",
                Category = "встроенный",
                IsUserAdded = false,
            });
        }

        foreach (var word in userLists.Whitelist.Exact)
        {
            entries.Add(new WordListEntry
            {
                Text = word,
                Type = "exact",
                Category = "пользовательские",
                IsUserAdded = true,
            });
        }

        return entries
            .OrderBy(entry => entry.Text, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public (bool Ok, string Message) AddBlacklistWord(UserWordLists lists, BannedWordsConfig config, string word, bool autoRegex)
    {
        var trimmed = word.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return (false, "Введите слово");

        var normalized = trimmed.ToLowerInvariant();
        var existing = GetBlacklistEntries(config, lists);
        if (existing.Any(entry => entry.Type == "exact" && entry.Text.Equals(normalized, StringComparison.OrdinalIgnoreCase)))
            return (false, "Это слово уже в чёрном списке");

        lists.Blacklist.Exact.Add(normalized);

        string? addedPattern = null;
        if (autoRegex)
        {
            var pattern = RegexPatternGenerator.Generate(normalized, config.Normalization);
            if (!RegexPatternGenerator.IsValidPattern(pattern))
            {
                SaveUserLists(lists);
                return (true, $"Добавлено: {normalized} (не удалось создать regex)");
            }

            if (!lists.Blacklist.Regex.Any(p => p.Equals(pattern, StringComparison.OrdinalIgnoreCase)))
                lists.Blacklist.Regex.Add(pattern);

            lists.BlacklistRegexLinks[normalized] = pattern;
            addedPattern = pattern;
        }

        SaveUserLists(lists);

        if (autoRegex && addedPattern is not null)
            return (true, $"Добавлено: {normalized} (+ regex)");

        return (true, $"Добавлено: {normalized}");
    }

    public (bool Ok, string Message) AddWhitelistWord(UserWordLists lists, BannedWordsConfig config, string word)
    {
        var trimmed = word.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return (false, "Введите слово или фразу");

        var existing = GetWhitelistEntries(config, lists);
        if (existing.Any(entry => entry.Text.Equals(trimmed, StringComparison.OrdinalIgnoreCase)))
            return (false, "Эта фраза уже в белом списке");

        lists.Whitelist.Exact.Add(trimmed);
        SaveUserLists(lists);
        return (true, $"Добавлено в белый список: {trimmed}");
    }

    public (bool Ok, string Message) RemoveBlacklistEntry(UserWordLists lists, WordListEntry entry)
    {
        RemoveBlacklistEntryInternal(lists, entry);
        SaveUserLists(lists);
        return (true, "Удалено из чёрного списка");
    }

    public (bool Ok, string Message) RemoveWhitelistEntry(UserWordLists lists, WordListEntry entry)
    {
        if (entry.IsUserAdded)
        {
            lists.Whitelist.Exact.RemoveAll(w => w.Equals(entry.Text, StringComparison.OrdinalIgnoreCase));
        }
        else if (!lists.Removed.WhitelistExact.Contains(entry.Text, StringComparer.OrdinalIgnoreCase))
        {
            lists.Removed.WhitelistExact.Add(entry.Text);
        }

        SaveUserLists(lists);
        return (true, "Удалено из белого списка");
    }

    public (bool Ok, string Message, int Count) RemoveBlacklistRegexForSearch(
        UserWordLists lists,
        BannedWordsConfig config,
        string searchQuery)
    {
        var query = searchQuery.Trim();
        if (string.IsNullOrEmpty(query))
            return (false, "Введите слово в поле поиска", 0);

        var all = GetBlacklistEntries(config, lists);
        var regexEntries = FilterBlacklistEntries(all, query, config.Normalization)
            .Where(entry => entry.Type == "regex")
            .GroupBy(entry => entry.Text, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();

        if (regexEntries.Count == 0)
            return (false, $"Regex для «{query}» не найден", 0);

        foreach (var entry in regexEntries)
            RemoveBlacklistEntryInternal(lists, entry);

        SaveUserLists(lists);
        return (true, $"Удалено regex для «{query}»: {regexEntries.Count}", regexEntries.Count);
    }

    public static IReadOnlyList<WordListEntry> FilterBlacklistEntries(
        IReadOnlyList<WordListEntry> entries,
        string query,
        NormalizationConfig normalization)
    {
        var trimmed = query.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return entries;

        var matchedExactWords = entries
            .Where(entry => entry.Type == "exact" && WordMatchesQuery(entry.Text, trimmed))
            .Select(entry => entry.Text)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var matchedCategories = entries
            .Where(entry => entry.Type == "exact" && WordMatchesQuery(entry.Text, trimmed))
            .Select(entry => entry.Category)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var linkedPatterns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var word in matchedExactWords)
        {
            foreach (var regexEntry in entries.Where(entry => entry.Type == "regex"))
            {
                if (IsRegexLinkedToWord(regexEntry, word, normalization))
                    linkedPatterns.Add(regexEntry.Text);
            }
        }

        return entries
            .Where(entry =>
                EntryMatchesQuery(entry, trimmed)
                || entry.Type == "regex" && matchedCategories.Contains(entry.Category)
                || entry.Type == "regex" && linkedPatterns.Contains(entry.Text)
                || entry.Type == "regex" && entry.LinkedWord is not null && WordMatchesQuery(entry.LinkedWord, trimmed))
            .ToList();
    }

    private static bool WordMatchesQuery(string word, string query)
        => word.Equals(query, StringComparison.OrdinalIgnoreCase)
           || word.Contains(query, StringComparison.OrdinalIgnoreCase);

    private static bool EntryMatchesQuery(WordListEntry entry, string query)
        => entry.Text.Contains(query, StringComparison.OrdinalIgnoreCase)
           || entry.Category.Contains(query, StringComparison.OrdinalIgnoreCase)
           || entry.Type.Contains(query, StringComparison.OrdinalIgnoreCase)
           || entry.LinkedWord is not null && entry.LinkedWord.Contains(query, StringComparison.OrdinalIgnoreCase);

    private static bool IsRegexLinkedToWord(WordListEntry regexEntry, string word, NormalizationConfig normalization)
    {
        if (regexEntry.LinkedWord?.Equals(word, StringComparison.OrdinalIgnoreCase) == true)
            return true;

        var generated = RegexPatternGenerator.Generate(word, normalization);
        return generated.Equals(regexEntry.Text, StringComparison.OrdinalIgnoreCase);
    }

    private static void RemoveBlacklistEntryInternal(UserWordLists lists, WordListEntry entry)
    {
        if (entry.IsUserAdded)
        {
            if (entry.Type == "exact")
            {
                lists.Blacklist.Exact.RemoveAll(w => w.Equals(entry.Text, StringComparison.OrdinalIgnoreCase));
                if (lists.BlacklistRegexLinks.TryGetValue(entry.Text, out var linkedPattern))
                {
                    lists.Blacklist.Regex.RemoveAll(p => p.Equals(linkedPattern, StringComparison.OrdinalIgnoreCase));
                    lists.BlacklistRegexLinks.Remove(entry.Text);
                }
            }
            else
            {
                lists.Blacklist.Regex.RemoveAll(w => w.Equals(entry.Text, StringComparison.OrdinalIgnoreCase));
                var linkedWord = lists.BlacklistRegexLinks
                    .FirstOrDefault(pair => pair.Value.Equals(entry.Text, StringComparison.OrdinalIgnoreCase))
                    .Key;
                if (!string.IsNullOrEmpty(linkedWord))
                    lists.BlacklistRegexLinks.Remove(linkedWord);
            }
        }
        else if (entry.Type == "exact")
        {
            if (!lists.Removed.BlacklistExact.Contains(entry.Text, StringComparer.OrdinalIgnoreCase))
                lists.Removed.BlacklistExact.Add(entry.Text);
        }
        else if (!lists.Removed.BlacklistRegex.Contains(entry.Text, StringComparer.OrdinalIgnoreCase))
        {
            lists.Removed.BlacklistRegex.Add(entry.Text);
        }
    }

    private static BannedWordsConfig Merge(BannedWordsConfig embedded, UserWordLists userLists)
    {
        var config = CloneConfig(embedded);
        EnsureUserCustomCategory(config);

        var removedExact = userLists.Removed.BlacklistExact.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var removedRegex = userLists.Removed.BlacklistRegex.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var removedWhitelist = userLists.Removed.WhitelistExact.ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var category in config.Categories.Values)
        {
            category.Exact.RemoveAll(word => removedExact.Contains(word));
            if (userLists.Removed.BlacklistRegexAll)
                category.Regex.Clear();
            else
                category.Regex.RemoveAll(pattern => removedRegex.Contains(pattern));
        }

        config.Whitelist.Exact.RemoveAll(word => removedWhitelist.Contains(word));

        var userCategory = config.Categories[UserCustomCategory];
        foreach (var word in userLists.Blacklist.Exact)
        {
            if (!userCategory.Exact.Contains(word, StringComparer.OrdinalIgnoreCase))
                userCategory.Exact.Add(word);
        }

        foreach (var pattern in userLists.Blacklist.Regex)
        {
            if (!userCategory.Regex.Contains(pattern, StringComparer.OrdinalIgnoreCase))
                userCategory.Regex.Add(pattern);
        }

        foreach (var word in userLists.Whitelist.Exact)
        {
            if (!config.Whitelist.Exact.Contains(word, StringComparer.OrdinalIgnoreCase))
                config.Whitelist.Exact.Add(word);
        }

        return config;
    }

    private static void EnsureUserCustomCategory(BannedWordsConfig config)
    {
        if (config.Categories.ContainsKey(UserCustomCategory))
            return;

        config.Categories[UserCustomCategory] = new CategoryConfig
        {
            Label = "пользовательские",
            Severity = "high",
            Action = "ban",
            Exact = [],
            Regex = [],
        };
    }

    private static BannedWordsConfig CloneConfig(BannedWordsConfig source)
        => JsonSerializer.Deserialize<BannedWordsConfig>(JsonSerializer.Serialize(source)) ?? new BannedWordsConfig();

    public static BannedWordsConfig LoadEmbeddedConfig()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames()
            .First(name => name.EndsWith("banned-words.json", StringComparison.OrdinalIgnoreCase));

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException("Embedded banned-words.json not found.");

        return JsonSerializer.Deserialize<BannedWordsConfig>(stream)
            ?? throw new InvalidOperationException("Failed to parse banned-words.json.");
    }
}
