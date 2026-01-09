using Algora.Application.DTOs.Communication;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.Communication;

public class HistoryDetailsModel : PageModel
{
    private readonly ICommunicationHistoryService _historyService;

    public HistoryDetailsModel(ICommunicationHistoryService historyService)
    {
        _historyService = historyService;
    }

    public CommunicationHistoryItemDto? Item { get; set; }
    public string Channel { get; set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync(string channel, int id)
    {
        var shopDomain = GetShopDomain();
        Channel = channel;

        try
        {
            Item = await _historyService.GetMessageDetailsAsync(shopDomain, channel, id);
        }
        catch (Exception)
        {
            Item = null;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostResendAsync(string channel, int id)
    {
        var shopDomain = GetShopDomain();

        try
        {
            await _historyService.ResendMessageAsync(shopDomain, channel, id);
            TempData["Success"] = "Message has been queued for resending.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Failed to resend message: {ex.Message}";
        }

        return RedirectToPage(new { channel, id });
    }

    private string GetShopDomain()
    {
        return User.FindFirst("shop_domain")?.Value ?? "demo.myshopify.com";
    }
}
