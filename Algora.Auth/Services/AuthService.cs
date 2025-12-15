using Algora.Auth.Models;
using Algora.Domain.Entities;
using Algora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Algora.Auth.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IJwtService _jwtService;
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        AppDbContext db,
        IJwtService jwtService,
        IOptions<JwtSettings> jwtSettings,
        ILogger<AuthService> logger)
    {
        _db = db;
        _jwtService = jwtService;
        _jwtSettings = jwtSettings.Value;
        _logger = logger;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var query = _db.AppUsers.AsQueryable();

        if (!string.IsNullOrEmpty(request.ShopDomain))
            query = query.Where(u => u.ShopDomain == request.ShopDomain);

        var user = await query.FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive);

        if (user is null)
        {
            return new AuthResponse { Success = false, Message = "Invalid email or password" };
        }

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return new AuthResponse { Success = false, Message = "Invalid email or password" };
        }

        var (accessToken, refreshToken, expiresAt) = _jwtService.GenerateTokenPair(user);

        // Store refresh token
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays);
        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        _logger.LogInformation("User {Email} logged in successfully", user.Email);

        return new AuthResponse
        {
            Success = true,
            Message = "Login successful",
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = expiresAt,
            User = MapToUserInfo(user)
        };
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        // Check if shop exists
        var shopExists = await _db.Shops.AnyAsync(s => s.Domain == request.ShopDomain);
        if (!shopExists)
        {
            return new AuthResponse { Success = false, Message = "Shop not found. Please install the app first." };
        }

        // Check if user already exists
        var existingUser = await _db.AppUsers
            .FirstOrDefaultAsync(u => u.ShopDomain == request.ShopDomain && u.Email == request.Email);

        if (existingUser is not null)
        {
            return new AuthResponse { Success = false, Message = "User with this email already exists" };
        }

        var user = new AppUser
        {
            ShopDomain = request.ShopDomain,
            Email = request.Email.ToLowerInvariant(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            Role = request.Role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.AppUsers.Add(user);
        await _db.SaveChangesAsync();

        var (accessToken, refreshToken, expiresAt) = _jwtService.GenerateTokenPair(user);

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays);
        await _db.SaveChangesAsync();

        _logger.LogInformation("User {Email} registered successfully for shop {Shop}", user.Email, request.ShopDomain);

        return new AuthResponse
        {
            Success = true,
            Message = "Registration successful",
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = expiresAt,
            User = MapToUserInfo(user)
        };
    }

    public async Task<AuthResponse> RefreshTokenAsync(string refreshToken)
    {
        var user = await _db.AppUsers
            .FirstOrDefaultAsync(u => u.RefreshToken == refreshToken && u.IsActive);

        if (user is null || user.RefreshTokenExpiresAt < DateTime.UtcNow)
        {
            return new AuthResponse { Success = false, Message = "Invalid or expired refresh token" };
        }

        var (newAccessToken, newRefreshToken, expiresAt) = _jwtService.GenerateTokenPair(user);

        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays);
        await _db.SaveChangesAsync();

        return new AuthResponse
        {
            Success = true,
            Message = "Token refreshed successfully",
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            ExpiresAt = expiresAt,
            User = MapToUserInfo(user)
        };
    }

    public async Task<AuthResponse> ChangePasswordAsync(int userId, ChangePasswordRequest request)
    {
        var user = await _db.AppUsers.FindAsync(userId);
        if (user is null)
        {
            return new AuthResponse { Success = false, Message = "User not found" };
        }

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
        {
            return new AuthResponse { Success = false, Message = "Current password is incorrect" };
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        _logger.LogInformation("User {Email} changed password", user.Email);

        return new AuthResponse { Success = true, Message = "Password changed successfully" };
    }

    public async Task<bool> RevokeTokenAsync(string refreshToken)
    {
        var user = await _db.AppUsers.FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);
        if (user is null) return false;

        user.RefreshToken = null;
        user.RefreshTokenExpiresAt = null;
        await _db.SaveChangesAsync();

        return true;
    }

    public async Task<UserInfo?> GetUserByIdAsync(int userId)
    {
        var user = await _db.AppUsers.FindAsync(userId);
        return user is null ? null : MapToUserInfo(user);
    }

    private static UserInfo MapToUserInfo(AppUser user) => new()
    {
        Id = user.Id,
        Email = user.Email,
        FirstName = user.FirstName,
        LastName = user.LastName,
        ShopDomain = user.ShopDomain,
        Role = user.Role
    };
}