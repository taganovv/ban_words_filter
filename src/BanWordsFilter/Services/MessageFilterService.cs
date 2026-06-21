using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using BanWordsFilter.Models;

namespace BanWordsFilter.Services;

public sealed class MessageFilterService
{
    private static readonly Regex ZeroWidth = new(@"[\u200b-\u200d\ufeff\u00ad]", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex BangObfuscation = new(@"^!+(?=\S)|(?<=\s)!+(?=\S)", RegexOptions.Compiled);
    private static readonly Dictionary<string, int> ActionPriority = new()
    {
        ["instant_ban"] = 3,
        ["ban"] = 2,
        ["timeout_or_ban"] = 1,
        ["timeout"] = 0,
    };

    private BannedWordsConfig _config;
    private HashSet<char> _removeChars;

    public MessageFilterService(BannedWordsConfig config)
    {
        _config = config;
        _removeChars = _config.Normalization.RemoveChars.SelectMany(x => x).ToHashSet();
    }

    public void Reload(BannedWordsConfig config)
    {
        _config = config;
        _removeChars = _config.Normalization.RemoveChars.SelectMany(x => x).ToHashSet();
    }

    public FilterResult CheckMessage(string text)
    {
        if (string.IsNullOrWhiteSpace(text) || IsWhitelisted(text))
            return new FilterResult { Banned = false, Matches = [] };

        var normalizedForms = NormalizeVariants(text);
        var matches = new List<FilterMatch>();

        foreach (var (categoryName, category) in _config.Categories)
        {
            var action = category.Action ?? _config.Meta.Action;
            foreach (var word in category.Exact)
            {
                var normalizedWord = NormalizeText(word);
                if (normalizedForms.Any(form => form.Contains(normalizedWord, StringComparison.Ordinal)))
                {
                    matches.Add(CreateMatch(categoryName, category, action, "exact", word));
                }
            }

            foreach (var pattern in category.Regex)
            {
                try
                {
                    if (normalizedForms.Any(form => Regex.IsMatch(form, pattern, RegexOptions.IgnoreCase)))
                        matches.Add(CreateMatch(categoryName, category, action, "regex", pattern));
                }
                catch (RegexParseException)
                {
                }
            }
        }

        if (matches.Count == 0)
            return new FilterResult { Banned = false, Matches = [] };

        var top = matches
            .OrderByDescending(match => ActionPriority.GetValueOrDefault(match.Action ?? "", 0))
            .First();

        var shouldBan = top.Action is "instant_ban" or "ban" or "timeout_or_ban" or "auto_ban";
        return new FilterResult
        {
            Banned = shouldBan,
            Action = top.Action,
            TopMatch = top,
            Matches = matches,
        };
    }

    public string FormatModerationReason(FilterMatch match)
        => $"Auto-mod ({match.CategoryLabel ?? match.Category})";

    private FilterMatch CreateMatch(string categoryName, CategoryConfig category, string? action, string type, string pattern)
        => new()
        {
            Category = categoryName,
            CategoryLabel = CategoryLabel(categoryName, category),
            Type = type,
            Pattern = pattern,
            Severity = category.Severity,
            Action = action,
        };

    private string CategoryLabel(string categoryName, CategoryConfig category)
        => string.IsNullOrWhiteSpace(category.Label) ? categoryName.Replace('_', ' ') : category.Label!;

    private bool IsWhitelisted(string text)
    {
        var normalized = NormalizeText(text);
        return _config.Whitelist.Exact
            .Select(NormalizeText)
            .Any(word => normalized.Contains(word, StringComparison.Ordinal));
    }

    private List<string> NormalizeVariants(string text)
    {
        var primary = NormalizeText(text);
        var variants = new List<string> { primary };

        var raw = text.ToLowerInvariant();
        if (_config.Normalization.StripZeroWidth)
            raw = ZeroWidth.Replace(raw, "");
        raw = StripBangObfuscation(raw);
        raw = FilterChars(raw);
        raw = ApplyLeetspeak(raw);
        if (!string.IsNullOrEmpty(raw) && !variants.Contains(raw))
            variants.Add(raw);

        return variants;
    }

    private string NormalizeText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return "";

        text = text.ToLowerInvariant();
        if (_config.Normalization.StripZeroWidth)
            text = ZeroWidth.Replace(text, "");

        text = StripBangObfuscation(text);
        text = ApplyHomoglyphs(text);
        text = ApplyLeetspeak(text);
        text = FilterChars(text);

        var maxRepeat = _config.Normalization.CollapseRepeatedCharsMax;
        if (maxRepeat > 0)
            text = Regex.Replace(text, $"(.)\\1{{{maxRepeat},}}", m => new string(m.Groups[1].Value[0], maxRepeat));

        return text;
    }

    private string ApplyHomoglyphs(string text)
    {
        var sb = new StringBuilder(text.Length * 2);
        foreach (var ch in text)
        {
            var key = ch.ToString();
            sb.Append(_config.Normalization.HomoglyphMap.GetValueOrDefault(key, key));
        }
        return sb.ToString();
    }

    private string ApplyLeetspeak(string text)
    {
        var sb = new StringBuilder(text.Length);
        foreach (var ch in text)
        {
            var key = ch.ToString();
            sb.Append(_config.Normalization.ReplaceLeetspeak.GetValueOrDefault(key, key));
        }
        return sb.ToString();
    }

    private string FilterChars(string text)
        => new string(text.Where(ch => !_removeChars.Contains(ch)).ToArray());

    private static string StripBangObfuscation(string text)
        => BangObfuscation.Replace(text, "");
}
