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

        /// <summary>
        /// Updates an existing order's editable fields (note, tags, email, etc.).
        /// </summary>
        /// <param name="input">The order update input containing the order ID and fields to update.</param>
        /// <returns>A task that resolves to the updated <see cref="OrderDto"/>.</returns>
        Task<OrderDto?> UpdateAsync(UpdateOrderInput input);

        /// <summary>
        /// Closes an order. Note: Shopify does not allow true deletion of orders.
        /// </summary>
        /// <param name="id">Numeric Shopify order id to close.</param>
        /// <returns>A task that completes when the order is closed.</returns>
        Task CloseAsync(long id);
    }

    /// <summary>
    /// Input model for updating an existing order.
    /// </summary>
    public class UpdateOrderInput
    {
        public long OrderId { get; set; }
        public string? Email { get; set; }
        public string? Note { get; set; }
        public string? Tags { get; set; }
        public string? ShippingName { get; set; }
        public string? ShippingAddress1 { get; set; }
        public string? ShippingAddress2 { get; set; }
        public string? ShippingCity { get; set; }
        public string? ShippingProvince { get; set; }
        public string? ShippingCountry { get; set; }
        public string? ShippingZip { get; set; }
        public string? ShippingPhone { get; set; }
    }
}
