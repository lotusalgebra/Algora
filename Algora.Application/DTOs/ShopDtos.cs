namespace Algora.Application.DTOs;

public record ShopDto
{
    public Guid Id { get; init; }
    public string Domain { get; init; } = string.Empty;
    public string? ShopName { get; init; }
    public string? Email { get; init; }
    public string? PrimaryLocale { get; init; }
    public string? Timezone { get; init; }
    public string? Currency { get; init; }
    public string? Country { get; init; }
    public string? PlanName { get; init; }
    public bool IsActive { get; init; }
    public bool UseCustomCredentials { get; init; }
    public bool HasCustomCredentials { get; init; }
    public DateTime InstalledAt { get; init; }
    public DateTime? LastSyncedAt { get; init; }
}

public record ShopCredentialsDto
{
    public string ApiKey { get; init; } = string.Empty;
    public string ApiSecret { get; init; } = string.Empty;
    public string Scopes { get; init; } = string.Empty;
    public string AppUrl { get; init; } = string.Empty;
    public bool IsCustom { get; init; }
}

public record UpdateShopCredentialsDto
{
    public string? ApiKey { get; init; }
    public string? ApiSecret { get; init; }
    public string? Scopes { get; init; }
    public string? AppUrl { get; init; }
    public bool UseCustomCredentials { get; init; }
}

public record UpdateShopInfoDto
{
    public string? ShopName { get; init; }
    public string? Email { get; init; }
    public string? PrimaryLocale { get; init; }
    public string? Timezone { get; init; }
    public string? Currency { get; init; }
    public string? Country { get; init; }
    public string? PlanName { get; init; }
}