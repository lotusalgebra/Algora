using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Algora.Application.Interfaces
{
    /// <summary>
    /// Provides contextual information about the current shop for API calls.
    /// Implementations supply the shop's myshopify domain and an access token that
    /// can be used to call the Shopify Admin API. Concrete implementations may:
    /// - derive values from the current HTTP request (query string/header/cookie),
    /// - return a configured single-store token, or
    /// - read a persisted offline token from storage.
    /// </summary>
    public interface IShopContext
    {
        /// <summary>
        /// The shop's myshopify domain (for example: "example-shop.myshopify.com").
        /// Implementations should return the domain appropriate for the current request
        /// or runtime configuration.
        /// </summary>
        string ShopDomain { get; }

        /// <summary>
        /// An access token for the Shopify Admin API for the shop identified by <see cref="ShopDomain"/>.
        /// This may be an online token scoped to a user session or an offline token persisted for the store.
        /// Implementations should ensure the token is valid for API calls or throw a clear exception if unavailable.
        /// </summary>
        string AccessToken { get; }
    }
}
