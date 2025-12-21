using Algora.Application.DTOs;

namespace Algora.Application.Interfaces
{
    /// <summary>
    /// Provides operations to query abandoned carts and to send reminder messages for checkouts.
    /// Implementations should map Shopify/Platform abandoned checkout data to <see cref="AbandonedCartDto"/>
    /// and handle delivery (email/SMS) for reminders.
    /// </summary>
    public interface IAbandonedCartService
    {
        /// <summary>
        /// Retrieves abandoned carts (checkouts) optionally filtered by a starting date/time.
        /// </summary>
        /// <param name="since">
        /// If provided, only carts abandoned on or after this <see cref="DateTime"/> are returned.
        /// If <c>null</c>, the implementation may return a reasonable recent window of abandoned carts.
        /// </param>
        /// <returns>
        /// A task that resolves to an enumerable of <see cref="AbandonedCartDto"/> describing each abandoned cart.
        /// </returns>
        Task<IEnumerable<AbandonedCartDto>> GetAllAsync(DateTime? since = null);

        /// <summary>
        /// Sends a reminder for a specific abandoned checkout.
        /// </summary>
        /// <param name="checkoutId">The platform-specific checkout identifier (Shopify checkout id).</param>
        /// <returns>
        /// A task that resolves to <c>true</c> if the reminder was successfully queued/sent; otherwise <c>false</c>.
        /// Implementations should handle transient failures and return <c>false</c> only when the operation definitively failed.
        /// </returns>
        Task<bool> SendReminderAsync(long checkoutId);
    }
}
