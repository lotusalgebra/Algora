using Algora.Application.Interfaces;
using Algora.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Algora.Web.Controllers
{
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

        [HttpPost]
        public async Task<IActionResult> Handle()
        {
            string body = await new StreamReader(Request.Body).ReadToEndAsync();
            var secret = _config["Shopify:ApiSecret"] ?? string.Empty;

            if (!ShopifyWebhookVerifier.IsValidWebhook(secret, Request, body))
                return Unauthorized();

            var topic = Request.Headers["X-Shopify-Topic"].ToString();
            var shopDomain = Request.Headers["X-Shopify-Shop-Domain"].ToString();

            switch (topic)
            {
                case "app/uninstalled":
                    await _licenseService.DeactivateLicenseAsync(shopDomain);
                    break;

                case "recurring_application_charges/delete":
                case "recurring_application_charges/update":
                    // On charge cancelled/expired, deactivate license
                    await _licenseService.DeactivateLicenseAsync(shopDomain);
                    break;

                    // Add more topics as needed
            }

            return Ok();
        }
    }
}
