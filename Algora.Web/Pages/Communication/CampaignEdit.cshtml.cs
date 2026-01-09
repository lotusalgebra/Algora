using Algora.Application.DTOs.Communication;
using Algora.Application.Interfaces;
using Algora.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.Communication;

[Authorize]
[RequireFeature(FeatureCodes.EmailCampaigns)]
public class CampaignEditModel : PageModel
{
    private readonly IEmailMarketingService _emailService;

    public CampaignEditModel(IEmailMarketingService emailService)
    {
        _emailService = emailService;
    }

    public EmailCampaignDto? Campaign { get; set; }
    public List<EmailListViewModel> Lists { get; set; } = new();
    public List<EmailTemplateDto> Templates { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    [BindProperty]
    public CampaignEditInput Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var shopDomain = GetShopDomain();

        try
        {
            Campaign = await _emailService.GetCampaignAsync(id);
            if (Campaign == null)
            {
                ErrorMessage = "Campaign not found.";
                return Page();
            }

            // Pre-fill the form
            Input = new CampaignEditInput
            {
                Id = Campaign.Id,
                Name = Campaign.Name,
                Subject = Campaign.Subject,
                PreviewText = Campaign.PreviewText,
                Body = Campaign.Body,
                FromName = Campaign.FromName,
                FromEmail = Campaign.FromEmail
            };

            // Load lists
            var lists = await _emailService.GetListsAsync(shopDomain);
            Lists = lists.Select(l => new EmailListViewModel
            {
                Id = l.Id,
                Name = l.Name,
                SubscriberCount = l.SubscriberCount
            }).ToList();

            // Load templates
            Templates = (await _emailService.GetTemplatesAsync(shopDomain)).ToList();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error loading campaign: {ex.Message}";
        }

        SuccessMessage = TempData["Success"]?.ToString();
        return Page();
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadPageDataAsync();
            return Page();
        }

        try
        {
            var dto = new UpdateEmailCampaignDto
            {
                Name = Input.Name,
                Subject = Input.Subject,
                PreviewText = Input.PreviewText,
                Body = Input.Body,
                FromName = Input.FromName,
                FromEmail = Input.FromEmail
            };

            await _emailService.UpdateCampaignAsync(Input.Id, dto);
            TempData["Success"] = "Campaign saved successfully!";
            return RedirectToPage(new { id = Input.Id });
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error saving campaign: {ex.Message}";
            await LoadPageDataAsync();
            return Page();
        }
    }

    public async Task<IActionResult> OnPostSendAsync()
    {
        try
        {
            // First save the campaign
            var dto = new UpdateEmailCampaignDto
            {
                Name = Input.Name,
                Subject = Input.Subject,
                PreviewText = Input.PreviewText,
                Body = Input.Body,
                FromName = Input.FromName,
                FromEmail = Input.FromEmail
            };

            await _emailService.UpdateCampaignAsync(Input.Id, dto);

            // Then send it
            await _emailService.SendCampaignAsync(Input.Id);
            TempData["Success"] = "Campaign sent successfully!";
            return RedirectToPage("/Communication/EmailCampaigns");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error sending campaign: {ex.Message}";
            await LoadPageDataAsync();
            return Page();
        }
    }

    public async Task<IActionResult> OnPostScheduleAsync(DateTime scheduledAt)
    {
        try
        {
            // First save the campaign
            var dto = new UpdateEmailCampaignDto
            {
                Name = Input.Name,
                Subject = Input.Subject,
                PreviewText = Input.PreviewText,
                Body = Input.Body,
                FromName = Input.FromName,
                FromEmail = Input.FromEmail
            };

            await _emailService.UpdateCampaignAsync(Input.Id, dto);

            // Then schedule it
            await _emailService.ScheduleCampaignAsync(Input.Id, scheduledAt);
            TempData["Success"] = $"Campaign scheduled for {scheduledAt:MMM dd, yyyy HH:mm}!";
            return RedirectToPage("/Communication/EmailCampaigns");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error scheduling campaign: {ex.Message}";
            await LoadPageDataAsync();
            return Page();
        }
    }

    public async Task<IActionResult> OnPostLoadTemplateAsync(int templateId)
    {
        try
        {
            var template = await _emailService.GetTemplateAsync(templateId);
            if (template != null)
            {
                Input.Subject = template.Subject;
                Input.PreviewText = template.PreviewText;
                Input.Body = template.Body;
            }

            await LoadPageDataAsync();
            return Page();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error loading template: {ex.Message}";
            await LoadPageDataAsync();
            return Page();
        }
    }

    private async Task LoadPageDataAsync()
    {
        var shopDomain = GetShopDomain();

        Campaign = await _emailService.GetCampaignAsync(Input.Id);

        var lists = await _emailService.GetListsAsync(shopDomain);
        Lists = lists.Select(l => new EmailListViewModel
        {
            Id = l.Id,
            Name = l.Name,
            SubscriberCount = l.SubscriberCount
        }).ToList();

        Templates = (await _emailService.GetTemplatesAsync(shopDomain)).ToList();
    }

    private string GetShopDomain()
    {
        return User.FindFirst("shop_domain")?.Value ?? "devlotusalgebra.myshopify.com";
    }
}

public class CampaignEditInput
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string? PreviewText { get; set; }
    public string Body { get; set; } = string.Empty;
    public string? FromName { get; set; }
    public string? FromEmail { get; set; }
}
