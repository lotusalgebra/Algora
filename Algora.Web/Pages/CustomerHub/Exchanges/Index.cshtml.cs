using Algora.Application.DTOs.CustomerHub;
using Algora.Application.Interfaces;
using Algora.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.CustomerHub.Exchanges;

[Authorize]
[RequireFeature(FeatureCodes.Exchanges)]
public class IndexModel : PageModel
{
    private readonly IExchangeService _exchangeService;
    private readonly IShopContext _shopContext;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        IExchangeService exchangeService,
        IShopContext shopContext,
        ILogger<IndexModel> logger)
    {
        _exchangeService = exchangeService;
        _shopContext = shopContext;
        _logger = logger;
    }

    public List<ExchangeDto> Exchanges { get; set; } = new();
    public int PendingCount { get; set; }
    public int ApprovedCount { get; set; }
    public int CompletedCount { get; set; }
    public int CancelledCount { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? FilterStatus { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            var shopDomain = _shopContext.ShopDomain;

            var filter = new ExchangeFilterDto { Status = FilterStatus };
            Exchanges = (await _exchangeService.GetExchangesAsync(shopDomain, filter)).ToList();

            // Get counts for each status
            var allExchanges = await _exchangeService.GetExchangesAsync(shopDomain, new ExchangeFilterDto { Take = 1000 });
            var exchangeList = allExchanges.ToList();
            PendingCount = exchangeList.Count(e => e.Status == "pending");
            ApprovedCount = exchangeList.Count(e => e.Status == "approved" || e.Status == "shipped" || e.Status == "received");
            CompletedCount = exchangeList.Count(e => e.Status == "completed");
            CancelledCount = exchangeList.Count(e => e.Status == "cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading exchanges");
        }
    }
}
