using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using BanWordsFilter.Models;

namespace BanWordsFilter.Services;

public static class RegexPatternGenerator
{
    private static readonly HashSet<char> RegexSpecialChars =
        ['.', '^', '$', '*', '+', '?', '(', ')', '[', ']', '{', '}', '|', '\\'];

    public static string Generate(string word, NormalizationConfig normalization)
    {
        if (string.IsNullOrWhiteSpace(word))
            return "";

        var latin = TransliterateToLatin(word.Trim().ToLowerInvariant(), normalization);
        if (string.IsNullOrEmpty(latin))
            latin = word.Trim().ToLowerInvariant();

        var variantMap = BuildVariantMap(normalization);
        var sb = new StringBuilder(latin.Length * 8);

        foreach (var ch in latin)
        {
            if (RegexSpecialChars.Contains(ch))
            {
                sb.Append('\\').Append(ch).Append('+');
                continue;
            }

            var variants = CollectVariants(ch, variantMap);
            sb.Append('[').Append(EscapeForCharClass(variants)).Append(']').Append('+');
        }

        return sb.ToString();
    }

    public static bool IsValidPattern(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
            return false;

        try
        {
            _ = new Regex(pattern, RegexOptions.IgnoreCase);
            return true;
        }
        catch (RegexParseException)
        {
            return false;
        }
    }

    public static string TransliterateToLatin(string word, NormalizationConfig normalization)
    {
        var sb = new StringBuilder(word.Length * 2);

        foreach (var ch in word)
        {
            var key = ch.ToString();
            if (normalization.HomoglyphMap.TryGetValue(key, out var mapped) && !string.IsNullOrEmpty(mapped))
            {
                sb.Append(mapped);
                continue;
            }

            if (normalization.ReplaceLeetspeak.TryGetValue(key, out var leet) && !string.IsNullOrEmpty(leet))
            {
                sb.Append(leet);
                continue;
            }

            sb.Append(char.ToLowerInvariant(ch));
        }

        return sb.ToString();
    }

    private static Dictionary<char, HashSet<char>> BuildVariantMap(NormalizationConfig normalization)
    {
        var map = new Dictionary<char, HashSet<char>>();

        void LinkVariant(char left, char right)
        {
            AddToMap(map, left, right);
            AddToMap(map, right, left);
        }

        foreach (var (from, to) in normalization.ReplaceLeetspeak)
        {
            if (from.Length == 0 || string.IsNullOrEmpty(to))
                continue;

            foreach (var target in to)
                LinkVariant(from[0], target);
        }

        foreach (var (from, to) in normalization.HomoglyphMap)
        {
            if (from.Length == 0 || string.IsNullOrEmpty(to))
                continue;

            foreach (var target in to)
                LinkVariant(from[0], target);
        }

        return map;
    }

    private static void AddToMap(Dictionary<char, HashSet<char>> map, char from, char to)
    {
        if (!map.TryGetValue(from, out var set))
        {
            set = [];
            map[from] = set;
        }

        set.Add(from);
        set.Add(to);
    }

    private static IEnumerable<char> CollectVariants(char ch, Dictionary<char, HashSet<char>> variantMap)
    {
        var result = new HashSet<char> { ch };

        if (variantMap.TryGetValue(ch, out var mapped))
        {
            foreach (var variant in mapped)
                result.Add(variant);
        }

        if (char.IsLetter(ch))
        {
            var alt = char.IsUpper(ch) ? char.ToLowerInvariant(ch) : char.ToUpperInvariant(ch);
            result.Add(alt);
        }

        return result.OrderBy(c => c);
    }

    private static string EscapeForCharClass(IEnumerable<char> chars)
    {
        var sb = new StringBuilder();
        foreach (var ch in chars)
        {
            if (ch is '-' or ']' or '\\' or '^')
                sb.Append('\\');
            sb.Append(ch);
        }

        return sb.ToString();
    }
}
