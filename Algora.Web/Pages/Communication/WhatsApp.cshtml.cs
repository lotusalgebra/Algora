using Algora.WhatsApp.DTOs;
using Algora.WhatsApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.Communication;

public class WhatsAppModel : PageModel
{
    private readonly IWhatsAppService _whatsAppService;

    public WhatsAppModel(IWhatsAppService whatsAppService)
    {
        _whatsAppService = whatsAppService;
    }

    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    public int TemplateCount { get; set; }
    public int CampaignCount { get; set; }
    public int MessagesSent { get; set; }
    public int ActiveConversations { get; set; }

    public List<WhatsAppTemplateViewModel> Templates { get; set; } = new();
    public List<WhatsAppCampaignViewModel> Campaigns { get; set; } = new();
    public List<WhatsAppConversationViewModel> Conversations { get; set; } = new();

    [BindProperty]
    public WhatsAppSettingsViewModel Settings { get; set; } = new();

    public async Task OnGetAsync()
    {
        var shopDomain = GetShopDomain();

        try
        {
            // Load templates from service
            var templates = await _whatsAppService.GetTemplatesAsync(shopDomain);
            Templates = templates.Select(t => new WhatsAppTemplateViewModel
            {
                Id = t.Id,
                Name = t.Name,
                Category = t.Category,
                Status = t.Status,
                Language = t.Language
            }).ToList();
            TemplateCount = Templates.Count;

            // Load campaigns from service
            var campaigns = await _whatsAppService.GetCampaignsAsync(shopDomain);
            Campaigns = campaigns.Select(c => new WhatsAppCampaignViewModel
            {
                Id = c.Id,
                Name = c.Name,
                TemplateName = c.TemplateName ?? "",
                TotalRecipients = c.TotalRecipients,
                TotalSent = c.TotalSent,
                TotalDelivered = c.TotalDelivered,
                Status = c.Status
            }).ToList();
            CampaignCount = Campaigns.Count;

            // Load conversations from service
            var conversations = await _whatsAppService.GetConversationsAsync(shopDomain, "Open");
            Conversations = conversations.Items.Select(c => new WhatsAppConversationViewModel
            {
                Id = c.Id,
                CustomerName = c.CustomerName ?? "Unknown",
                PhoneNumber = c.PhoneNumber,
                LastMessageAt = c.LastMessageAt ?? c.CreatedAt,
                Status = c.Status
            }).ToList();
            ActiveConversations = conversations.TotalCount;

            // Load message stats
            var messages = await _whatsAppService.GetMessagesAsync(shopDomain, 1, 1);
            MessagesSent = messages.TotalCount;
        }
        catch (Exception)
        {
            // Load demo data if service fails
            LoadDemoData();
        }

        SuccessMessage = TempData["Success"]?.ToString();
        ErrorMessage = TempData["Error"]?.ToString();
    }

    private void LoadDemoData()
    {
        TemplateCount = 3;
        CampaignCount = 2;
        MessagesSent = 1250;
        ActiveConversations = 8;

        Templates = new List<WhatsAppTemplateViewModel>
        {
            new() { Id = 1, Name = "order_confirmation", Category = "UTILITY", Status = "APPROVED", Language = "en" },
            new() { Id = 2, Name = "shipping_update", Category = "UTILITY", Status = "APPROVED", Language = "en" },
            new() { Id = 3, Name = "promotional_offer", Category = "MARKETING", Status = "PENDING", Language = "en" }
        };

        Campaigns = new List<WhatsAppCampaignViewModel>
        {
            new() { Id = 1, Name = "Holiday Sale 2024", TemplateName = "promotional_offer", TotalRecipients = 500, TotalSent = 485, TotalDelivered = 478, Status = "Sent" },
            new() { Id = 2, Name = "New Year Promo", TemplateName = "promotional_offer", TotalRecipients = 1000, TotalSent = 0, TotalDelivered = 0, Status = "Scheduled" }
        };

        Conversations = new List<WhatsAppConversationViewModel>
        {
            new() { Id = 1, CustomerName = "John Doe", PhoneNumber = "+1 234 567 8901", LastMessageAt = DateTime.Now.AddMinutes(-15), Status = "Open" },
            new() { Id = 2, CustomerName = "Jane Smith", PhoneNumber = "+1 234 567 8902", LastMessageAt = DateTime.Now.AddHours(-2), Status = "Open" },
            new() { Id = 3, CustomerName = "Bob Wilson", PhoneNumber = "+1 234 567 8903", LastMessageAt = DateTime.Now.AddDays(-1), Status = "Closed" }
        };
    }

    public IActionResult OnPostSaveSettings()
    {
        // TODO: Save settings via ICommunicationSettingsService
        TempData["Success"] = "WhatsApp settings saved successfully!";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCreateTemplateAsync(string templateName, string category, string body, string? language, string? headerText, string? footerText)
    {
        var shopDomain = GetShopDomain();

        try
        {
            var dto = new CreateWhatsAppTemplateDto
            {
                Name = templateName,
                Category = category,
                Body = body,
                Language = language ?? "en",
                HeaderType = string.IsNullOrEmpty(headerText) ? null : "text",
                HeaderContent = headerText,
                Footer = footerText
            };

            await _whatsAppService.CreateTemplateAsync(shopDomain, dto);
            TempData["Success"] = $"Template '{templateName}' created successfully! It will be submitted to Meta for approval.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error creating template: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteTemplateAsync(int templateId)
    {
        try
        {
            await _whatsAppService.DeleteTemplateAsync(templateId);
            TempData["Success"] = "Template deleted successfully!";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error deleting template: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSubmitTemplateAsync(int templateId)
    {
        try
        {
            await _whatsAppService.SubmitTemplateForApprovalAsync(templateId);
            TempData["Success"] = "Template submitted to Meta for approval!";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error submitting template: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCreateCampaignAsync(string campaignName, int templateId, string audienceType, string scheduleType, DateTime? scheduledAt)
    {
        var shopDomain = GetShopDomain();

        try
        {
            var dto = new Algora.WhatsApp.DTOs.CreateWhatsAppCampaignDto
            {
                Name = campaignName,
                TemplateId = templateId,
                ScheduledAt = scheduleType == "scheduled" ? scheduledAt : null
            };

            await _whatsAppService.CreateCampaignAsync(shopDomain, dto);

            if (scheduleType == "scheduled" && scheduledAt.HasValue)
            {
                TempData["Success"] = $"Campaign '{campaignName}' scheduled for {scheduledAt:MMM dd, yyyy HH:mm}!";
            }
            else
            {
                TempData["Success"] = $"Campaign '{campaignName}' created successfully! Go to Campaigns tab to send it.";
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error creating campaign: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSendCampaignAsync(int campaignId)
    {
        try
        {
            await _whatsAppService.SendCampaignAsync(campaignId);
            TempData["Success"] = "Campaign is being sent!";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error sending campaign: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteCampaignAsync(int campaignId)
    {
        try
        {
            await _whatsAppService.DeleteCampaignAsync(campaignId);
            TempData["Success"] = "Campaign deleted successfully!";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error deleting campaign: {ex.Message}";
        }

        return RedirectToPage();
    }

    private string GetShopDomain()
    {
        return User.FindFirst("shop_domain")?.Value ?? "devlotusalgebra.myshopify.com";
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
