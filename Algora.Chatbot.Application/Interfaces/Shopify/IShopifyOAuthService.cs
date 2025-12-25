namespace Algora.Chatbot.Application.Interfaces.Shopify;

public interface IShopifyOAuthService
{
    string GetAuthorizationUrl(string shopDomain, string redirectUri, string state);
    Task<string> ExchangeCodeForTokenAsync(string shopDomain, string code);
    bool ValidateHmac(string queryString, string hmac);
}
