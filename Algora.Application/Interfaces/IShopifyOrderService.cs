using Algora.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Algora.Application.Interfaces
{
    /// <summary>
    /// Abstraction for working with orders in a Shopify shop.
    /// Implementations should call the Shopify Admin API (or a wrapper) and map results
    /// to <see cref="OrderDto"/> instances, handling paging, creation and order lifecycle operations.
    /// </summary>
    public interface IShopifyOrderService
    {
        /// <summary>
        /// Retrieves a page of orders for the configured shop.
        /// </summary>
        /// <param name="limit">Maximum number of orders to return. Default is 25.</param>
        /// <returns>
        /// A task that resolves to an enumerable of <see cref="OrderDto"/> containing the orders.
        /// Implementations may return fewer items than requested depending on available data.
        /// </returns>
        Task<IEnumerable<OrderDto>> GetAllAsync(int limit = 25);

        /// <summary>
        /// Retrieves a single order by its numeric identifier.
        /// </summary>
        /// <param name="id">Numeric Shopify order id.</param>
        /// <returns>
        /// A task that resolves to the matching <see cref="OrderDto"/>, or <c>null</c> if no order is found.
        /// </returns>
        Task<OrderDto?> GetByIdAsync(long id);

        /// <summary>
        /// Creates a new order in the shop.
        /// </summary>
        /// <param name="order">
        /// The order data to create. Implementations should map this DTO to the API request payload.
        /// </param>
        /// <returns>
        /// A task that resolves to the created <see cref="OrderDto"/> (including assigned id) or <c>null</c> on failure.
        /// </returns>
        Task<OrderDto?> CreateAsync(OrderDto order);

        /// <summary>
        /// Cancels the specified order.
        /// </summary>
        /// <param name="id">Numeric Shopify order id to cancel.</param>
        /// <returns>
        /// A task that completes when the cancellation operation has finished. Implementations should
        /// handle API errors and throw or surface failures as appropriate.
        /// </returns>
        Task CancelAsync(long id);

        /// <summary>
        /// Sends an invoice (for example, creates and emails a draft invoice) related to the specified order.
        /// </summary>
        /// <param name="orderId">Numeric Shopify order id for which an invoice should be sent.</param>
        /// <returns>
        /// A task that completes when the invoice send operation is requested/queued. Implementations should
        /// ensure the appropriate Shopify API calls are performed and update any local state if needed.
        /// </returns>
        Task SendInvoiceAsync(long orderId);
    }
}
