using Algora.Application.DTOs;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace Algora.Web.Pages.AbandonedCheckouts;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IAbandonedCartService _abandonedCartService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(IAbandonedCartService abandonedCartService, ILogger<IndexModel> logger)
    {
        _abandonedCartService = abandonedCartService;
        _logger = logger;
    }

    public IEnumerable<AbandonedCartDto> AbandonedCarts { get; set; } = Enumerable.Empty<AbandonedCartDto>();
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            _logger.LogInformation("Loading abandoned checkouts page");
            AbandonedCarts = await _abandonedCartService.GetAllAsync();
            _logger.LogInformation("Loaded {Count} abandoned checkouts", AbandonedCarts.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load abandoned checkouts");
            ErrorMessage = "Failed to load abandoned checkouts. Please ensure the shop is connected.";
        }
    }

    public async Task<IActionResult> OnPostSendReminderAsync(long checkoutId)
    {
        try
        {
            _logger.LogInformation("Sending reminder for checkout {CheckoutId}", checkoutId);
            var result = await _abandonedCartService.SendReminderAsync(checkoutId);

            if (result)
            {
                SuccessMessage = "Reminder sent successfully!";
            }
            else
            {
                ErrorMessage = "Failed to send reminder. The checkout may no longer exist.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending reminder for checkout {CheckoutId}", checkoutId);
            ErrorMessage = "Failed to send reminder. Please try again.";
        }

        // Reload the data
        try
        {
            AbandonedCarts = await _abandonedCartService.GetAllAsync();
        }
        catch
        {
            AbandonedCarts = Enumerable.Empty<AbandonedCartDto>();
        }

        return Page();
    }
}
