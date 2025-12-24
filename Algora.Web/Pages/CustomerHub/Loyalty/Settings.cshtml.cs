using Algora.Application.DTOs.CustomerHub;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.CustomerHub.Loyalty;

[Authorize]
public class SettingsModel : PageModel
{
    private readonly ILoyaltyService _loyaltyService;
    private readonly IShopContext _shopContext;
    private readonly ILogger<SettingsModel> _logger;

    public SettingsModel(
        ILoyaltyService loyaltyService,
        IShopContext shopContext,
        ILogger<SettingsModel> logger)
    {
        _loyaltyService = loyaltyService;
        _shopContext = shopContext;
        _logger = logger;
    }

    public LoyaltyProgramDto? Program { get; set; }
    public List<LoyaltyTierDto> Tiers { get; set; } = new();
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    [BindProperty]
    public ProgramFormModel ProgramForm { get; set; } = new();

    [BindProperty]
    public TierFormModel TierForm { get; set; } = new();

    public class ProgramFormModel
    {
        public string Name { get; set; } = "Rewards Program";
        public int PointsPerDollar { get; set; } = 1;
        public int PointsValueCents { get; set; } = 1;
        public int MinimumRedemption { get; set; } = 100;
        public int SignupBonus { get; set; } = 0;
        public int BirthdayBonus { get; set; } = 0;
        public int ReviewBonus { get; set; } = 0;
        public int ReferralBonus { get; set; } = 0;
        public int? PointsExpireMonths { get; set; }
        public string PointsName { get; set; } = "Points";
        public string Currency { get; set; } = "USD";
    }

    public class TierFormModel
    {
        public int? Id { get; set; }
        public string Name { get; set; } = "";
        public int MinimumPoints { get; set; }
        public decimal PointsMultiplier { get; set; } = 1.0m;
        public decimal? PercentageDiscount { get; set; }
        public bool FreeShipping { get; set; }
        public bool ExclusiveAccess { get; set; }
        public string? Color { get; set; } = "#6b7280";
        public string? Icon { get; set; } = "fas fa-medal";
        public int DisplayOrder { get; set; }
    }

    public async Task OnGetAsync()
    {
        await LoadDataAsync();
    }

    public async Task<IActionResult> OnPostSaveProgramAsync()
    {
        try
        {
            var shopDomain = _shopContext.ShopDomain;
            var dto = new SaveLoyaltyProgramDto(
                shopDomain,
                ProgramForm.Name,
                ProgramForm.PointsPerDollar,
                ProgramForm.PointsValueCents,
                ProgramForm.MinimumRedemption,
                ProgramForm.SignupBonus,
                ProgramForm.BirthdayBonus,
                ProgramForm.ReviewBonus,
                ProgramForm.ReferralBonus,
                ProgramForm.PointsExpireMonths,
                ProgramForm.PointsName,
                ProgramForm.Currency
            );

            await _loyaltyService.CreateOrUpdateProgramAsync(dto);
            SuccessMessage = "Loyalty program settings saved successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving loyalty program");
            ErrorMessage = "Failed to save program settings.";
        }

        await LoadDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostActivateProgramAsync()
    {
        try
        {
            var shopDomain = _shopContext.ShopDomain;
            await _loyaltyService.ActivateProgramAsync(shopDomain);
            SuccessMessage = "Loyalty program activated successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating loyalty program");
            ErrorMessage = "Failed to activate program.";
        }

        await LoadDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostDeactivateProgramAsync()
    {
        try
        {
            var shopDomain = _shopContext.ShopDomain;
            await _loyaltyService.DeactivateProgramAsync(shopDomain);
            SuccessMessage = "Loyalty program deactivated.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating loyalty program");
            ErrorMessage = "Failed to deactivate program.";
        }

        await LoadDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostSaveTierAsync()
    {
        try
        {
            if (Program == null)
            {
                ErrorMessage = "Please create a loyalty program first.";
                await LoadDataAsync();
                return Page();
            }

            if (TierForm.Id.HasValue)
            {
                // Update existing tier
                var updateDto = new UpdateLoyaltyTierDto(
                    TierForm.Name,
                    TierForm.MinimumPoints,
                    TierForm.PointsMultiplier,
                    TierForm.PercentageDiscount,
                    TierForm.FreeShipping,
                    TierForm.ExclusiveAccess,
                    TierForm.Color,
                    TierForm.Icon,
                    TierForm.DisplayOrder
                );
                await _loyaltyService.UpdateTierAsync(TierForm.Id.Value, updateDto);
                SuccessMessage = "Tier updated successfully.";
            }
            else
            {
                // Create new tier
                var createDto = new CreateLoyaltyTierDto(
                    Program.Id,
                    TierForm.Name,
                    TierForm.MinimumPoints,
                    TierForm.PointsMultiplier,
                    TierForm.PercentageDiscount,
                    TierForm.FreeShipping,
                    TierForm.ExclusiveAccess,
                    TierForm.Color,
                    TierForm.Icon,
                    TierForm.DisplayOrder
                );
                await _loyaltyService.CreateTierAsync(createDto);
                SuccessMessage = "Tier created successfully.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving tier");
            ErrorMessage = "Failed to save tier.";
        }

        await LoadDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteTierAsync(int id)
    {
        try
        {
            await _loyaltyService.DeleteTierAsync(id);
            SuccessMessage = "Tier deleted successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting tier");
            ErrorMessage = "Failed to delete tier.";
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
                Tiers = (await _loyaltyService.GetTiersAsync(Program.Id)).ToList();

                // Populate form with existing values
                ProgramForm = new ProgramFormModel
                {
                    Name = Program.Name,
                    PointsPerDollar = Program.PointsPerDollar,
                    PointsValueCents = Program.PointsValueCents,
                    MinimumRedemption = Program.MinimumRedemption,
                    SignupBonus = Program.SignupBonus,
                    BirthdayBonus = Program.BirthdayBonus,
                    ReviewBonus = Program.ReviewBonus,
                    ReferralBonus = Program.ReferralBonus,
                    PointsExpireMonths = Program.PointsExpireMonths,
                    PointsName = Program.PointsName,
                    Currency = Program.Currency
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading loyalty settings");
            ErrorMessage = "Failed to load settings.";
        }
    }
}
