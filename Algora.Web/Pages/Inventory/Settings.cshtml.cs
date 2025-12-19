using Algora.Application.DTOs.Inventory;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Algora.Web.Pages.Inventory;

[Authorize]
public class SettingsModel : PageModel
{
    private readonly IInventoryAlertService _alertService;
    private readonly ILogger<SettingsModel> _logger;

    public SettingsModel(IInventoryAlertService alertService, ILogger<SettingsModel> logger)
    {
        _alertService = alertService;
        _logger = logger;
    }

    [BindProperty]
    public SettingsInput Settings { get; set; } = new();

    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            var settings = await _alertService.GetSettingsAsync(HttpContext.GetShopDomain());
            Settings = new SettingsInput
            {
                AlertsEnabled = settings.AlertsEnabled,
                LowStockDaysThreshold = settings.LowStockDaysThreshold,
                CriticalStockDaysThreshold = settings.CriticalStockDaysThreshold,
                DefaultLeadTimeDays = settings.DefaultLeadTimeDays,
                DefaultSafetyStockDays = settings.DefaultSafetyStockDays,
                EmailNotificationsEnabled = settings.EmailNotificationsEnabled,
                NotificationEmail = settings.NotificationEmail,
                SmsNotificationsEnabled = settings.SmsNotificationsEnabled,
                NotificationPhone = settings.NotificationPhone,
                WhatsAppNotificationsEnabled = settings.WhatsAppNotificationsEnabled,
                WhatsAppPhone = settings.WhatsAppPhone,
                MinHoursBetweenAlerts = settings.MinHoursBetweenAlerts,
                DailyDigestEnabled = settings.DailyDigestEnabled
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading settings");
            ErrorMessage = "Failed to load settings.";
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            await _alertService.UpdateSettingsAsync(HttpContext.GetShopDomain(), new UpdateInventoryAlertSettingsDto
            {
                AlertsEnabled = Settings.AlertsEnabled,
                LowStockDaysThreshold = Settings.LowStockDaysThreshold,
                CriticalStockDaysThreshold = Settings.CriticalStockDaysThreshold,
                DefaultLeadTimeDays = Settings.DefaultLeadTimeDays,
                DefaultSafetyStockDays = Settings.DefaultSafetyStockDays,
                EmailNotificationsEnabled = Settings.EmailNotificationsEnabled,
                NotificationEmail = Settings.NotificationEmail,
                SmsNotificationsEnabled = Settings.SmsNotificationsEnabled,
                NotificationPhone = Settings.NotificationPhone,
                WhatsAppNotificationsEnabled = Settings.WhatsAppNotificationsEnabled,
                WhatsAppPhone = Settings.WhatsAppPhone,
                MinHoursBetweenAlerts = Settings.MinHoursBetweenAlerts,
                DailyDigestEnabled = Settings.DailyDigestEnabled
            });

            SuccessMessage = "Settings saved successfully!";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving settings");
            ErrorMessage = "Failed to save settings.";
        }

        return Page();
    }

    public class SettingsInput
    {
        public bool AlertsEnabled { get; set; } = true;
        public int LowStockDaysThreshold { get; set; } = 14;
        public int CriticalStockDaysThreshold { get; set; } = 7;
        public int DefaultLeadTimeDays { get; set; } = 7;
        public int DefaultSafetyStockDays { get; set; } = 3;
        public bool EmailNotificationsEnabled { get; set; } = true;
        public string? NotificationEmail { get; set; }
        public bool SmsNotificationsEnabled { get; set; }
        public string? NotificationPhone { get; set; }
        public bool WhatsAppNotificationsEnabled { get; set; }
        public string? WhatsAppPhone { get; set; }
        public int MinHoursBetweenAlerts { get; set; } = 24;
        public bool DailyDigestEnabled { get; set; }
    }
}
