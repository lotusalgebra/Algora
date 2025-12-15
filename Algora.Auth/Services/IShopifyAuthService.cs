using Algora.Auth.Models;

namespace Algora.Auth.Services;

public interface IShopifyAuthService
{
    Task<string> GetInstallUrlAsync(string shopDomain, string state);
    Task<ShopifyAuthResponse> HandleCallbackAsync(ShopifyCallbackRequest request, string savedState);
    Task<bool> ValidateHmacAsync(string shopDomain, IDictionary<string, string> queryParams, string hmac);
    Task<ShopifyAuthResponse> CreateOrUpdateShopUserAsync(string shopDomain, string accessToken);
}