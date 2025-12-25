using Algora.Chatbot.Application.Interfaces.Shopify;
using Algora.Chatbot.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Algora.Chatbot.Web.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly IShopifyOAuthService _oauthService;
    private readonly ChatbotDbContext _db;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IShopifyOAuthService oauthService,
        ChatbotDbContext db,
        ILogger<AuthController> logger)
    {
        _oauthService = oauthService;
        _db = db;
        _logger = logger;
    }

    [HttpGet("install")]
    public IActionResult Install([FromQuery] string shop)
    {
        if (string.IsNullOrEmpty(shop))
        {
            return BadRequest("Shop parameter is required");
        }

        var redirectUri = $"{Request.Scheme}://{Request.Host}/auth/callback";
        var state = Guid.NewGuid().ToString("N");
        var authUrl = _oauthService.GetAuthorizationUrl(shop, redirectUri, state);
        return Redirect(authUrl);
    }

    [HttpGet("callback")]
    public async Task<IActionResult> Callback(
        [FromQuery] string code,
        [FromQuery] string shop,
        [FromQuery] string hmac,
        [FromQuery] string state)
    {
        try
        {
            // Validate HMAC
            if (!_oauthService.ValidateHmac(Request.QueryString.Value ?? "", hmac))
            {
                _logger.LogWarning("Invalid HMAC for shop {Shop}", shop);
                return BadRequest("Invalid request signature");
            }

            // Exchange code for access token
            var accessToken = await _oauthService.ExchangeCodeForTokenAsync(shop, code);

            if (string.IsNullOrEmpty(accessToken))
            {
                return BadRequest("Failed to obtain access token");
            }

            // Store or update shop
            var existingShop = await _db.Shops.FirstOrDefaultAsync(s => s.Domain == shop);

            if (existingShop != null)
            {
                existingShop.OfflineAccessToken = accessToken;
                existingShop.IsActive = true;
            }
            else
            {
                var newShop = new Algora.Chatbot.Domain.Entities.Shop
                {
                    Domain = shop,
                    OfflineAccessToken = accessToken,
                    IsActive = true,
                    InstalledAt = DateTime.UtcNow
                };
                _db.Shops.Add(newShop);

                // Create default settings
                var settings = new Algora.Chatbot.Domain.Entities.ChatbotSettings
                {
                    ShopDomain = shop,
                    BotName = "Support Assistant",
                    WelcomeMessage = "Hi! How can I help you today?",
                    Tone = "professional",
                    PreferredAiProvider = "openai",
                    FallbackAiProvider = "anthropic"
                };
                _db.ChatbotSettings.Add(settings);

                // Create default widget config
                var widgetConfig = new Algora.Chatbot.Domain.Entities.WidgetConfiguration
                {
                    ShopDomain = shop,
                    Position = "bottom-right",
                    PrimaryColor = "#7c3aed",
                    HeaderTitle = "Chat with us",
                    TriggerText = "Need help?",
                    PlaceholderText = "Type your message...",
                    ShowPoweredBy = true
                };
                _db.WidgetConfigurations.Add(widgetConfig);
            }

            await _db.SaveChangesAsync();

            _logger.LogInformation("Shop {Shop} installed successfully", shop);

            // Redirect to Shopify admin app page
            return Redirect($"https://{shop}/admin/apps");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during OAuth callback for shop {Shop}", shop);
            return BadRequest("Authentication failed");
        }
    }

    [HttpGet("")]
    public IActionResult Index([FromQuery] string shop)
    {
        if (string.IsNullOrEmpty(shop))
        {
            return BadRequest("Shop parameter is required");
        }

        // For embedded apps, redirect to install flow
        return RedirectToAction(nameof(Install), new { shop });
    }
}
