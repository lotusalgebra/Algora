using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Algora.Application.Interfaces;

/// <summary>
/// Service for Shopify OAuth authentication flow.
/// Supports per-shop custom credentials.
/// </summary>
public interface IShopifyOAuthService
{
    /// <summary>
    /// Exchanges an authorization code for an access token.
    /// </summary>
    Task<string> ExchangeCodeForTokenAsync(string shopDomain, string code);

    /// <summary>
    /// Gets the stored access token for a shop.
    /// </summary>
    Task<string?> GetAccessTokenAsync(string shopDomain);

    /// <summary>
    /// Gets the OAuth authorization URL for a shop (uses shop-specific credentials if configured).
    /// </summary>
    Task<string> GetAuthorizationUrlAsync(string shopDomain, string state);

    /// <summary>
    /// Validates HMAC signature using shop-specific credentials.
    /// </summary>
    Task<bool> ValidateHmacAsync(string shopDomain, string message, string hmac);
}
