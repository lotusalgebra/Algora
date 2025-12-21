using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Algora.Application.Interfaces
{
    /// <summary>
    /// Minimal GraphQL client abstraction for calling the Shopify Admin Graph API.
    /// Implementations should execute the provided GraphQL query or mutation and
    /// deserialize the response payload into the requested type <typeparamref name="T"/>.
    /// </summary>
    public interface IShopifyGraphClient
    {
        /// <summary>
        /// Executes a GraphQL query or mutation against the shop's Graph API and
        /// returns the deserialized result.
        /// </summary>
        /// <typeparam name="T">
        /// The CLR type to deserialize the GraphQL response data into.
        /// Implementations should map the GraphQL "data" section to this type.
        /// </typeparam>
        /// <param name="gql">The GraphQL query or mutation as a string.</param>
        /// <param name="variables">Optional variables to be sent with the GraphQL request.</param>
        /// <returns>
        /// A task that resolves to an instance of <typeparamref name="T"/> containing the
        /// deserialized response data, or <c>null</c> if the response did not contain
        /// the requested data or deserialization failed.
        /// </returns>
        Task<T?> QueryAsync<T>(string gql, object? variables = null);
    }
}
