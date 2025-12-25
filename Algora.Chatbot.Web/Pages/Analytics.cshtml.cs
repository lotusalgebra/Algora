using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Chatbot.Web.Pages;

public class AnalyticsModel : PageModel
{
    public string ShopDomain { get; set; } = "";

    public void OnGet(string? shop)
    {
        ShopDomain = shop ?? "";
    }
}
