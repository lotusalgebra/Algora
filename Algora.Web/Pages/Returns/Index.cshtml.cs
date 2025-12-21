using Algora.Application.DTOs.Returns;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.Returns;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IReturnService _returnService;
    private readonly IShopContext _shopContext;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        IReturnService returnService,
        IShopContext shopContext,
        ILogger<IndexModel> logger)
    {
        _returnService = returnService;
        _shopContext = shopContext;
        _logger = logger;
    }

    public ReturnSummaryDto Summary { get; set; } = new();
    public ReturnAnalyticsDto? Analytics { get; set; }
    public ReturnSettingsDto? Settings { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            Summary = await _returnService.GetReturnSummaryAsync(_shopContext.ShopDomain);
            Settings = await _returnService.GetSettingsAsync(_shopContext.ShopDomain);

            // Get analytics for last 30 days
            var startDate = DateTime.UtcNow.AddDays(-30);
            Analytics = await _returnService.GetAnalyticsAsync(_shopContext.ShopDomain, startDate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading returns dashboard");
            ErrorMessage = "Failed to load return data. Please try again.";
        }
    }
}
