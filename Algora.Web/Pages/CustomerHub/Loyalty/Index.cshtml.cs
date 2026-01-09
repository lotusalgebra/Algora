using Algora.Application.DTOs.CustomerHub;
using Algora.Application.Interfaces;
using Algora.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.CustomerHub.Loyalty;

[Authorize]
[RequireFeature(FeatureCodes.LoyaltyProgram)]
public class IndexModel : PageModel
{
    private readonly ILoyaltyService _loyaltyService;
    private readonly IShopContext _shopContext;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        ILoyaltyService loyaltyService,
        IShopContext shopContext,
        ILogger<IndexModel> logger)
    {
        _loyaltyService = loyaltyService;
        _shopContext = shopContext;
        _logger = logger;
    }

    public LoyaltyProgramDto? Program { get; set; }
    public List<LoyaltyTierDto> Tiers { get; set; } = new();
    public List<CustomerLoyaltyDto> TopMembers { get; set; } = new();
    public LoyaltyAnalyticsDto Analytics { get; set; } = new(0, 0, 0, 0, 0, 0, 0, 0, new Dictionary<string, int>(), new Dictionary<string, int>(), new List<DailyPointsDto>());

    public async Task OnGetAsync()
    {
        try
        {
            var shopDomain = _shopContext.ShopDomain;

            Program = await _loyaltyService.GetProgramAsync(shopDomain);
            if (Program != null)
            {
                Tiers = (await _loyaltyService.GetTiersAsync(Program.Id)).ToList();
                TopMembers = (await _loyaltyService.GetTopMembersAsync(shopDomain, 10)).ToList();
                Analytics = await _loyaltyService.GetAnalyticsAsync(shopDomain, DateTime.UtcNow.AddDays(-30), DateTime.UtcNow);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading loyalty dashboard");
        }
    }
}
