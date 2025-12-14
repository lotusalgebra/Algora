namespace Algora.Application.DTOs.Communication;

public record EmailCampaignDto
{
    public int Id { get; init; }
    public string ShopDomain { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public string? PreviewText { get; init; }
    public string? FromName { get; init; }
    public string? FromEmail { get; init; }
    public string CampaignType { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public int? SegmentId { get; init; }
    public string? SegmentName { get; init; }
    public DateTime? ScheduledAt { get; init; }
    public DateTime? SentAt { get; init; }
    public int TotalRecipients { get; init; }
    public int TotalSent { get; init; }
    public int TotalDelivered { get; init; }
    public int TotalOpened { get; init; }
    public int TotalClicked { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record CreateEmailCampaignDto
{
    public required string Name { get; init; }
    public required string Subject { get; init; }
    public string? PreviewText { get; init; }
    public required string Body { get; init; }
    public string? FromName { get; init; }
    public string? FromEmail { get; init; }
    public string CampaignType { get; init; } = "regular";
    public int? EmailTemplateId { get; init; }
    public int? SegmentId { get; init; }
    public int? ListId { get; init; }
}

public record UpdateEmailCampaignDto
{
    public string? Name { get; init; }
    public string? Subject { get; init; }
    public string? PreviewText { get; init; }
    public string? Body { get; init; }
    public string? FromName { get; init; }
    public string? FromEmail { get; init; }
    public int? SegmentId { get; init; }
}

public record EmailCampaignStatsDto
{
    public int CampaignId { get; init; }
    public int TotalRecipients { get; init; }
    public int TotalSent { get; init; }
    public int TotalDelivered { get; init; }
    public int TotalOpened { get; init; }
    public int TotalClicked { get; init; }
    public int TotalBounced { get; init; }
    public int TotalUnsubscribed { get; init; }
    public decimal OpenRate { get; init; }
    public decimal ClickRate { get; init; }
    public decimal BounceRate { get; init; }
}