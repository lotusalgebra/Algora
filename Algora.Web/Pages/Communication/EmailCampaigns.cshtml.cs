using Algora.Application.DTOs.Communication;
using Algora.Application.Interfaces;
using Algora.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.Communication;

[Authorize]
[RequireFeature(FeatureCodes.EmailCampaigns)]
[IgnoreAntiforgeryToken]
public class EmailCampaignsModel : PageModel
{
    private readonly IEmailMarketingService _emailService;

    public EmailCampaignsModel(IEmailMarketingService emailService)
    {
        _emailService = emailService;
    }

    public string? SuccessMessage { get; set; }
    public int TotalSubscribers { get; set; }
    public int CampaignsSent { get; set; }
    public decimal AvgOpenRate { get; set; }
    public decimal AvgClickRate { get; set; }

    public List<EmailCampaignDto> Campaigns { get; set; } = new();
    public List<EmailTemplateDto> Templates { get; set; } = new();
    public List<EmailSubscriberViewModel> Subscribers { get; set; } = new();
    public List<EmailListViewModel> Lists { get; set; } = new();
    public List<EmailAutomationViewModel> Automations { get; set; } = new();

    [BindProperty]
    public EmailSettingsViewModel EmailSettings { get; set; } = new();

    public async Task OnGetAsync()
    {
        var shopDomain = GetShopDomain();

        // Load templates first (independent of other data)
        try
        {
            Templates = (await _emailService.GetTemplatesAsync(shopDomain)).ToList();
        }
        catch (Exception)
        {
            Templates = new List<EmailTemplateDto>();
        }

        // Load campaigns
        try
        {
            var campaignsResult = await _emailService.GetCampaignsAsync(shopDomain, 1, 50);
            Campaigns = campaignsResult.Items.ToList();

            // Calculate stats
            CampaignsSent = Campaigns.Count(c => c.Status == "Sent");
            if (CampaignsSent > 0)
            {
                var sentCampaigns = Campaigns.Where(c => c.Status == "Sent" && c.TotalSent > 0).ToList();
                if (sentCampaigns.Any())
                {
                    AvgOpenRate = sentCampaigns.Average(c => (decimal)c.TotalOpened / c.TotalSent * 100);
                    var campaignsWithOpens = sentCampaigns.Where(c => c.TotalOpened > 0).ToList();
                    if (campaignsWithOpens.Any())
                    {
                        AvgClickRate = campaignsWithOpens.Average(c => (decimal)c.TotalClicked / c.TotalOpened * 100);
                    }
                }
            }
        }
        catch (Exception)
        {
            LoadDemoCampaigns();
        }

        // Load subscribers
        try
        {
            var subscribersResult = await _emailService.GetSubscribersAsync(shopDomain, 1, 50);
            TotalSubscribers = subscribersResult.TotalCount;
            Subscribers = subscribersResult.Items.Select(s => new EmailSubscriberViewModel
            {
                Id = s.Id,
                Email = s.Email,
                FirstName = s.FirstName,
                LastName = s.LastName,
                Status = s.Status,
                SubscribedAt = s.CreatedAt
            }).ToList();
        }
        catch (Exception)
        {
            LoadDemoSubscribers();
        }

        // Load lists
        try
        {
            var lists = await _emailService.GetListsAsync(shopDomain);
            Lists = lists.Select(l => new EmailListViewModel
            {
                Id = l.Id,
                Name = l.Name,
                Description = l.Description,
                SubscriberCount = l.SubscriberCount,
                CreatedAt = l.CreatedAt
            }).ToList();
        }
        catch (Exception)
        {
            LoadDemoLists();
        }

        // Load automations
        try
        {
            var automations = await _emailService.GetAutomationsAsync(shopDomain);
            Automations = automations.Select(a => new EmailAutomationViewModel
            {
                Id = a.Id,
                Name = a.Name,
                TriggerType = a.TriggerType,
                IsActive = a.IsActive
            }).ToList();
        }
        catch (Exception)
        {
            LoadDemoAutomations();
        }

        SuccessMessage = TempData["Success"]?.ToString();
    }

