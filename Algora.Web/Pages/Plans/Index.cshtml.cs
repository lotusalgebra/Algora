using Algora.Application.DTOs.Plan;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.Plans
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly IPlanService _planService;
        private readonly IShopContext _shopContext;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(
            IPlanService planService,
            IShopContext shopContext,
            ILogger<IndexModel> logger)
        {
            _planService = planService;
            _shopContext = shopContext;
            _logger = logger;
        }

        public IEnumerable<PlanDto> Plans { get; set; } = [];
        public PlanDto? CurrentPlan { get; set; }
        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        public async Task OnGetAsync()
        {
            try
            {
                var shopDomain = _shopContext.ShopDomain;
                Plans = await _planService.GetAllPlansAsync();
                CurrentPlan = await _planService.GetCurrentPlanAsync(shopDomain);

                // Mark current plan in the list
                if (CurrentPlan != null)
                {
                    Plans = Plans.Select(p => p with { IsCurrentPlan = p.Name == CurrentPlan.Name });
                }

                // Check for query string messages
                if (Request.Query.ContainsKey("upgraded"))
                {
                    SuccessMessage = "Your plan has been upgraded successfully!";
                }
                else if (Request.Query.ContainsKey("pending"))
                {
                    SuccessMessage = "Your downgrade request has been submitted and is pending admin approval.";
                }
                else if (Request.Query.ContainsKey("error"))
                {
                    ErrorMessage = Request.Query["error"].ToString();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load plans");
                ErrorMessage = "Failed to load plans. Please try again later.";
            }
        }
    }
}
