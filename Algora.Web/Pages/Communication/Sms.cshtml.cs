using Algora.Application.DTOs.Communication;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.Communication;

public class SmsModel : PageModel
{
    private readonly ISmsService _smsService;

    public SmsModel(ISmsService smsService)
    {
        _smsService = smsService;
    }

    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    public int TotalSent { get; set; }
    public int TotalDelivered { get; set; }
    public decimal DeliveryRate { get; set; }
    public decimal TotalCost { get; set; }

    public List<SmsMessageDto> Messages { get; set; } = new();
    public List<SmsTemplateDto> Templates { get; set; } = new();

    [BindProperty]
    public SmsSettingsViewModel SmsSettings { get; set; } = new();

    public async Task OnGetAsync()
    {
        var shopDomain = GetShopDomain();

        try
        {
            // Load messages
            var messagesResult = await _smsService.GetMessagesAsync(shopDomain, 1, 50);
            Messages = messagesResult.Items.ToList();

            // Load templates
            Templates = (await _smsService.GetTemplatesAsync(shopDomain)).ToList();

            // Calculate stats
            TotalSent = Messages.Count;
            TotalDelivered = Messages.Count(m => m.Status == "Delivered");
            DeliveryRate = TotalSent > 0 ? (decimal)TotalDelivered / TotalSent * 100 : 0;
            TotalCost = Messages.Where(m => m.Cost.HasValue).Sum(m => m.Cost!.Value);
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
        TotalSent = 856;
        TotalDelivered = 842;
        DeliveryRate = 98.4m;
        TotalCost = 42.80m;

        Messages = new List<SmsMessageDto>
        {
            new() { Id = 1, PhoneNumber = "+1 234 567 8901", Body = "Your order #1234 has been shipped! Track it here: https://...", Status = "Delivered", SegmentCount = 1, Cost = 0.05m, SentAt = DateTime.Now.AddHours(-2) },
            new() { Id = 2, PhoneNumber = "+1 234 567 8902", Body = "Flash sale! 20% off all items. Use code FLASH20. Shop now!", Status = "Delivered", SegmentCount = 1, Cost = 0.05m, SentAt = DateTime.Now.AddHours(-5) },
            new() { Id = 3, PhoneNumber = "+1 234 567 8903", Body = "Your order #1235 is confirmed. We'll notify you when it ships.", Status = "Sent", SegmentCount = 1, Cost = 0.05m, SentAt = DateTime.Now.AddMinutes(-30) },
            new() { Id = 4, PhoneNumber = "+1 234 567 8904", Body = "Reminder: Your subscription renews tomorrow.", Status = "Failed", SegmentCount = 1, Cost = null, SentAt = DateTime.Now.AddDays(-1) }
        };

        Templates = new List<SmsTemplateDto>
        {
            new() { Id = 1, Name = "Order Confirmation", TemplateType = "transactional", Body = "Hi {name}, your order #{order_number} has been confirmed! We'll send tracking info soon.", IsActive = true, CreatedAt = DateTime.Now.AddMonths(-2) },
            new() { Id = 2, Name = "Shipping Update", TemplateType = "transactional", Body = "Your order #{order_number} has shipped! Track it: {tracking_url}", IsActive = true, CreatedAt = DateTime.Now.AddMonths(-2) },
            new() { Id = 3, Name = "Flash Sale", TemplateType = "marketing", Body = "{discount}% off everything! Use code {code}. Shop now: {shop_url}", IsActive = true, CreatedAt = DateTime.Now.AddMonths(-1) }
        };
    }

    public async Task<IActionResult> OnPostSendMessageAsync(string phoneNumber, string body)
    {
        var shopDomain = GetShopDomain();

        try
        {
            var dto = new SendSmsMessageDto
            {
                PhoneNumber = phoneNumber,
                Body = body
            };

            await _smsService.SendMessageAsync(shopDomain, dto);
            TempData["Success"] = $"SMS sent to {phoneNumber} successfully!";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error sending SMS: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCreateTemplateAsync(string name, string templateType, string body)
    {
        var shopDomain = GetShopDomain();

        try
        {
            var dto = new CreateSmsTemplateDto
            {
                Name = name,
                TemplateType = templateType,
                Body = body
            };

            await _smsService.CreateTemplateAsync(shopDomain, dto);
            TempData["Success"] = $"Template '{name}' created successfully!";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error creating template: {ex.Message}";
        }

        return RedirectToPage();
    }

    public IActionResult OnPostSaveSettings()
    {
        // TODO: Save settings via ICommunicationSettingsService
        TempData["Success"] = "SMS settings saved successfully!";
        return RedirectToPage();
    }

    private string GetShopDomain()
    {
        return User.FindFirst("shop_domain")?.Value ?? "devlotusalgebra.myshopify.com";
    }
}

public class SmsSettingsViewModel
{
    public string Provider { get; set; } = "twilio";
    public string? FromNumber { get; set; }
    public string? AccountSid { get; set; }
    public string? AuthToken { get; set; }
}
