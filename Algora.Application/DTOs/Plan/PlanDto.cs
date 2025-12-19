namespace Algora.Application.DTOs.Plan
{
    /// <summary>
    /// Data transfer object for plan information.
    /// </summary>
    public record PlanDto
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public decimal MonthlyPrice { get; init; }
        public int OrderLimit { get; init; }
        public int ProductLimit { get; init; }
        public int CustomerLimit { get; init; }
        public bool HasWhatsApp { get; init; }
        public bool HasEmailCampaigns { get; init; }
        public bool HasSms { get; init; }
        public bool HasAdvancedReports { get; init; }
        public bool HasApiAccess { get; init; }
        public int TrialDays { get; init; }
        public bool IsCurrentPlan { get; init; }
    }
}
