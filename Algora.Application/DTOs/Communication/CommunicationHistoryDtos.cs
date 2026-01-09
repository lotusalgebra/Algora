namespace Algora.Application.DTOs.Communication;

/// <summary>
/// Unified communication history item for display.
/// </summary>
public class CommunicationHistoryItemDto
{
    public int Id { get; set; }
    public string Channel { get; set; } = string.Empty; // email, sms, whatsapp
    public string Type { get; set; } = string.Empty; // campaign, automation, direct, reply
    public string Direction { get; set; } = "outbound"; // inbound, outbound
    public string? RecipientEmail { get; set; }
    public string? RecipientPhone { get; set; }
    public string? RecipientName { get; set; }
    public string? Subject { get; set; }
    public string? Preview { get; set; }
    public string? Body { get; set; } // Full message body for details view
    public string Status { get; set; } = string.Empty; // pending, sent, delivered, opened, clicked, failed, bounced
    public DateTime CreatedAt { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? OpenedAt { get; set; }
    public DateTime? ClickedAt { get; set; }
    public string? CampaignName { get; set; }
    public string? TemplateName { get; set; }
    public string? ErrorMessage { get; set; }
    public int? RelatedId { get; set; } // Campaign ID, Automation ID, etc.
}

/// <summary>
/// Filter options for communication history.
/// </summary>
public class CommunicationHistoryFilterDto
{
    public string? Channel { get; set; } // email, sms, whatsapp, all
    public string? Status { get; set; } // sent, delivered, failed, all
    public string? Type { get; set; } // campaign, automation, direct, all
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? Search { get; set; } // Search by recipient or subject
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    public string SortBy { get; set; } = "CreatedAt";
    public bool SortDescending { get; set; } = true;
}

/// <summary>
/// Paginated result for communication history.
/// </summary>
public class CommunicationHistoryResultDto
{
    public IEnumerable<CommunicationHistoryItemDto> Items { get; set; } = new List<CommunicationHistoryItemDto>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

/// <summary>
/// Communication statistics summary.
/// </summary>
public class CommunicationStatsDto
{
    public int TotalSent { get; set; }
    public int TotalDelivered { get; set; }
    public int TotalFailed { get; set; }
    public int TotalOpened { get; set; }
    public int TotalClicked { get; set; }
    public int EmailCount { get; set; }
    public int SmsCount { get; set; }
    public int WhatsAppCount { get; set; }
    public decimal DeliveryRate => TotalSent > 0 ? (decimal)TotalDelivered / TotalSent * 100 : 0;
    public decimal OpenRate => TotalDelivered > 0 ? (decimal)TotalOpened / TotalDelivered * 100 : 0;
    public decimal ClickRate => TotalOpened > 0 ? (decimal)TotalClicked / TotalOpened * 100 : 0;
}
