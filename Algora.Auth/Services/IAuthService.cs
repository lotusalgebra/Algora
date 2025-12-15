using Algora.Auth.Models;

namespace Algora.Auth.Services;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> RefreshTokenAsync(string refreshToken);
    Task<AuthResponse> ChangePasswordAsync(int userId, ChangePasswordRequest request);
    Task<bool> RevokeTokenAsync(string refreshToken);
    Task<UserInfo?> GetUserByIdAsync(int userId);
}