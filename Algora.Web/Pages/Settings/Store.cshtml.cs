using Algora.Application.DTOs.Communication;
using Algora.Application.Interfaces;
using Algora.Domain.Entities;
using Algora.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Algora.Web.Pages.Settings;

[Authorize]
public class StoreModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IShopContext _shopContext;
    private readonly ICommunicationSettingsService _commService;
    private readonly ILogger<StoreModel> _logger;

    public StoreModel(
        AppDbContext db,
        IShopContext shopContext,
        ICommunicationSettingsService commService,
        ILogger<StoreModel> logger)
    {
        _db = db;
        _shopContext = shopContext;
        _commService = commService;
        _logger = logger;
    }

    public Shop? Shop { get; set; }
    public CommunicationSettingsDto CommunicationSettings { get; set; } = new();
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    [BindProperty(SupportsGet = true)]
    public string ActiveTab { get; set; } = "shopify";

    // Shopify Credentials
    [BindProperty]
    public bool UseCustomCredentials { get; set; }

    [BindProperty]
    public string? CustomApiKey { get; set; }

    [BindProperty]
    public string? CustomApiSecret { get; set; }

    [BindProperty]
    public string? CustomScopes { get; set; }

    [BindProperty]
    public string? CustomAppUrl { get; set; }

    // Email Settings
    [BindProperty]
    public string? EmailProvider { get; set; }

    [BindProperty]
    public string? EmailApiKey { get; set; }

    [BindProperty]
    public string? SmtpHost { get; set; }

    [BindProperty]
    public int SmtpPort { get; set; }

    [BindProperty]
    public string? SmtpUsername { get; set; }

    [BindProperty]
    public string? SmtpPassword { get; set; }

    [BindProperty]
    public bool SmtpUseSsl { get; set; }

    [BindProperty]
    public string? DefaultFromName { get; set; }

    [BindProperty]
    public string? DefaultFromEmail { get; set; }

    [BindProperty]
    public string? DefaultReplyTo { get; set; }

    [BindProperty]
    public bool EmailEnabled { get; set; }

    // SMS Settings
    [BindProperty]
    public string? SmsProvider { get; set; }

    [BindProperty]
    public string? SmsAccountSid { get; set; }

    [BindProperty]
    public string? SmsAuthToken { get; set; }

    [BindProperty]
    public string? SmsFromNumber { get; set; }

    [BindProperty]
    public bool SmsEnabled { get; set; }

    // WhatsApp Settings
    [BindProperty]
    public string? WhatsAppAccessToken { get; set; }

    [BindProperty]
    public string? WhatsAppPhoneNumberId { get; set; }

    [BindProperty]
    public string? WhatsAppBusinessAccountId { get; set; }

    [BindProperty]
    public string? WhatsAppWebhookVerifyToken { get; set; }

    [BindProperty]
    public bool WhatsAppEnabled { get; set; }

    public async Task OnGetAsync()
    {
        await LoadDataAsync();
    }

    public async Task<IActionResult> OnPostSaveShopifyAsync()
    {
        try
        {
            var shop = await _db.Shops.FirstOrDefaultAsync(s => s.Domain == _shopContext.ShopDomain);
            if (shop == null)
            {
                ErrorMessage = "Shop not found.";
                await LoadDataAsync();
                return Page();
            }

            shop.UseCustomCredentials = UseCustomCredentials;

            if (UseCustomCredentials)
            {
                if (!string.IsNullOrWhiteSpace(CustomApiKey))
                    shop.CustomApiKey = CustomApiKey;
                if (!string.IsNullOrWhiteSpace(CustomApiSecret))
                    shop.CustomApiSecret = CustomApiSecret;
                shop.CustomScopes = CustomScopes;
                shop.CustomAppUrl = CustomAppUrl;
            }
            else
            {
                shop.CustomApiKey = null;
                shop.CustomApiSecret = null;
                shop.CustomScopes = null;
                shop.CustomAppUrl = null;
            }

            shop.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            SuccessMessage = "Shopify credentials updated successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save Shopify credentials");
            ErrorMessage = "Failed to save credentials. Please try again.";
        }

        await LoadDataAsync();
        ActiveTab = "shopify";
        return Page();
    }

    public async Task<IActionResult> OnPostSaveEmailAsync()
    {
        try
        {
            var updateDto = new UpdateCommunicationSettingsDto
            {
                EmailProvider = EmailProvider,
                EmailApiKey = string.IsNullOrWhiteSpace(EmailApiKey) ? null : EmailApiKey,
                SmtpHost = SmtpHost,
                SmtpPort = SmtpPort,
                SmtpUsername = SmtpUsername,
                SmtpPassword = string.IsNullOrWhiteSpace(SmtpPassword) ? null : SmtpPassword,
                SmtpUseSsl = SmtpUseSsl,
                DefaultFromName = DefaultFromName,
                DefaultFromEmail = DefaultFromEmail,
                DefaultReplyTo = DefaultReplyTo,
                EmailEnabled = EmailEnabled
            };

            await _commService.SaveSettingsAsync(_shopContext.ShopDomain, updateDto);
            SuccessMessage = "Email settings saved successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save email settings");
            ErrorMessage = "Failed to save settings. Please try again.";
        }

        await LoadDataAsync();
        ActiveTab = "email";
        return Page();
    }

    public async Task<IActionResult> OnPostSaveSmsAsync()
    {
        try
        {
            var updateDto = new UpdateCommunicationSettingsDto
            {
                SmsProvider = SmsProvider,
                SmsAccountSid = SmsAccountSid,
                SmsAuthToken = string.IsNullOrWhiteSpace(SmsAuthToken) ? null : SmsAuthToken,
                SmsFromNumber = SmsFromNumber,
                SmsEnabled = SmsEnabled
            };

            await _commService.SaveSettingsAsync(_shopContext.ShopDomain, updateDto);
            SuccessMessage = "SMS settings saved successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save SMS settings");
            ErrorMessage = "Failed to save settings. Please try again.";
        }

        await LoadDataAsync();
        ActiveTab = "sms";
        return Page();
    }

    public async Task<IActionResult> OnPostSaveWhatsAppAsync()
    {
        try
        {
            var updateDto = new UpdateCommunicationSettingsDto
            {
                WhatsAppProvider = "meta",
                WhatsAppAccessToken = string.IsNullOrWhiteSpace(WhatsAppAccessToken) ? null : WhatsAppAccessToken,
                WhatsAppPhoneNumberId = WhatsAppPhoneNumberId,
                WhatsAppBusinessAccountId = WhatsAppBusinessAccountId,
                WhatsAppWebhookVerifyToken = WhatsAppWebhookVerifyToken,
                WhatsAppEnabled = WhatsAppEnabled
            };

            await _commService.SaveSettingsAsync(_shopContext.ShopDomain, updateDto);
            SuccessMessage = "WhatsApp settings saved successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save WhatsApp settings");
            ErrorMessage = "Failed to save settings. Please try again.";
        }

        await LoadDataAsync();
        ActiveTab = "whatsapp";
        return Page();
    }

    public async Task<IActionResult> OnPostTestEmailAsync()
    {
        var success = await _commService.TestEmailConnectionAsync(_shopContext.ShopDomain);
        return new JsonResult(new { success, message = success ? "Email connection successful" : "Email connection failed" });
    }

    public async Task<IActionResult> OnPostTestSmsAsync()
    {
        var success = await _commService.TestSmsConnectionAsync(_shopContext.ShopDomain);
        return new JsonResult(new { success, message = success ? "SMS connection successful" : "SMS connection failed" });
    }

    public async Task<IActionResult> OnPostTestWhatsAppAsync()
    {
        var success = await _commService.TestWhatsAppConnectionAsync(_shopContext.ShopDomain);
        return new JsonResult(new { success, message = success ? "WhatsApp connection successful" : "WhatsApp connection failed" });
    }

    private async Task LoadDataAsync()
    {
        Shop = await _db.Shops.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Domain == _shopContext.ShopDomain);

        CommunicationSettings = await _commService.GetOrCreateSettingsAsync(_shopContext.ShopDomain);

        // Populate Shopify form properties
        if (Shop != null)
        {
            UseCustomCredentials = Shop.UseCustomCredentials;
            CustomScopes = Shop.CustomScopes;
            CustomAppUrl = Shop.CustomAppUrl;
        }

        // Populate Email form properties
        EmailProvider = CommunicationSettings.EmailProvider ?? "smtp";
        SmtpHost = CommunicationSettings.SmtpHost;
        SmtpPort = CommunicationSettings.SmtpPort ?? 587;
        SmtpUseSsl = CommunicationSettings.SmtpUseSsl;
        DefaultFromName = CommunicationSettings.DefaultFromName;
        DefaultFromEmail = CommunicationSettings.DefaultFromEmail;
        DefaultReplyTo = CommunicationSettings.DefaultReplyTo;
        EmailEnabled = CommunicationSettings.EmailEnabled;

        // Populate SMS form properties
        SmsProvider = CommunicationSettings.SmsProvider ?? "twilio";
        SmsFromNumber = CommunicationSettings.SmsFromNumber;
        SmsEnabled = CommunicationSettings.SmsEnabled;

        // Populate WhatsApp form properties
        WhatsAppPhoneNumberId = CommunicationSettings.WhatsAppPhoneNumberId;
        WhatsAppEnabled = CommunicationSettings.WhatsAppEnabled;
    }
}
