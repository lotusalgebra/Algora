using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Algora.Application.Interfaces
{
    /// <summary>
    /// Handles the OAuth token exchange and retrieval for installed Shopify shops.
    /// Implementations should exchange the temporary installation code for a permanent
    /// (offline) access token and persist it for later API calls.
    /// </summary>
    public interface IShopifyOAuthService
    {
        /// <summary>
        /// Exchanges the temporary OAuth authorization code provided by Shopify for an access token.
        /// The implementation is expected to persist the returned token (offline token) for the shop.
        /// </summary>
        /// <param name="shopDomain">
        /// The shop's myshopify domain (for example "example-shop.myshopify.com") that performed the install.
        /// </param>
        /// <param name="code">
        /// The temporary authorization code received from Shopify in the OAuth callback.
        /// </param>
        /// <returns>
        /// A task that resolves to the access token string returned by Shopify. Throws on failure.
        /// </returns>
        Task<string> ExchangeCodeForTokenAsync(string shopDomain, string code);

        /// <summary>
        /// Retrieves the stored access token for a previously installed shop.
        /// </summary>
        /// <param name="shopDomain">
        /// The shop's myshopify domain to look up the token for.
        /// </param>
        /// <returns>
        /// A task that resolves to the stored access token, or <c>null</c> if no token is available.
        /// </returns>
        Task<string?> GetAccessTokenAsync(string shopDomain);
    }

}