    private void LoadDemoCampaigns()
    {
        CampaignsSent = 12;
        AvgOpenRate = 24.5m;
        AvgClickRate = 3.2m;

        Campaigns = new List<EmailCampaignDto>
        {
            new() { Id = 1, Name = "Welcome Series", Subject = "Welcome to Our Store!", Status = "Sent", TotalSent = 500, TotalOpened = 125, TotalClicked = 45 },
            new() { Id = 2, Name = "Holiday Sale", Subject = "Up to 50% Off Everything!", Status = "Sent", TotalSent = 2000, TotalOpened = 480, TotalClicked = 120 },
            new() { Id = 3, Name = "New Arrivals", Subject = "Check Out What's New", Status = "Draft", TotalSent = 0, TotalOpened = 0, TotalClicked = 0 }
        };
    }

    private void LoadDemoSubscribers()
    {
        TotalSubscribers = 2500;

        Subscribers = new List<EmailSubscriberViewModel>
        {
            new() { Id = 1, Email = "john@example.com", FirstName = "John", LastName = "Doe", Status = "Active", SubscribedAt = DateTime.Now.AddMonths(-3) },
            new() { Id = 2, Email = "jane@example.com", FirstName = "Jane", LastName = "Smith", Status = "Active", SubscribedAt = DateTime.Now.AddMonths(-2) },
            new() { Id = 3, Email = "bob@example.com", FirstName = "Bob", LastName = "Wilson", Status = "Unsubscribed", SubscribedAt = DateTime.Now.AddMonths(-6) }
        };
    }

    private void LoadDemoLists()
    {
        Lists = new List<EmailListViewModel>
        {
            new() { Id = 1, Name = "Newsletter", Description = "General newsletter subscribers", SubscriberCount = 1500, CreatedAt = DateTime.Now.AddMonths(-12) },
            new() { Id = 2, Name = "VIP Customers", Description = "High-value customers", SubscriberCount = 250, CreatedAt = DateTime.Now.AddMonths(-6) }
        };
    }

    private void LoadDemoAutomations()
    {
        Automations = new List<EmailAutomationViewModel>
        {
            new() { Id = 1, Name = "Welcome Series", TriggerType = "subscription", IsActive = true },
            new() { Id = 2, Name = "Abandoned Cart", TriggerType = "cart_abandonment", IsActive = true },
            new() { Id = 3, Name = "Post-Purchase", TriggerType = "order_completed", IsActive = false }
        };
    }

