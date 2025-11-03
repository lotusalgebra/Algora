using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using ShopifySharp;
using ShopifySharp.Filters;

namespace Algora.Web.Controllers
{
    [ApiController]
    [Route("api/shopify")]
    public class ShopifyController : ControllerBase
    {
        private readonly IShopifyOAuthService _oauth;
        private readonly ILogger<ShopifyController> _logger;

        public ShopifyController(IShopifyOAuthService oauth, ILogger<ShopifyController> logger)
        {
            _oauth = oauth;
            _logger = logger;
        }

        // GET /api/shopify/customers?shop=my-shop.myshopify.com&limit=10
        [HttpGet("customers")]
        public async Task<IActionResult> GetCustomers([FromQuery] string shop, [FromQuery] int limit = 25)
        {
            if (string.IsNullOrWhiteSpace(shop)) return BadRequest("shop is required");
            var token = await _oauth.GetAccessTokenAsync(shop);
            if (string.IsNullOrWhiteSpace(token)) return NotFound("Shop not installed or token not available");

            var service = new CustomerService(shop, token);
            var filter = new CustomerListFilter { Limit = limit };
            var page = await service.ListAsync(filter);
            return Ok(page.Items);
        }

        // GET /api/shopify/orders?shop=my-shop.myshopify.com&limit=10
        [HttpGet("orders")]
        public async Task<IActionResult> GetOrders([FromQuery] string shop, [FromQuery] int limit = 25)
        {
            if (string.IsNullOrWhiteSpace(shop)) return BadRequest("shop is required");
            var token = await _oauth.GetAccessTokenAsync(shop);
            if (string.IsNullOrWhiteSpace(token)) return NotFound("Shop not installed or token not available");

            var service = new OrderService(shop, token);
            var filter = new ShopifySharp.Filters.OrderListFilter { Limit = limit };
            var page = await service.ListAsync(filter);
            return Ok(page.Items);
        }
    }
}