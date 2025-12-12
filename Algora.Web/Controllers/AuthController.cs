using Algora.Infrastructure;
using Algora.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using RazorLight;

namespace Algora.Web.Controllers
{
    /// <summary>
    /// Handles the Shopify OAuth install and callback flow.
    /// <para>
    /// Endpoints:
    /// - GET /auth/install?shop=...  -> Redirect merchant to Shopify install/consent page.
    /// - GET /auth/callback?shop=... -> OAuth callback invoked by Shopify after merchant consents.
    /// </para>
    /// Responsibilities:
    /// - Build install URL (requests scopes and state cookie).
    /// - Validate HMAC and state on callback to protect against tampering/CSRF.
    /// - Exchange temporary code for an offline access token and persist the shop record.
    /// </summary>
    /// <remarks>
    /// Constructs the controller with required dependencies.
    /// </remarks>
    /// <param name="opt">Bound Shopify options (ApiKey, ApiSecret, AppUrl, Scopes).</param>
    /// <param name="httpFactory">Http client factory used for token exchange requests.</param>
    /// <param name="db">EF Core DbContext for persisting shop/install information.</param>
    /// <param name="templateService">RazorLight template service for rendering HTML email/notifications.</param>
    [Route("auth")]
    public class AuthController(IOptions<ShopifyOptions> opt, IHttpClientFactory httpFactory, AppDbContext db, IRazorLightEngine templateService) : Controller
    {
        private readonly ShopifyOptions _opt = opt.Value;
        private readonly IHttpClientFactory _httpFactory = httpFactory;
        private readonly AppDbContext _db = db; // EF DbContext storing shops/tokens
        private readonly IRazorLightEngine _templateService = templateService;

        /// <summary>
        /// Initiates the OAuth install flow by redirecting the merchant to Shopify's authorization page.
        /// Stores a short-lived state cookie to validate the callback and prevent CSRF.
        /// </summary>
        /// <param name="shop">Merchant shop domain (example: "store-name.myshopify.com").</param>
        /// <returns>Redirect to Shopify's authorization URL or BadRequest when shop is missing.</returns>
        // 1) Install redirect -> merchant clicks this to install app on their store
        // Example: GET /auth/install?shop=store-name.myshopify.com
        [HttpGet("install")]
        public IActionResult Install([FromQuery] string shop)
        {
            if (string.IsNullOrWhiteSpace(shop)) return BadRequest("Missing shop param");

            var state = Guid.NewGuid().ToString("N");
            Response.Cookies.Append("shopify_state", state, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None
            });

            var scopes = Uri.EscapeDataString(_opt.Scopes);
            var redirect = Uri.EscapeDataString($"{_opt.AppUrl}/auth/callback");
            var url = $"https://{shop}/admin/oauth/authorize?client_id={_opt.ApiKey}&scope={scopes}&redirect_uri={redirect}&state={state}";
            return Redirect(url);
        }

        /// <summary>
        /// OAuth callback endpoint invoked by Shopify after the merchant approves the app.
        /// Validates HMAC signature and state cookie, exchanges the temporary code for an access token,
        /// persists the shop record with the offline access token, and redirects to the app/dashboard.
        /// </summary>
        /// <returns>
        /// Redirect to embedded app URL on success; BadRequest/Unauthorized/500 on validation or exchange failures.
        /// </returns>
        // 2) OAuth callback: Shopify redirects here after merchant approves
        // GET /auth/callback?shop=...&code=...&hmac=...&state=...
        [HttpGet("callback")]
        public async Task<IActionResult> Callback()
        {
            var q = Request.Query.ToDictionary(k => k.Key, v => v.Value.ToString());

            // Basic required query params
            if (!q.TryGetValue("shop", out var shop) || !q.TryGetValue("code", out var code))
                return BadRequest("Missing shop or code");

            // 2a) Validate HMAC of query (prevents tampering)
            if (!IsAuthenticShopifyHmac(q, _opt.ApiSecret))
                return Unauthorized("Invalid HMAC");

            // 2b) Validate state cookie (prevent CSRF)
            var stateCookie = Request.Cookies["shopify_state"];
            if (q.TryGetValue("state", out var state) && stateCookie != state)
                return Unauthorized("Invalid state");

            // 3) Exchange code for access token (server -> Shopify)
            var token = await ExchangeCodeForTokenAsync(shop, code);
            if (string.IsNullOrWhiteSpace(token)) return StatusCode(500, "Token exchange failed");

            // 4) Persist the shop + token to DB (simplified)
            var dbShop = await _db.Shops.FirstOrDefaultAsync(s => s.Domain == shop);
            if (dbShop == null)
            {
                dbShop = new Domain.Entities.Shop { Domain = shop, OfflineAccessToken = token, InstalledAt = DateTime.UtcNow };
                _db.Shops.Add(dbShop);
            }
            else
            {
                dbShop.OfflineAccessToken = token;
            }
            await _db.SaveChangesAsync();

            // 5) Redirect to embedded app URL (or dashboard)
            return Redirect($"/app?shop={shop}");
        }

