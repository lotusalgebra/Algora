using Algora.Application.Interfaces;
using Algora.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Algora.Web.Controllers
{
    /// <summary>
    /// Receives Shopify webhooks and updates licensing state accordingly
    /// (for example: app uninstall or billing events).
    /// </summary>
    [ApiController]
    [Route("webhooks/shopify")]
    public class WebhookController : Controller
    {
        private readonly ILicenseService _licenseService;
        private readonly IConfiguration _config;
        private readonly IShopContext _shopContext;

        public WebhookController(ILicenseService licenseService, IConfiguration config, IShopContext shopContext)
        {
            _licenseService = licenseService;
            _config = config;
            _shopContext = shopContext;
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
                return Unauthorized();

            // Extract the webhook topic and originating shop domain from headers
            var topic = Request.Headers["X-Shopify-Topic"].ToString();
            var shopDomain = Request.Headers["X-Shopify-Shop-Domain"].ToString();

            // TODO: consider persisting a WebhookLog record here for auditing/debugging

            switch (topic)
            {
                case "app/uninstalled":
                    // App uninstalled: deactivate licensing for this shop
                    await _licenseService.DeactivateLicenseAsync(shopDomain);
                    break;

                case "recurring_application_charges/delete":
                case "recurring_application_charges/update":
                    // Charge cancelled/expired/updated: deactivate license as appropriate
                    await _licenseService.DeactivateLicenseAsync(shopDomain);
                    break;

                // Add more topics as needed (e.g., charge created/accepted to create/update license)
            }

            return Ok();
        }
    }
}
