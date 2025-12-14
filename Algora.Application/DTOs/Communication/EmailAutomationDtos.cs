namespace Algora.Application.DTOs.Communication;

public record EmailAutomationDto
{
    public int Id { get; init; }
    public string ShopDomain { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string TriggerType { get; init; } = string.Empty;
    public string? TriggerConditions { get; init; }
    public bool IsActive { get; init; }
    public int TotalEnrolled { get; init; }
    public int TotalCompleted { get; init; }
    public decimal? Revenue { get; init; }
    public DateTime CreatedAt { get; init; }
    public IEnumerable<EmailAutomationStepDto> Steps { get; init; } = [];
}

public record EmailAutomationStepDto
{
    public int Id { get; init; }
    public int StepOrder { get; init; }
    public string StepType { get; init; } = string.Empty;
    public string? Subject { get; init; }
    public string? Body { get; init; }
    public int? EmailTemplateId { get; init; }
    public int DelayMinutes { get; init; }
    public string? Conditions { get; init; }
    public bool IsActive { get; init; }
}

public record CreateEmailAutomationDto
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required string TriggerType { get; init; }
    public string? TriggerConditions { get; init; }
    public IEnumerable<CreateEmailAutomationStepDto> Steps { get; init; } = [];
}

public record CreateEmailAutomationStepDto
{
    public int StepOrder { get; init; }
    public string StepType { get; init; } = "email";
    public string? Subject { get; init; }
    public string? Body { get; init; }
    public int? EmailTemplateId { get; init; }
    public int DelayMinutes { get; init; }
    public string? Conditions { get; init; }
}

public record UpdateEmailAutomationDto
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? TriggerConditions { get; init; }
}