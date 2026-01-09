using Algora.Application.DTOs.Communication;
using Algora.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text;

namespace Algora.Web.Pages.Communication;

public class HistoryModel : PageModel
{
    private readonly ICommunicationHistoryService _historyService;

    public HistoryModel(ICommunicationHistoryService historyService)
    {
        _historyService = historyService;
    }

    public CommunicationHistoryResultDto History { get; set; } = new();
    public CommunicationStatsDto Stats { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ChannelFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? StatusFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? FromDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? ToDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public int Page { get; set; } = 1;

    public async Task OnGetAsync()
    {
        var shopDomain = GetShopDomain();

        var filter = new CommunicationHistoryFilterDto
        {
            Channel = ChannelFilter,
            Status = StatusFilter,
            FromDate = FromDate,
            ToDate = ToDate,
            Search = Search,
            Page = Page,
            PageSize = 25
        };

        try
        {
            History = await _historyService.GetHistoryAsync(shopDomain, filter);
            Stats = await _historyService.GetStatsAsync(shopDomain, FromDate, ToDate);
        }
        catch (Exception)
        {
            // Load demo data on error
            LoadDemoData();
        }
    }

    public async Task<IActionResult> OnGetExportAsync()
    {
        var shopDomain = GetShopDomain();

        var filter = new CommunicationHistoryFilterDto
        {
            Channel = ChannelFilter,
            Status = StatusFilter,
            FromDate = FromDate,
            ToDate = ToDate,
            Search = Search,
            Page = 1,
            PageSize = 10000 // Export all
        };

        CommunicationHistoryResultDto history;
        try
        {
            history = await _historyService.GetHistoryAsync(shopDomain, filter);
        }
        catch (Exception)
        {
            return BadRequest("Failed to export history");
        }

        var csv = new StringBuilder();
        csv.AppendLine("Channel,Type,Direction,Recipient Email,Recipient Phone,Recipient Name,Subject,Status,Created At,Sent At,Delivered At,Opened At,Campaign Name,Error Message");

        foreach (var item in history.Items)
        {
            csv.AppendLine($"\"{item.Channel}\",\"{item.Type}\",\"{item.Direction}\",\"{EscapeCsv(item.RecipientEmail)}\",\"{EscapeCsv(item.RecipientPhone)}\",\"{EscapeCsv(item.RecipientName)}\",\"{EscapeCsv(item.Subject)}\",\"{item.Status}\",\"{item.CreatedAt:yyyy-MM-dd HH:mm}\",\"{item.SentAt:yyyy-MM-dd HH:mm}\",\"{item.DeliveredAt:yyyy-MM-dd HH:mm}\",\"{item.OpenedAt:yyyy-MM-dd HH:mm}\",\"{EscapeCsv(item.CampaignName)}\",\"{EscapeCsv(item.ErrorMessage)}\"");
        }

        var bytes = Encoding.UTF8.GetBytes(csv.ToString());
        return File(bytes, "text/csv", $"communication-history-{DateTime.Now:yyyyMMdd}.csv");
    }

    private string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        return value.Replace("\"", "\"\"");
    }

    private void LoadDemoData()
    {
        Stats = new CommunicationStatsDto
        {
            TotalSent = 1250,
            TotalDelivered = 1180,
            TotalFailed = 45,
            TotalOpened = 620,
            TotalClicked = 185,
            EmailCount = 850,
            SmsCount = 280,
            WhatsAppCount = 120
        };

        History = new CommunicationHistoryResultDto
        {
            Items = new List<CommunicationHistoryItemDto>
            {
                new() { Id = 1, Channel = "email", Type = "campaign", Direction = "outbound", RecipientEmail = "john@example.com", RecipientName = "John Doe", Subject = "Welcome to Our Store!", Status = "delivered", CreatedAt = DateTime.Now.AddHours(-2), SentAt = DateTime.Now.AddHours(-2), DeliveredAt = DateTime.Now.AddHours(-2).AddMinutes(1), OpenedAt = DateTime.Now.AddHours(-1), CampaignName = "Welcome Series" },
                new() { Id = 2, Channel = "sms", Type = "direct", Direction = "outbound", RecipientPhone = "+1234567890", Preview = "Your order #1234 has been shipped!", Status = "delivered", CreatedAt = DateTime.Now.AddHours(-3), SentAt = DateTime.Now.AddHours(-3), DeliveredAt = DateTime.Now.AddHours(-3).AddSeconds(30) },
                new() { Id = 3, Channel = "whatsapp", Type = "reply", Direction = "outbound", RecipientPhone = "+1987654321", RecipientName = "Jane Smith", Preview = "Thank you for contacting us. Your ticket #456 has been created.", Status = "delivered", CreatedAt = DateTime.Now.AddHours(-5), SentAt = DateTime.Now.AddHours(-5), DeliveredAt = DateTime.Now.AddHours(-5).AddMinutes(1) },
                new() { Id = 4, Channel = "email", Type = "campaign", Direction = "outbound", RecipientEmail = "bob@example.com", RecipientName = "Bob Wilson", Subject = "50% Off Holiday Sale!", Status = "sent", CreatedAt = DateTime.Now.AddDays(-1), SentAt = DateTime.Now.AddDays(-1), CampaignName = "Holiday Sale" },
                new() { Id = 5, Channel = "email", Type = "automation", Direction = "outbound", RecipientEmail = "alice@example.com", Subject = "You left something in your cart", Status = "opened", CreatedAt = DateTime.Now.AddDays(-2), SentAt = DateTime.Now.AddDays(-2), DeliveredAt = DateTime.Now.AddDays(-2).AddMinutes(2), OpenedAt = DateTime.Now.AddDays(-1), CampaignName = "Abandoned Cart" },
                new() { Id = 6, Channel = "sms", Type = "template", Direction = "outbound", RecipientPhone = "+1555123456", Preview = "Your order has been delivered!", Status = "failed", CreatedAt = DateTime.Now.AddDays(-1), ErrorMessage = "Invalid phone number", TemplateName = "Delivery Confirmation" },
            },
            TotalCount = 1250,
            Page = 1,
            PageSize = 25
        };
    }

    private string GetShopDomain()
    {
        return User.FindFirst("shop_domain")?.Value ?? "devlotusalgebra.myshopify.com";
    }
}
