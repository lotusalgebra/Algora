using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Algora.Application.Interfaces
{
    /// <summary>
    /// Abstraction for sending WhatsApp messages.
    /// Implementations should handle provider-specific details (Twilio, Meta Business API, etc.),
    /// rate limiting, retries and delivery reporting as appropriate.
    /// </summary>
    public interface IWhatsAppService
    {
        /// <summary>
        /// Sends an order-related update message to the specified phone number using WhatsApp.
        /// </summary>
        /// <param name="toPhone">
        /// Destination phone number in E.164 format (for example: "+15551234567").
        /// Implementations may validate or normalize the number before sending.
        /// </param>
        /// <param name="message">
        /// Message body to deliver. Keep content concise; if templates are required by the provider,
        /// the implementation should map or transform the text accordingly.
        /// </param>
        /// <returns>
        /// A task that completes when the send request has been queued or completed.
        /// Implementations should throw on unrecoverable errors or return a failed task when sending fails.
        /// </returns>
        Task SendOrderUpdateAsync(string toPhone, string message);
    }
}
