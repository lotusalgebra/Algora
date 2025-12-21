using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Algora.Application.Interfaces
{
    /// <summary>
    /// Lightweight Graph API helper for executing GraphQL queries and mutations
    /// against the Shopify Admin GraphQL endpoint.
    /// Implementations should call Shopify's /admin/api/{version}/graphql.json endpoint
    /// and return the raw response payload (typically JSON).
    /// </summary>
    public interface IShopifyGraphService
    {
        /// <summary>
        /// Executes a GraphQL query or mutation for the specified shop using the provided access token.
        /// </summary>
        /// <param name="shopDomain">The shop's myshopify domain (for example "example-shop.myshopify.com").</param>
        /// <param name="accessToken">A valid access token for the shop (offline or online token).</param>
        /// <param name="query">GraphQL query or mutation string.</param>
        /// <param name="variables">Optional variables object to send with the GraphQL request.</param>
        /// <returns>
        /// A task that resolves to the raw response body as a string (usually JSON). The caller
        /// is responsible for deserializing and handling GraphQL errors contained in the response.
        /// </returns>
        /// <remarks>
        /// Implementations should set the required headers (for example: X-Shopify-Access-Token,
        /// Content-Type: application/json) and propagate HTTP error status as exceptions or return
        /// the response body for callers to inspect. Consider adding a generic variant that returns
        /// a typed result if the project needs structured deserialization.
        /// </remarks>
        Task<string> PostAsync(string shopDomain, string accessToken, string query, object? variables = null);
    }

}
