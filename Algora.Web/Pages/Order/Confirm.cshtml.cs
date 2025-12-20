using Algora.Application.DTOs.Upsell;
using Algora.Application.Interfaces;
using Algora.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Algora.Web.Pages.Order;

/// <summary>
/// Post-purchase confirmation page with upsell offers.
/// This page is PUBLIC (no authorization) - accessed via post-purchase redirect.
/// </summary>
public class ConfirmModel : PageModel
{
    private readonly IUpsellRecommendationService _upsellService;
    private readonly IUpsellExperimentService _experimentService;
    private readonly AppDbContext _db;
    private readonly ILogger<ConfirmModel> _logger;

    public ConfirmModel(
        IUpsellRecommendationService upsellService,
        IUpsellExperimentService experimentService,
        AppDbContext db,
        ILogger<ConfirmModel> logger)
    {
        _upsellService = upsellService;
        _experimentService = experimentService;
        _db = db;
        _logger = logger;
    }

    public OrderSummaryDto? Order { get; set; }
    public List<UpsellOfferDto> Offers { get; set; } = new();
    public UpsellPageSettingsDto? Settings { get; set; }
    public string SessionId { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(long id, string? shop = null, string? session = null)
    {
        // Generate or use provided session ID for experiment consistency
        SessionId = session ?? Guid.NewGuid().ToString("N");

        if (string.IsNullOrEmpty(shop))
        {
            ErrorMessage = "Shop domain is required.";
            return Page();
        }

        try
        {
            // Load order from database
            var order = await _db.Orders
                .Include(o => o.Lines)
                .FirstOrDefaultAsync(o => o.ShopDomain == shop && o.PlatformOrderId == id);

            if (order == null)
            {
                _logger.LogWarning("Order {OrderId} not found for shop {Shop}", id, shop);
                ErrorMessage = "Order not found.";
                return Page();
            }

            // Map to order summary DTO
            Order = new OrderSummaryDto
            {
                OrderId = order.PlatformOrderId,
                OrderNumber = order.OrderNumber,
                CustomerName = !string.IsNullOrEmpty(order.CustomerEmail)
                    ? order.CustomerEmail.Split('@')[0]
                    : "Valued Customer",
                CustomerEmail = order.CustomerEmail,
                TotalPrice = order.GrandTotal,
                Currency = order.Currency,
                OrderDate = order.OrderDate,
                Items = order.Lines.Select(l => new OrderItemSummaryDto
                {
                    ProductTitle = l.ProductTitle,
                    VariantTitle = l.VariantTitle,
                    Quantity = l.Quantity,
                    Price = l.UnitPrice
                }).ToList()
            };

            // Get upsell offers
            Offers = await _upsellService.GetOffersForOrderAsync(shop, id, SessionId);

            // Get settings
            var settings = await _upsellService.GetSettingsAsync(shop);
            Settings = new UpsellPageSettingsDto
            {
                PageTitle = settings.PageTitle ?? "Order Confirmed",
                ThankYouMessage = settings.ThankYouMessage ?? "Thank you for your order!",
                UpsellSectionTitle = settings.UpsellSectionTitle ?? "You might also like",
                DisplayLayout = settings.DisplayLayout,
                LogoUrl = settings.LogoUrl,
                PrimaryColor = settings.PrimaryColor,
                SecondaryColor = settings.SecondaryColor
            };

            // Record impressions for each offer
            foreach (var offer in Offers)
            {
                try
                {
                    await _experimentService.RecordImpressionAsync(
                        shop,
                        offer.Id,
                        id,
                        SessionId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error recording impression for offer {OfferId}", offer.Id);
                }
            }

            _logger.LogInformation("Loaded confirmation page for order {OrderId} with {OfferCount} upsell offers",
                id, Offers.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading confirmation page for order {OrderId}", id);
            ErrorMessage = "Unable to load order confirmation.";
        }

        return Page();
    }

    /// <summary>
    /// Handle click tracking via AJAX.
    /// </summary>
    public async Task<IActionResult> OnPostTrackClickAsync([FromBody] TrackClickRequest request)
    {
        try
        {
            await _experimentService.RecordClickAsync(request.ConversionId);
            return new JsonResult(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tracking click for conversion {ConversionId}", request.ConversionId);
            return new JsonResult(new { success = false });
        }
    }
}

public record TrackClickRequest
{
    public int ConversionId { get; init; }
}
