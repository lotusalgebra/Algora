namespace Algora.Web.Services;

public interface IAuthApiClient
{
    Task<AuthResponse?> LoginAsync(LoginRequest request);
    Task<AuthResponse?> RegisterAsync(RegisterRequest request);
    Task<AuthResponse?> RefreshTokenAsync(string refreshToken);
}

public record LoginRequest(string Email, string Password, string? ShopDomain = null);

public record RegisterRequest
{
    public required string ShopDomain { get; init; }
    public required string Email { get; init; }
    public required string Password { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string Role { get; init; } = "Admin";
}

public record AuthResponse
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public string? AccessToken { get; init; }
    public string? RefreshToken { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public UserInfo? User { get; init; }
}

public record UserInfo
{
    public int Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? ShopDomain { get; init; }
    public string? Role { get; init; }
}