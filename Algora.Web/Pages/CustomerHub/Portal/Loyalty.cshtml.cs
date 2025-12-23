using Algora.Application.DTOs.CustomerHub;
using Algora.Application.Interfaces;
using Algora.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CustomerEntity = Algora.Domain.Entities.Customer;

namespace Algora.Web.Pages.CustomerHub.Portal;

public class LoyaltyModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly ILoyaltyService _loyaltyService;
    private readonly IShopContext _shopContext;
    private readonly ILogger<LoyaltyModel> _logger;

    public LoyaltyModel(
        AppDbContext context,
        ILoyaltyService loyaltyService,
        IShopContext shopContext,
        ILogger<LoyaltyModel> logger)
    {
        _context = context;
        _loyaltyService = loyaltyService;
        _shopContext = shopContext;
        _logger = logger;
    }

    public CustomerEntity? Customer { get; set; }
    public LoyaltyProgramDto? Program { get; set; }
    public CustomerLoyaltyDto? Membership { get; set; }
    public List<LoyaltyTierDto> Tiers { get; set; } = new();
    public List<LoyaltyRewardDto> AvailableRewards { get; set; } = new();
    public List<LoyaltyPointsDto> PointsHistory { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Email { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (string.IsNullOrEmpty(Email))
        {
            return RedirectToPage("./Index");
        }

        await LoadDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostEnrollAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(Email))
            {
                return RedirectToPage("./Index");
            }

            var shopDomain = _shopContext.ShopDomain;
            Customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.ShopDomain == shopDomain && c.Email == Email);

            if (Customer == null)
            {
                ErrorMessage = "Customer not found.";
                await LoadDataAsync();
                return Page();
            }

            var enrollDto = new EnrollMemberDto(shopDomain, Customer.Id);
            await _loyaltyService.EnrollMemberAsync(enrollDto);
            SuccessMessage = "You've been enrolled in our rewards program!";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enrolling customer in loyalty program");
            ErrorMessage = "Unable to enroll. Please try again.";
        }

        await LoadDataAsync();
        return Page();
    }

    private async Task LoadDataAsync()
    {
        try
        {
            var shopDomain = _shopContext.ShopDomain;

            Customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.ShopDomain == shopDomain && c.Email == Email);

            if (Customer == null)
            {
                ErrorMessage = "Customer not found.";
                return;
            }

            Program = await _loyaltyService.GetProgramAsync(shopDomain);
            if (Program == null || !Program.IsActive)
            {
                return;
            }

            Membership = await _loyaltyService.GetMemberAsync(Customer.Id);
            Tiers = (await _loyaltyService.GetTiersAsync(Program.Id)).OrderBy(t => t.MinimumPoints).ToList();
            AvailableRewards = (await _loyaltyService.GetActiveRewardsAsync(shopDomain))
                .Where(r => Membership == null || r.PointsCost <= Membership.PointsBalance)
                .OrderBy(r => r.PointsCost)
                .ToList();

            if (Membership != null)
            {
                PointsHistory = (await _loyaltyService.GetPointsHistoryAsync(Customer.Id, 10)).ToList();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading loyalty data for {Email}", Email);
            ErrorMessage = "Unable to load rewards information.";
        }
    }

    public string GetTierColor(int? tierId)
    {
        if (tierId == null) return "#6b7280";
        return Tiers.FirstOrDefault(t => t.Id == tierId)?.Color ?? "#6b7280";
    }

    public int GetTierProgress()
    {
        if (Membership == null || Tiers.Count == 0) return 0;

        var currentTier = Tiers.FirstOrDefault(t => t.Id == Membership.CurrentTierId);
        var nextTier = Tiers.FirstOrDefault(t => t.MinimumPoints > Membership.LifetimePoints);

        if (nextTier == null) return 100; // Already at max tier

        var currentMin = currentTier?.MinimumPoints ?? 0;
        var nextMin = nextTier.MinimumPoints;
        var progress = ((double)(Membership.LifetimePoints - currentMin) / (nextMin - currentMin)) * 100;

        return Math.Min(100, Math.Max(0, (int)progress));
    }

    public string GetPointsTypeIcon(string type) => type.ToLower() switch
    {
        "earn" => "fas fa-plus-circle text-green-500",
        "bonus" => "fas fa-gift text-purple-500",
        "redeem" => "fas fa-minus-circle text-red-500",
        "expire" => "fas fa-clock text-orange-500",
        "adjust" => "fas fa-edit text-blue-500",
        _ => "fas fa-circle text-gray-500"
    };
}