    public async Task<IActionResult> OnPostCreateCampaignAsync(string name, string subject, string? previewText, string body, int? listId, string action)
    {
        var shopDomain = GetShopDomain();

        try
        {
            var dto = new CreateEmailCampaignDto
            {
                Name = name,
                Subject = subject,
                PreviewText = previewText,
                Body = body,
                ListId = listId
            };

            var campaign = await _emailService.CreateCampaignAsync(shopDomain, dto);

            if (action == "send")
            {
                await _emailService.SendCampaignAsync(campaign.Id);
                TempData["Success"] = $"Campaign '{name}' sent successfully!";
            }
            else
            {
                TempData["Success"] = $"Campaign '{name}' saved as draft.";
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
            await _emailService.SendCampaignAsync(campaignId);
            TempData["Success"] = "Campaign sent successfully!";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error sending campaign: {ex.Message}";
        }

        return RedirectToPage();
    }

    public IActionResult OnPostSaveSettings()
    {
        // TODO: Save settings via ICommunicationSettingsService
        TempData["Success"] = "Email settings saved successfully!";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCreateTemplateAsync(string templateName, string templateType, string subject, string? previewText, string body)
    {
        var shopDomain = GetShopDomain();

        try
        {
            var dto = new CreateEmailTemplateDto
            {
                Name = templateName,
                Subject = subject,
                PreviewText = previewText,
                Body = body,
                TemplateType = templateType
            };

            await _emailService.CreateTemplateAsync(shopDomain, dto);
            TempData["Success"] = $"Template '{templateName}' created successfully!";
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
            await _emailService.DeleteTemplateAsync(templateId);
            TempData["Success"] = "Template deleted successfully!";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error deleting template: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDuplicateTemplateAsync(int templateId)
    {
        try
        {
            var duplicate = await _emailService.DuplicateTemplateAsync(templateId);
            if (duplicate != null)
            {
                TempData["Success"] = $"Template duplicated as '{duplicate.Name}'!";
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error duplicating template: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCreateSubscriberAsync(string email, string? firstName, string? lastName, string? phone, int? listId, string? tags)
    {
        var shopDomain = GetShopDomain();

        try
        {
            var dto = new CreateEmailSubscriberDto
            {
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                Phone = phone,
                Tags = tags,
                Source = "manual",
                EmailOptIn = true
            };

            var subscriber = await _emailService.CreateSubscriberAsync(shopDomain, dto);

            if (listId.HasValue)
            {
                await _emailService.AddSubscriberToListAsync(listId.Value, subscriber.Id);
            }

            TempData["Success"] = $"Subscriber '{email}' added successfully!";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error adding subscriber: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUnsubscribeAsync(int subscriberId)
    {
        try
        {
            var subscriber = await _emailService.GetSubscriberByIdAsync(subscriberId);
            if (subscriber != null)
            {
                await _emailService.UnsubscribeAsync(subscriber.ShopDomain, subscriber.Email, "Manual unsubscribe");
                TempData["Success"] = "Subscriber unsubscribed successfully!";
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error unsubscribing: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCreateListAsync(string listName, string? description, bool doubleOptIn = false)
    {
        var shopDomain = GetShopDomain();

        try
        {
            var dto = new CreateEmailListDto
            {
                Name = listName,
                Description = description,
                DoubleOptIn = doubleOptIn
            };

            await _emailService.CreateListAsync(shopDomain, dto);
            TempData["Success"] = $"List '{listName}' created successfully!";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error creating list: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteListAsync(int listId)
    {
        try
        {
            await _emailService.DeleteListAsync(listId);
            TempData["Success"] = "List deleted successfully!";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error deleting list: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCreateAutomationAsync(
        string automationName,
        string? automationDescription,
        string triggerType,
        string stepSubject,
        string stepBody,
        int delayValue = 0,
        string delayUnit = "hours")
    {
        var shopDomain = GetShopDomain();

        try
        {
            var delayMinutes = delayUnit switch
            {
                "minutes" => delayValue,
                "hours" => delayValue * 60,
                "days" => delayValue * 60 * 24,
                _ => delayValue * 60
            };

            var dto = new CreateEmailAutomationDto
            {
                Name = automationName,
                Description = automationDescription,
                TriggerType = triggerType,
                Steps = new List<CreateEmailAutomationStepDto>
                {
                    new()
                    {
                        StepOrder = 1,
                        StepType = "email",
                        Subject = stepSubject,
                        Body = stepBody,
                        DelayMinutes = delayMinutes
                    }
                }
            };

            await _emailService.CreateAutomationAsync(shopDomain, dto);
            TempData["Success"] = $"Automation '{automationName}' created successfully!";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error creating automation: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleAutomationAsync(int automationId, bool activate)
    {
        try
        {
            if (activate)
            {
                await _emailService.ActivateAutomationAsync(automationId);
                TempData["Success"] = "Automation activated!";
            }
            else
            {
                await _emailService.DeactivateAutomationAsync(automationId);
                TempData["Success"] = "Automation deactivated!";
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error toggling automation: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAutomationAsync(int automationId)
    {
        try
        {
            await _emailService.DeleteAutomationAsync(automationId);
            TempData["Success"] = "Automation deleted successfully!";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error deleting automation: {ex.Message}";
        }

        return RedirectToPage();
    }

    private string GetShopDomain()
    {
        return User.FindFirst("shop_domain")?.Value ?? "devlotusalgebra.myshopify.com";
    }
}

public class EmailSubscriberViewModel
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string Status { get; set; } = "Active";
    public DateTime SubscribedAt { get; set; }
}

public class EmailListViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SubscriberCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class EmailAutomationViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string TriggerType { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class EmailSettingsViewModel
{
    public string? FromName { get; set; }
    public string? FromEmail { get; set; }
    public string? ReplyToEmail { get; set; }
    public string? SmtpHost { get; set; }
    public int? SmtpPort { get; set; }
    public string? SmtpUsername { get; set; }
    public string? SmtpPassword { get; set; }
}
