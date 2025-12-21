using Algora.Auth.Models;
using Algora.Domain.Entities;

namespace Algora.Auth.Services;

public interface IJwtService
{
    string GenerateAccessToken(AppUser user);
    string GenerateRefreshToken();
    int? ValidateToken(string token);
    (string AccessToken, string RefreshToken, DateTime ExpiresAt) GenerateTokenPair(AppUser user);
}