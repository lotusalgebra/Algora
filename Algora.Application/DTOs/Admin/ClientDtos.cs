namespace Algora.Application.DTOs.Admin
{
    /// <summary>
    /// Data transfer object for client/shop information in admin view.
    /// </summary>
    public record ClientDto
    {
        public Guid Id { get; init; }
        public string Domain { get; init; } = string.Empty;
        public string? ShopName { get; init; }
        public string? Email { get; init; }
        public string? Country { get; init; }
        public string? Currency { get; init; }
        public bool IsActive { get; init; }
        public DateTime InstalledAt { get; init; }
        public DateTime? LastSyncedAt { get; init; }

        // License/Plan info
        public string? PlanName { get; init; }
        public string? LicenseStatus { get; init; }
        public DateTime? LicenseExpiry { get; init; }
        public bool HasActiveLicense { get; init; }
    }

    /// <summary>
    /// Detailed client view with full license and feature information.
    /// </summary>
    public record ClientDetailDto
    {
        public Guid Id { get; init; }
        public string Domain { get; init; } = string.Empty;
        public string? ShopName { get; init; }
        public string? Email { get; init; }
        public string? Country { get; init; }
        public string? Currency { get; init; }
        public string? Timezone { get; init; }
        public string? PrimaryLocale { get; init; }
        public bool IsActive { get; init; }
        public DateTime InstalledAt { get; init; }
        public DateTime? LastSyncedAt { get; init; }
        public bool UseCustomCredentials { get; init; }

        // License info
        public string? PlanName { get; init; }
        public string? LicenseStatus { get; init; }
        public DateTime? LicenseStartDate { get; init; }
        public DateTime? LicenseExpiry { get; init; }
        public bool HasActiveLicense { get; init; }

        // Plan details
        public decimal? PlanPrice { get; init; }
        public int? OrderLimit { get; init; }
        public int? ProductLimit { get; init; }
        public int? CustomerLimit { get; init; }

        // Assigned features
        public List<string> Features { get; init; } = new();
    }

    /// <summary>
    /// DTO for updating a client's plan.
    /// </summary>
    public record UpdateClientPlanDto
    {
        public string ShopDomain { get; init; } = string.Empty;
        public string NewPlanName { get; init; } = string.Empty;
        public string? AdminNotes { get; init; }
    }

    /// <summary>
    /// Filter options for client list.
    /// </summary>
    public record ClientFilterDto
    {
        public string? SearchTerm { get; init; }
        public string? PlanName { get; init; }
        public string? LicenseStatus { get; init; }
        public bool? IsActive { get; init; }
        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = 25;
    }

    /// <summary>
    /// Paginated result for client list.
    /// </summary>
    public record ClientListResultDto
    {
        public List<ClientDto> Clients { get; init; } = new();
        public int TotalCount { get; init; }
        public int Page { get; init; }
        public int PageSize { get; init; }
        public int TotalPages { get; init; }
    }
}
