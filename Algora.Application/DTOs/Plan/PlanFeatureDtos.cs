namespace Algora.Application.DTOs.Plan
{
    /// <summary>
    /// Data transfer object for plan feature information.
    /// </summary>
    public record PlanFeatureDto
    {
        public int Id { get; init; }
        public string Code { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public string Category { get; init; } = string.Empty;
        public string? IconClass { get; init; }
        public int SortOrder { get; init; }
        public bool IsActive { get; init; }
        public DateTime CreatedAt { get; init; }
    }

    /// <summary>
    /// DTO for creating a new plan feature.
    /// </summary>
    public record CreatePlanFeatureDto
    {
        public string Code { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public string Category { get; init; } = string.Empty;
        public string? IconClass { get; init; }
        public int SortOrder { get; init; }
    }

    /// <summary>
    /// DTO for updating a plan feature.
    /// </summary>
    public record UpdatePlanFeatureDto
    {
        public string Name { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public string Category { get; init; } = string.Empty;
        public string? IconClass { get; init; }
        public int SortOrder { get; init; }
        public bool IsActive { get; init; }
    }

    /// <summary>
    /// DTO showing a plan with its assigned features.
    /// </summary>
    public record PlanWithFeaturesDto
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public decimal MonthlyPrice { get; init; }
        public int OrderLimit { get; init; }
        public int ProductLimit { get; init; }
        public int CustomerLimit { get; init; }
        public bool IsActive { get; init; }
        public List<PlanFeatureDto> Features { get; init; } = new();
    }

    /// <summary>
    /// DTO for assigning/unassigning features to a plan.
    /// </summary>
    public record AssignFeatureToPlanDto
    {
        public int PlanId { get; init; }
        public int FeatureId { get; init; }
        public string? AssignedBy { get; init; }
    }

    /// <summary>
    /// DTO for bulk feature assignment to a plan.
    /// </summary>
    public record BulkAssignFeaturesDto
    {
        public int PlanId { get; init; }
        public List<int> FeatureIds { get; init; } = new();
        public string? AssignedBy { get; init; }
    }
}
