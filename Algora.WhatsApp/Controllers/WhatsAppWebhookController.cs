using Algora.WhatsApp.DTOs;
using Algora.WhatsApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Algora.WhatsApp.Controllers;

/// <summary>
/// Controller for handling Facebook WhatsApp Business API webhooks.
/// </summary>
[ApiController]
[Route("api/whatsapp/webhook")]
public class WhatsAppWebhookController : ControllerBase
{
    private readonly IWhatsAppService _whatsAppService;
    private readonly ILogger<WhatsAppWebhookController> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public WhatsAppWebhookController(
        IWhatsAppService whatsAppService,
        ILogger<WhatsAppWebhookController> logger)
    {
        _whatsAppService = whatsAppService;
        _logger = logger;
    }

    /// <summary>
    /// Webhook verification endpoint for Meta.
    /// Called when setting up the webhook in Meta App Dashboard.
    /// </summary>
    [HttpGet]
    public IActionResult VerifyWebhook(
        [FromQuery(Name = "hub.mode")] string mode,
        [FromQuery(Name = "hub.verify_token")] string verifyToken,
        [FromQuery(Name = "hub.challenge")] string challenge)
    {
        _logger.LogInformation("Received webhook verification request: mode={Mode}", mode);

        if (_whatsAppService.VerifyWebhook(mode, verifyToken, challenge, out var response))
        {
            return Ok(response);
        }

        return Unauthorized(response);
    }

    /// <summary>
    /// Webhook endpoint for incoming messages and status updates from Meta.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> ReceiveWebhook(
        [FromHeader(Name = "X-Hub-Signature-256")] string? signature)
    {
        try
        {
            // Read raw body for signature verification
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();

            // Verify signature if provided
            if (!string.IsNullOrEmpty(signature))
            {
                if (!_whatsAppService.VerifySignature(body, signature))
                {
                    _logger.LogWarning("Invalid webhook signature");
                    return Unauthorized("Invalid signature");
                }
            }

            // Parse payload
            var payload = JsonSerializer.Deserialize<WhatsAppWebhookPayload>(body, JsonOptions);
            if (payload is null)
            {
                _logger.LogWarning("Failed to parse webhook payload");
                return BadRequest("Invalid payload");
            }

            // Get shop domain from header or extract from business account
            var shopDomain = Request.Headers["X-Shop-Domain"].FirstOrDefault()
                ?? ExtractShopDomainFromPayload(payload);

            if (string.IsNullOrEmpty(shopDomain))
            {
                _logger.LogWarning("Could not determine shop domain from webhook");
                // Still return OK to prevent Meta from retrying
                return Ok();
            }

            // Process webhook asynchronously
            _ = Task.Run(async () =>
            {
                try
                {
                    await _whatsAppService.ProcessWebhookAsync(shopDomain, payload);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing webhook for shop {ShopDomain}", shopDomain);
                }
            });

            // Always return 200 OK to Meta quickly to prevent retries
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error receiving webhook");
            // Return OK to prevent Meta from retrying
            return Ok();
        }
    }

    /// <summary>
    /// Webhook endpoint for a specific shop.
    /// Use this when you have multiple shops with different webhook URLs.
    /// </summary>
    [HttpPost("{shopDomain}")]
    public async Task<IActionResult> ReceiveWebhookForShop(
        string shopDomain,
        [FromHeader(Name = "X-Hub-Signature-256")] string? signature)
    {
        try
        {
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();

            if (!string.IsNullOrEmpty(signature) && !_whatsAppService.VerifySignature(body, signature))
            {
                _logger.LogWarning("Invalid webhook signature for shop {ShopDomain}", shopDomain);
                return Unauthorized("Invalid signature");
            }

            var payload = JsonSerializer.Deserialize<WhatsAppWebhookPayload>(body, JsonOptions);
            if (payload is null)
            {
                return BadRequest("Invalid payload");
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    await _whatsAppService.ProcessWebhookAsync(shopDomain, payload);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing webhook for shop {ShopDomain}", shopDomain);
                }
            });

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error receiving webhook for shop {ShopDomain}", shopDomain);
            return Ok();
        }
    }

    private static string? ExtractShopDomainFromPayload(WhatsAppWebhookPayload payload)
    {
        // Try to extract from the business account ID or phone number ID
        // This would require a lookup table in production
        var entry = payload.Entry.FirstOrDefault();
        if (entry is not null)
        {
            // In production, look up shop domain by business account ID
            // For now, return null and rely on X-Shop-Domain header
            return null;
        }
        return null;
    }
}
