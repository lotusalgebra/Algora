using System.Security.Cryptography;
using System.Text;
using System.Web;
using Algora.Chatbot.Application.Interfaces.Shopify;
using Microsoft.Extensions.Options;

namespace Algora.Chatbot.Infrastructure.Shopify;

public class ShopifyOAuthService : IShopifyOAuthService
{
    private readonly ShopifyOptions _options;
    private readonly HttpClient _http;

    public ShopifyOAuthService(
        IOptions<ShopifyOptions> options,
        IHttpClientFactory httpFactory)
    {
        _options = options.Value;
        _http = httpFactory.CreateClient("Shopify");
    }

    public string GetAuthorizationUrl(string shopDomain, string redirectUri, string state)
    {
        var scopes = _options.Scopes;
        var apiKey = _options.ApiKey;

        var url = $"https://{shopDomain}/admin/oauth/authorize" +
            $"?client_id={apiKey}" +
            $"&scope={HttpUtility.UrlEncode(scopes)}" +
            $"&redirect_uri={HttpUtility.UrlEncode(redirectUri)}" +
            $"&state={state}";

        return url;
    }

    public async Task<string> ExchangeCodeForTokenAsync(string shopDomain, string code)
    {
        var requestBody = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "client_id", _options.ApiKey },
            { "client_secret", _options.ApiSecret },
            { "code", code }
        });

        var response = await _http.PostAsync(
            $"https://{shopDomain}/admin/oauth/access_token",
            requestBody);

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("access_token").GetString() ?? "";
    }

    public bool ValidateHmac(string queryString, string hmac)
    {
        if (string.IsNullOrEmpty(_options.ApiSecret) || string.IsNullOrEmpty(hmac))
            return false;

        // Parse query string and rebuild without hmac
        var pairs = HttpUtility.ParseQueryString(queryString);
        pairs.Remove("hmac");

        var sortedPairs = pairs.AllKeys
            .Where(k => k != null)
            .OrderBy(k => k)
            .Select(k => $"{k}={pairs[k]}")
            .ToArray();

        var message = string.Join("&", sortedPairs);

        using var hmacsha256 = new HMACSHA256(Encoding.UTF8.GetBytes(_options.ApiSecret));
        var hash = hmacsha256.ComputeHash(Encoding.UTF8.GetBytes(message));
        var calculatedHmac = BitConverter.ToString(hash).Replace("-", "").ToLower();

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(calculatedHmac),
            Encoding.UTF8.GetBytes(hmac.ToLower()));
    }
}
