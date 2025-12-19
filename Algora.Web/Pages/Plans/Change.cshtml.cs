using Algora.Application.DTOs.Plan;
using Algora.Application.Interfaces;
using Algora.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Algora.Web.Pages.Plans
{
    [Authorize]
    public class ChangeModel : PageModel
    {
        private readonly IPlanService _planService;
        private readonly IShopContext _shopContext;
        private readonly AppDbContext _db;
        private readonly ILogger<ChangeModel> _logger;

        public ChangeModel(
            IPlanService planService,
            IShopContext shopContext,
            AppDbContext db,
            ILogger<ChangeModel> logger)
        {
            _planService = planService;
            _shopContext = shopContext;
            _db = db;
            _logger = logger;
        }

        [BindProperty(SupportsGet = true)]
        public string? Plan { get; set; }

        public PlanDto? CurrentPlan { get; set; }
        public PlanDto? NewPlan { get; set; }
        public bool IsUpgrade { get; set; }
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (string.IsNullOrWhiteSpace(Plan))
            {
                return RedirectToPage("/Plans/Index");
            }

            try
            {
                var shopDomain = _shopContext.ShopDomain;
                CurrentPlan = await _planService.GetCurrentPlanAsync(shopDomain);
                NewPlan = await _planService.GetPlanByNameAsync(Plan);

                if (NewPlan == null)
                {
                    return RedirectToPage("/Plans/Index", new { error = "Invalid plan selected" });
                }

                if (CurrentPlan != null && CurrentPlan.Name == NewPlan.Name)
                {
                    return RedirectToPage("/Plans/Index", new { error = "You are already on this plan" });
                }

                IsUpgrade = CurrentPlan == null || NewPlan.MonthlyPrice > CurrentPlan.MonthlyPrice;

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load plan change page");
                return RedirectToPage("/Plans/Index", new { error = "An error occurred. Please try again." });
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(Plan))
            {
                return RedirectToPage("/Plans/Index");
            }

            try
            {
                var shopDomain = _shopContext.ShopDomain;

                // Get shop's access token
                var shop = await _db.Shops.FirstOrDefaultAsync(s => s.Domain == shopDomain);
                if (shop == null || string.IsNullOrEmpty(shop.OfflineAccessToken))
                {
                    ErrorMessage = "Shop not found or not properly configured.";
                    await LoadPageDataAsync(shopDomain);
                    return Page();
                }

                var result = await _planService.RequestPlanChangeAsync(shopDomain, shop.OfflineAccessToken, Plan);

                if (result == null)
                {
                    ErrorMessage = "Failed to process plan change. Please try again.";
                    await LoadPageDataAsync(shopDomain);
                    return Page();
                }

                switch (result)
                {
                    case "pending":
                        return RedirectToPage("/Plans/Index", new { pending = true });

                    case "pending_request_exists":
                        return RedirectToPage("/Plans/Index", new { error = "You already have a pending plan change request." });

                    case "already_on_plan":
                        return RedirectToPage("/Plans/Index", new { error = "You are already on this plan." });

                    case "upgraded":
                        return RedirectToPage("/Plans/Index", new { upgraded = true });

                    default:
                        // It's a Shopify billing URL - redirect to it
                        if (result.StartsWith("http"))
                        {
                            return Redirect(result);
                        }

                        ErrorMessage = "Unexpected response. Please try again.";
                        await LoadPageDataAsync(shopDomain);
                        return Page();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process plan change");
                ErrorMessage = "An error occurred. Please try again.";

                var shopDomain = _shopContext.ShopDomain;
                await LoadPageDataAsync(shopDomain);
                return Page();
            }
        }

        private async Task LoadPageDataAsync(string shopDomain)
        {
            CurrentPlan = await _planService.GetCurrentPlanAsync(shopDomain);
            NewPlan = await _planService.GetPlanByNameAsync(Plan ?? "");
            IsUpgrade = CurrentPlan == null || (NewPlan != null && NewPlan.MonthlyPrice > CurrentPlan.MonthlyPrice);
        }
    }
}
