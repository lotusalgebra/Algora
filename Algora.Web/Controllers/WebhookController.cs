using Algora.Application.Interfaces;
using Algora.Domain.Entities;
using Algora.Infrastructure;
using Algora.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;

namespace Algora.Web.Controllers
{
    /// <summary>
    /// Receives Shopify webhooks and handles:
    /// - App lifecycle events (install/uninstall, billing)
    /// - Order events (create, update, cancel, fulfill)
    /// - Customer events (create, update, delete)
    /// - Product events (create, update, delete)
    /// </summary>
    [ApiController]
    [Route("webhooks/shopify")]
    public class WebhookController : Controller
    {
        private readonly ILicenseService _licenseService;
        private readonly IWebhookSyncService _webhookSyncService;
        private readonly IConfiguration _config;
        private readonly AppDbContext _db;
        private readonly ILogger<WebhookController> _logger;

        public WebhookController(
            ILicenseService licenseService,
            IWebhookSyncService webhookSyncService,
            IConfiguration config,
            AppDbContext db,
            ILogger<WebhookController> logger)
        {
            _licenseService = licenseService;
            _webhookSyncService = webhookSyncService;
            _config = config;
            _db = db;
            _logger = logger;
        }

        /// <summary>
        /// Handles incoming Shopify webhooks.
        /// - Reads the raw request body
        /// - Verifies the HMAC signature using the configured API secret
        /// - Dispatches handling based on the X-Shopify-Topic header
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Handle()
        {
            // Read the raw request body (Shopify posts JSON payload here)
            string body = await new StreamReader(Request.Body).ReadToEndAsync();

            // Load the shared secret used to verify webhook HMAC
            var secret = _config["Shopify:ApiSecret"] ?? string.Empty;

            // Verify the webhook signature to ensure the request is from Shopify
            if (!ShopifyWebhookVerifier.IsValidWebhook(secret, Request, body))
            {
                _logger.LogWarning("Invalid webhook HMAC signature");
                return Unauthorized();
            }

            // Extract the webhook topic and originating shop domain from headers
            var topic = Request.Headers["X-Shopify-Topic"].ToString();
            var shopDomain = Request.Headers["X-Shopify-Shop-Domain"].ToString();

            _logger.LogInformation("Received webhook: {Topic} from {Shop}", topic, shopDomain);

            // Log webhook for auditing/debugging
            await LogWebhookAsync(shopDomain, topic, body);

            try
            {
                switch (topic)
                {
                    // App lifecycle webhooks
                    case "app/uninstalled":
                        await _licenseService.DeactivateLicenseAsync(shopDomain);
                        break;

                    case "recurring_application_charges/delete":
                    case "recurring_application_charges/update":
                        await _licenseService.DeactivateLicenseAsync(shopDomain);
                        break;

                    // Order webhooks
                    case "orders/create":
                        await _webhookSyncService.SyncOrderCreatedAsync(shopDomain, body);
                        break;

                    case "orders/updated":
                        await _webhookSyncService.SyncOrderUpdatedAsync(shopDomain, body);
                        break;

                    case "orders/cancelled":
                        await _webhookSyncService.SyncOrderCancelledAsync(shopDomain, body);
                        break;

                    case "orders/fulfilled":
                        await _webhookSyncService.SyncOrderFulfilledAsync(shopDomain, body);
                        break;

                    // Customer webhooks
                    case "customers/create":
                        await _webhookSyncService.SyncCustomerCreatedAsync(shopDomain, body);
                        break;

                    case "customers/update":
                        await _webhookSyncService.SyncCustomerUpdatedAsync(shopDomain, body);
                        break;

                    case "customers/delete":
                        await _webhookSyncService.SyncCustomerDeletedAsync(shopDomain, body);
                        break;

                    // Product webhooks
                    case "products/create":
                        await _webhookSyncService.SyncProductCreatedAsync(shopDomain, body);
                        break;

                    case "products/update":
                        await _webhookSyncService.SyncProductUpdatedAsync(shopDomain, body);
                        break;

                    case "products/delete":
                        await _webhookSyncService.SyncProductDeletedAsync(shopDomain, body);
                        break;

                    default:
                        _logger.LogInformation("Unhandled webhook topic: {Topic}", topic);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing webhook {Topic} for shop {Shop}", topic, shopDomain);
                // Still return OK to prevent Shopify from retrying
                // The webhook is logged for manual inspection
            }

            return Ok();
        }

        private async Task LogWebhookAsync(string shopDomain, string topic, string body)
        {
            try
            {
                var log = new WebhookLog
                {
                    Shop = shopDomain,
                    Topic = topic,
                    Payload = body.Length > 10000 ? body[..10000] : body, // Truncate if too large
                    ReceivedAt = DateTime.UtcNow
                };
                _db.WebhookLogs.Add(log);
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log webhook");
            }
        }
    }
}
