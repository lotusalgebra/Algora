using Algora.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Algora.Application.Interfaces
{
    /// <summary>
    /// Abstraction for working with shop invoices (draft orders / invoices) via the Shopify Admin API.
    /// Implementations should map Shopify draft orders / invoices to <see cref="InvoiceDto"/> and
    /// perform lifecycle operations such as creating, sending, completing and cancelling invoices.
    /// </summary>
    public interface IShopifyInvoiceService
    {
        /// <summary>
        /// Retrieves a page of invoices (draft orders / invoices) for the shop.
        /// </summary>
        /// <param name="limit">Maximum number of invoices to return. Defaults to 25.</param>
        /// <returns>
        /// A task that resolves to an enumerable of <see cref="InvoiceDto"/> instances.
        /// Implementations may return fewer items than requested depending on available data.
        /// </returns>
        Task<IEnumerable<InvoiceDto>> GetAllAsync(int limit = 25);

        /// <summary>
        /// Retrieves a single invoice by its numeric identifier.
        /// </summary>
        /// <param name="id">The numeric invoice (draft order) identifier.</param>
        /// <returns>
        /// A task that resolves to the matching <see cref="InvoiceDto"/> or <c>null</c> if not found.
        /// </returns>
        Task<InvoiceDto?> GetByIdAsync(long id);

        /// <summary>
        /// Creates a new invoice (draft order) for the shop and persists it in Shopify.
        /// </summary>
        /// <param name="email">Customer email to associate with the invoice.</param>
        /// <param name="title">Human-friendly title or name for the invoice/draft order.</param>
        /// <param name="price">Total price for the invoice.</param>
        /// <returns>
        /// A task that resolves to the created <see cref="InvoiceDto"/> (including assigned id),
        /// or <c>null</c> if creation failed.
        /// </returns>
        Task<InvoiceDto?> CreateInvoiceAsync(string email, string title, decimal price);

        /// <summary>
        /// Sends the invoice to the customer (for example, via Shopify's invoice email).
        /// </summary>
        /// <param name="draftOrderId">
        /// The draft order identifier in Shopify that represents the invoice to send.
        /// </param>
        /// <returns>
        /// A task that completes when the send operation has been requested. Implementations
        /// should update invoice state to indicate it was sent (for example "invoice_sent").
        /// </returns>
        Task SendInvoiceAsync(long draftOrderId);

        /// <summary>
        /// Marks the invoice (draft order) as completed. Use this when the invoice is paid or the
        /// merchant finalizes the draft order.
        /// </summary>
        /// <param name="draftOrderId">The draft order identifier to complete.</param>
        /// <returns>
        /// A task that completes when the invoice has been transitioned to the completed state.
        /// </returns>
        Task CompleteInvoiceAsync(long draftOrderId);

        /// <summary>
        /// Cancels the invoice (draft order). Implementations should call the appropriate Shopify API
        /// to cancel or delete the draft order and update any local invoice state.
        /// </summary>
        /// <param name="draftOrderId">The draft order identifier to cancel.</param>
        /// <returns>A task that completes when the cancel operation has finished.</returns>
        Task CancelInvoiceAsync(long draftOrderId);
    }
}
