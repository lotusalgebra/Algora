using Algora.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Algora.Application.Interfaces
{
    /// <summary>
    /// Defines operations for working with Shopify customers.
    /// Implementations should call the Shopify Admin API (or a wrapper library)
    /// and map results to <see cref="CustomerDto"/>.
    /// </summary>
    public interface IShopifyCustomerService
    {
        /// <summary>
        /// Retrieves a page of customers from the shop.
        /// </summary>
        /// <param name="limit">Maximum number of customers to return. Default is 25.</param>
        /// <returns>
        /// A task that resolves to an enumerable of <see cref="CustomerDto"/>.
        /// The implementation may return fewer items than <paramref name="limit"/>.
        /// </returns>
        Task<IEnumerable<CustomerDto>> GetAllAsync(int limit = 25);
        
        /// <summary>
        /// Retrieves a single customer by Shopify id.
        /// </summary>
        /// <param name="id">Numeric Shopify customer id.</param>
        /// <returns>
        /// A task that resolves to the matching <see cref="CustomerDto"/>, or null if no customer is found.
        /// </returns>
        Task<CustomerDto?> GetByIdAsync(long id);
        
        /// <summary>
        /// Creates a new customer in Shopify.
        /// </summary>
        /// <param name="customer">Customer data to create. The Id property is ignored for creation.</param>
        /// <returns>
        /// A task that resolves to the created <see cref="CustomerDto"/> containing the assigned Shopify id,
        /// or null if creation failed.
        /// </returns>
        Task<CustomerDto?> CreateAsync(CustomerDto customer);
        
        /// <summary>
        /// Updates an existing customer identified by <paramref name="id"/>.
        /// </summary>
        /// <param name="id">Numeric Shopify customer id to update.</param>
        /// <param name="customer">Customer data to update. Fields set on this DTO will be applied.</param>
        /// <returns>
        /// A task that resolves to the updated <see cref="CustomerDto"/>, or null if the customer was not found.
        /// </returns>
        Task<CustomerDto?> UpdateAsync(long id, CustomerDto customer);
        
        /// <summary>
        /// Deletes a customer from Shopify.
        /// </summary>
        /// <param name="id">Numeric Shopify customer id to delete.</param>
        /// <returns>A task that completes when the deletion has finished.</returns>
        Task DeleteAsync(long id);
    }
}
