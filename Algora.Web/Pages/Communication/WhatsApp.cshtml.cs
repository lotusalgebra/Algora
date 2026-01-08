using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.Communication;

public class WhatsAppModel : PageModel
{
    public int TemplateCount { get; set; }
    public int CampaignCount { get; set; }
    public int MessagesSent { get; set; }
    public int ActiveConversations { get; set; }

    public List<WhatsAppTemplateViewModel> Templates { get; set; } = new();
    public List<WhatsAppCampaignViewModel> Campaigns { get; set; } = new();
    public List<WhatsAppConversationViewModel> Conversations { get; set; } = new();

    [BindProperty]
    public WhatsAppSettingsViewModel Settings { get; set; } = new();

    public void OnGet()
    {
        // TODO: Load from WhatsApp service when integrated
        // For now, show demo data
        TemplateCount = 3;
        CampaignCount = 2;
        MessagesSent = 1250;
        ActiveConversations = 8;

        Templates = new List<WhatsAppTemplateViewModel>
        {
            new() { Name = "order_confirmation", Category = "UTILITY", Status = "APPROVED", Language = "en" },
            new() { Name = "shipping_update", Category = "UTILITY", Status = "APPROVED", Language = "en" },
            new() { Name = "promotional_offer", Category = "MARKETING", Status = "PENDING", Language = "en" }
        };

        Campaigns = new List<WhatsAppCampaignViewModel>
        {
            new() { Name = "Holiday Sale 2024", TemplateName = "promotional_offer", TotalRecipients = 500, TotalSent = 485, TotalDelivered = 478, Status = "Sent" },
            new() { Name = "New Year Promo", TemplateName = "promotional_offer", TotalRecipients = 1000, TotalSent = 0, TotalDelivered = 0, Status = "Scheduled" }
        };

        Conversations = new List<WhatsAppConversationViewModel>
        {
            new() { CustomerName = "John Doe", PhoneNumber = "+1 234 567 8901", LastMessageAt = DateTime.Now.AddMinutes(-15), Status = "Open" },
            new() { CustomerName = "Jane Smith", PhoneNumber = "+1 234 567 8902", LastMessageAt = DateTime.Now.AddHours(-2), Status = "Open" },
            new() { CustomerName = "Bob Wilson", PhoneNumber = "+1 234 567 8903", LastMessageAt = DateTime.Now.AddDays(-1), Status = "Closed" }
        };
    }

    public IActionResult OnPostSaveSettings()
    {
        // TODO: Save settings via ICommunicationSettingsService
        TempData["Success"] = "WhatsApp settings saved successfully!";
        return RedirectToPage();
    }

    public IActionResult OnPostCreateTemplate(string templateName, string category, string body)
    {
        // TODO: Create template via WhatsApp service
        TempData["Success"] = $"Template '{templateName}' created successfully!";
        return RedirectToPage();
    }
}

public class WhatsAppTemplateViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Language { get; set; } = "en";
}

public class WhatsAppCampaignViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string TemplateName { get; set; } = string.Empty;
    public int TotalRecipients { get; set; }
    public int TotalSent { get; set; }
    public int TotalDelivered { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class WhatsAppConversationViewModel
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public DateTime LastMessageAt { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class WhatsAppSettingsViewModel
{
    public string? PhoneNumberId { get; set; }
    public string? BusinessAccountId { get; set; }
    public string? AccessToken { get; set; }
    public string? WebhookVerifyToken { get; set; }
}
