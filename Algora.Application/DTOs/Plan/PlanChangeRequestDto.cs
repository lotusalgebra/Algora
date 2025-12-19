namespace Algora.Application.DTOs.Plan
{
    /// <summary>
    /// Data transfer object for plan change requests.
    /// </summary>
    public record PlanChangeRequestDto
    {
        public int Id { get; init; }
        public string ShopDomain { get; init; } = string.Empty;
        public string CurrentPlanName { get; init; } = string.Empty;
        public string RequestedPlanName { get; init; } = string.Empty;
        public string RequestType { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public string? AdminNotes { get; init; }
        public DateTime RequestedAt { get; init; }
        public DateTime? ProcessedAt { get; init; }
        public string? ProcessedBy { get; init; }
    }
}
