using Algora.Application.DTOs.Returns;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.Returns;

[Authorize]
public class ReasonsModel : PageModel
{
    private readonly IReturnService _returnService;
    private readonly IShopContext _shopContext;
    private readonly ILogger<ReasonsModel> _logger;

    public ReasonsModel(
        IReturnService returnService,
        IShopContext shopContext,
        ILogger<ReasonsModel> logger)
    {
        _returnService = returnService;
        _shopContext = shopContext;
        _logger = logger;
    }

    public List<ReturnReasonDto> Reasons { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    [BindProperty]
    public CreateReturnReasonDto NewReason { get; set; } = new();

    public async Task OnGetAsync()
    {
        try
        {
            Reasons = await _returnService.GetAllReasonsAsync(_shopContext.ShopDomain);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading return reasons");
            ErrorMessage = "Failed to load return reasons.";
        }
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (string.IsNullOrWhiteSpace(NewReason.Code) || string.IsNullOrWhiteSpace(NewReason.DisplayText))
        {
            ErrorMessage = "Code and display text are required.";
            await OnGetAsync();
            return Page();
        }

        try
        {
            await _returnService.CreateReasonAsync(_shopContext.ShopDomain, NewReason);
            SuccessMessage = "Return reason created successfully.";
            NewReason = new CreateReturnReasonDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating return reason");
            ErrorMessage = "Failed to create return reason: " + ex.Message;
        }

        await OnGetAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostUpdateAsync(int id, string code, string displayText, string? description,
        int displayOrder, bool isActive, bool requiresNote, bool isDefect, bool eligibleForAutoApproval)
    {
        try
        {
            var dto = new CreateReturnReasonDto
            {
                Code = code,
                DisplayText = displayText,
                Description = description,
                DisplayOrder = displayOrder,
                IsActive = isActive,
                RequiresNote = requiresNote,
                IsDefect = isDefect,
                EligibleForAutoApproval = eligibleForAutoApproval
            };

            await _returnService.UpdateReasonAsync(id, dto);
            SuccessMessage = "Return reason updated successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating return reason {ReasonId}", id);
            ErrorMessage = "Failed to update return reason: " + ex.Message;
        }

        await OnGetAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        try
        {
            await _returnService.DeleteReasonAsync(id);
            SuccessMessage = "Return reason deleted successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting return reason {ReasonId}", id);
            ErrorMessage = "Failed to delete return reason: " + ex.Message;
        }

        await OnGetAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostSeedDefaultsAsync()
    {
        try
        {
            await _returnService.SeedDefaultReasonsAsync(_shopContext.ShopDomain);
            SuccessMessage = "Default return reasons created successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding default return reasons");
            ErrorMessage = "Failed to seed default reasons: " + ex.Message;
        }

        await OnGetAsync();
        return Page();
    }
}
