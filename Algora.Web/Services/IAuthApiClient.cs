namespace Algora.Web.Services;

public interface IAuthApiClient
{
    Task<AuthResponse?> LoginAsync(LoginRequest request);
    Task<AuthResponse?> RegisterAsync(RegisterRequest request);
    Task<AuthResponse?> RefreshTokenAsync(string refreshToken);
}

public record LoginRequest(string Email, string Password);
public record RegisterRequest(string Email, string Password, string? Name);
public record AuthResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt);