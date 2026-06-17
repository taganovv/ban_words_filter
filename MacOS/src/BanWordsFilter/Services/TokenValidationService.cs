using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using BanWordsFilter.Models;

namespace BanWordsFilter.Services;

public sealed class TokenValidationService
{
    private static readonly HashSet<string> RequiredScopes =
    [
        "chat:read",
        "chat:edit",
        "moderator:manage:banned_users",
        "user:bot",
    ];

    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(15) };

    public async Task<TokenInfo> ValidateAsync(string token, string clientId)
    {
        token = SettingsService.NormalizeOAuthToken(token);
        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException("OAuth Token пустой");

        using var request = new HttpRequestMessage(HttpMethod.Get, "https://id.twitch.tv/oauth2/validate");
        request.Headers.Authorization = new AuthenticationHeaderValue("OAuth", token);

        using var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Токен недействителен (HTTP {(int)response.StatusCode})");

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        var root = document.RootElement;

        var scopes = root.TryGetProperty("scopes", out var scopesElement)
            ? scopesElement.EnumerateArray().Select(x => x.GetString() ?? "").Where(x => x.Length > 0).ToHashSet()
            : new HashSet<string>();

        var missing = RequiredScopes.Where(scope => !scopes.Contains(scope)).OrderBy(x => x).ToList();

        return new TokenInfo
        {
            Login = root.TryGetProperty("login", out var login) ? login.GetString() : "",
            UserId = root.TryGetProperty("user_id", out var userId) ? userId.GetString() : "",
            Valid = missing.Count == 0,
            MissingScopes = missing,
        };
    }
}