        /// <summary>
        /// Exchanges the temporary authorization code for a shop access token by calling Shopify's access_token endpoint.
        /// </summary>
        /// <param name="shopDomain">Shop domain (myshopify domain).</param>
        /// <param name="code">Temporary OAuth code returned by Shopify.</param>
        /// <returns>The access token string on success; otherwise null.</returns>
        // --- Helper: Exchange code for token ---
        private async Task<string?> ExchangeCodeForTokenAsync(string shopDomain, string code)
        {
            var client = _httpFactory.CreateClient();
            var url = $"https://{shopDomain}/admin/oauth/access_token";
            var payload = new { client_id = _opt.ApiKey, client_secret = _opt.ApiSecret, code = code };
            var resp = await client.PostAsJsonAsync(url, payload);
            if (!resp.IsSuccessStatusCode) return null;
            using var doc = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync());
            return doc.RootElement.GetProperty("access_token").GetString();
        }

        /// <summary>
        /// Validates Shopify's HMAC on the OAuth callback querystring.
        /// The HMAC is computed as HMAC-SHA256(secret, message) where message is the sorted
        /// query string constructed from key=value pairs excluding "hmac" and "signature".
        /// </summary>
        /// <param name="query">Dictionary of query parameters.</param>
        /// <param name="secret">Shopify Api secret shared secret.</param>
        /// <returns>True when the provided hmac matches the computed value; otherwise false.</returns>
        // --- Helper: Validate HMAC of query parameters (Shopify OAuth callback) ---
        // Implementation uses hex of HMAC-SHA256 over sorted querystring key=value (excluding hmac & signature).
        private static bool IsAuthenticShopifyHmac(IDictionary<string, string> query, string secret)
        {
            // Build message
            var kv = query
                .Where(kv => kv.Key != "hmac" && kv.Key != "signature")
                .OrderBy(kv => kv.Key, StringComparer.Ordinal)
                .Select(kv => $"{kv.Key}={kv.Value}");
            var message = string.Join("&", kv);

            var secretBytes = Encoding.UTF8.GetBytes(secret);
            using var hmac = new HMACSHA256(secretBytes);
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
            var hex = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();

            var provided = query.TryGetValue("hmac", out var h) ? h : "";
            return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(hex), Encoding.UTF8.GetBytes(provided));
        }

        /// <summary>
        /// Receives shopify webhooks routed to /webhooks/{topic}.
        /// Verifies the HMAC header using the app secret and reads the raw body payload.
        /// </summary>
        /// <param name="topic">Webhook topic (for example: "app/uninstalled", "orders/create").</param>
        /// <returns>200 OK when webhook is accepted; 401 Unauthorized if verification fails.</returns>
        [HttpPost("/webhooks/{topic}")]
        public async Task<IActionResult> Webhook([FromRoute] string topic)
        {
            var shop = Request.Headers["X-Shopify-Shop-Domain"].ToString();
            var hmac = Request.Headers["X-Shopify-Hmac-Sha256"].ToString();

            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();

            if (!IsAuthenticWebhook(hmac, body, _opt.ApiSecret!)) return Unauthorized();

            // process webhook (persist log etc)
            return Ok();
        }

        /// <summary>
        /// Validates the HMAC header included with Shopify webhooks.
        /// Computation: Base64(HMAC-SHA256(secret, body)).
        /// </summary>
        /// <param name="hmacHeader">Value of the X-Shopify-Hmac-Sha256 header from the request.</param>
        /// <param name="body">Raw request body received from Shopify (JSON payload).</param>
        /// <param name="secret">Shopify Api secret shared secret.</param>
        /// <returns>True when the computed HMAC (base64) matches the provided header; otherwise false.</returns>
        private static bool IsAuthenticWebhook(string hmacHeader, string body, string secret)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(body));
            var computed = Convert.ToBase64String(hash);
            return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(computed), Encoding.UTF8.GetBytes(hmacHeader ?? ""));
        }
    }
}
