using Algora.Application.DTOs.Reviews;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.Reviews.Admin;

[Authorize]
public class SettingsModel : PageModel
{
    private readonly IReviewService _reviewService;
    private readonly IShopContext _shopContext;
    private readonly ILogger<SettingsModel> _logger;

    public SettingsModel(
        IReviewService reviewService,
        IShopContext shopContext,
        ILogger<SettingsModel> logger)
    {
        _reviewService = reviewService;
        _shopContext = shopContext;
        _logger = logger;
    }

    public ReviewSettingsDto Settings { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            Settings = await _reviewService.GetSettingsAsync(_shopContext.ShopDomain);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading review settings");
            ErrorMessage = "Failed to load settings. Please try again.";
        }
    }

    public async Task<IActionResult> OnPostAsync([FromForm] UpdateReviewSettingsDto dto)
    {
        try
        {
            Settings = await _reviewService.UpdateSettingsAsync(_shopContext.ShopDomain, dto);
            SuccessMessage = "Settings saved successfully!";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating review settings");
            ErrorMessage = "Failed to save settings. Please try again.";
        }

        return Page();
    }
}
