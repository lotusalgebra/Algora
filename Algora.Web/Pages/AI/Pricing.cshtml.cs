using Algora.Application.Interfaces;
using Algora.Application.Interfaces.AI;
using Algora.Domain.Entities;
using Algora.Infrastructure.Data;
using Algora.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Algora.Web.Pages.AI;

[Authorize]
[RequireFeature(FeatureCodes.AiPricing)]
[IgnoreAntiforgeryToken]
public class PricingModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IPricingOptimizerService _pricingService;
    private readonly IShopContext _shopContext;

    public PricingModel(
        AppDbContext db,
        IPricingOptimizerService pricingService,
        IShopContext shopContext)
    {
        _db = db;
        _pricingService = pricingService;
        _shopContext = shopContext;
    }

    public List<Product> Products { get; set; } = new();

    public async Task OnGetAsync()
    {
        Products = await _db.Products
            .Where(p => p.ShopDomain == _shopContext.ShopDomain)
            .OrderBy(p => p.Title)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostAnalyzeAsync([FromBody] AnalyzeRequest request)
    {
        try
        {
            var result = await _pricingService.GetSuggestionAsync(request.ProductId);

            if (result.Success)
            {
                return new JsonResult(new
                {
                    success = true,
                    data = new
                    {
                        suggestionId = result.SuggestionId,
                        currentPrice = result.CurrentPrice,
                        suggestedPrice = result.SuggestedPrice,
                        minPrice = result.MinPrice,
                        maxPrice = result.MaxPrice,
                        priceChange = result.PriceChange,
                        changePercent = result.ChangePercent,
                        currentMargin = result.CurrentMargin,
                        suggestedMargin = result.SuggestedMargin,
                        reasoning = result.Reasoning,
                        confidence = result.Confidence,
                        provider = result.Provider
                    }
                });
            }

            return new JsonResult(new { success = false, error = result.Error });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, error = ex.Message });
        }
    }

    public async Task<IActionResult> OnPostApplyAsync([FromBody] ApplyRequest request)
    {
        try
        {
            await _pricingService.ApplySuggestionAsync(request.SuggestionId);
            return new JsonResult(new { success = true });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, error = ex.Message });
        }
    }

    public async Task<IActionResult> OnGetHistoryAsync(int productId)
    {
        var history = await _pricingService.GetHistoryAsync(productId);
        return new JsonResult(new { success = true, data = history });
    }

    public class AnalyzeRequest
    {
        public int ProductId { get; set; }
    }

    public class ApplyRequest
    {
        public int SuggestionId { get; set; }
    }
}
