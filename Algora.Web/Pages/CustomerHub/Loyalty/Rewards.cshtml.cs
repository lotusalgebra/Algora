using Algora.Application.DTOs.CustomerHub;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.CustomerHub.Loyalty;

[Authorize]
public class RewardsModel : PageModel
{
    private readonly ILoyaltyService _loyaltyService;
    private readonly IShopContext _shopContext;
    private readonly ILogger<RewardsModel> _logger;

    public RewardsModel(
        ILoyaltyService loyaltyService,
        IShopContext shopContext,
        ILogger<RewardsModel> logger)
    {
        _loyaltyService = loyaltyService;
        _shopContext = shopContext;
        _logger = logger;
    }

    public LoyaltyProgramDto? Program { get; set; }
    public List<LoyaltyRewardDto> Rewards { get; set; } = new();
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    [BindProperty]
    public RewardFormModel RewardForm { get; set; } = new();

    public class RewardFormModel
    {
        public int? Id { get; set; }
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public string Type { get; set; } = "discount_fixed";
        public int PointsCost { get; set; } = 100;
        public decimal Value { get; set; } = 5.00m;
        public decimal? MinimumOrderAmount { get; set; }
        public int? MaxRedemptions { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime? StartsAt { get; set; }
        public DateTime? EndsAt { get; set; }
        public string? ImageUrl { get; set; }
    }

    public async Task OnGetAsync()
    {
        await LoadDataAsync();
    }

    public async Task<IActionResult> OnPostSaveRewardAsync()
    {
        try
        {
            if (Program == null)
            {
                await LoadDataAsync();
                if (Program == null)
                {
                    ErrorMessage = "Please create a loyalty program first.";
                    return Page();
                }
            }

            if (RewardForm.Id.HasValue)
            {
                // Update existing reward
                var updateDto = new UpdateLoyaltyRewardDto(
                    RewardForm.Name,
                    RewardForm.Description,
                    RewardForm.Type,
                    RewardForm.PointsCost,
                    RewardForm.Value,
                    RewardForm.MinimumOrderAmount,
                    null, // ProductId
                    RewardForm.MaxRedemptions,
                    RewardForm.IsActive,
                    RewardForm.StartsAt,
                    RewardForm.EndsAt,
                    RewardForm.ImageUrl
                );
                await _loyaltyService.UpdateRewardAsync(RewardForm.Id.Value, updateDto);
                SuccessMessage = "Reward updated successfully.";
            }
            else
            {
                // Create new reward
                var createDto = new CreateLoyaltyRewardDto(
                    Program!.Id,
                    RewardForm.Name,
                    RewardForm.Description,
                    RewardForm.Type,
                    RewardForm.PointsCost,
                    RewardForm.Value,
                    RewardForm.MinimumOrderAmount,
                    null, // ProductId
                    RewardForm.MaxRedemptions,
                    RewardForm.StartsAt,
                    RewardForm.EndsAt,
                    RewardForm.ImageUrl
                );
                await _loyaltyService.CreateRewardAsync(createDto);
                SuccessMessage = "Reward created successfully.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving reward");
            ErrorMessage = "Failed to save reward.";
        }

        await LoadDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostToggleRewardAsync(int id)
    {
        try
        {
            await LoadDataAsync();
            var reward = Rewards.FirstOrDefault(r => r.Id == id);
            if (reward != null)
            {
                var updateDto = new UpdateLoyaltyRewardDto(IsActive: !reward.IsActive);
                await _loyaltyService.UpdateRewardAsync(id, updateDto);
                SuccessMessage = reward.IsActive ? "Reward deactivated." : "Reward activated.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling reward");
            ErrorMessage = "Failed to update reward.";
        }

        await LoadDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteRewardAsync(int id)
    {
        try
        {
            await _loyaltyService.DeleteRewardAsync(id);
            SuccessMessage = "Reward deleted successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting reward");
            ErrorMessage = "Failed to delete reward.";
        }

        await LoadDataAsync();
        return Page();
    }

    private async Task LoadDataAsync()
    {
        try
        {
            var shopDomain = _shopContext.ShopDomain;
            Program = await _loyaltyService.GetProgramAsync(shopDomain);

            if (Program != null)
            {
                Rewards = (await _loyaltyService.GetRewardsAsync(Program.Id)).ToList();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading rewards");
            ErrorMessage = "Failed to load rewards.";
        }
    }

    public string GetRewardTypeLabel(string type) => type switch
    {
        "discount_percent" => "Percentage Discount",
        "discount_fixed" => "Fixed Discount",
        "free_shipping" => "Free Shipping",
        "free_product" => "Free Product",
        _ => type
    };

    public string GetRewardTypeIcon(string type) => type switch
    {
        "discount_percent" => "fas fa-percent",
        "discount_fixed" => "fas fa-dollar-sign",
        "free_shipping" => "fas fa-truck",
        "free_product" => "fas fa-gift",
        _ => "fas fa-tag"
    };

    public string GetRewardTypeColor(string type) => type switch
    {
        "discount_percent" => "from-blue-600 to-cyan-400",
        "discount_fixed" => "from-green-600 to-lime-400",
        "free_shipping" => "from-purple-600 to-pink-400",
        "free_product" => "from-orange-500 to-yellow-300",
        _ => "from-gray-600 to-gray-400"
    };
}
