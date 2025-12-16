using System.Net.Http.Json;
using Algora.Application.Interfaces;

namespace Algora.Web.Services;

public class AuthApiClient : IAuthApiClient
{
    private readonly HttpClient _httpClient;
    private readonly IAppConfigurationService _configService;
    private readonly ILogger<AuthApiClient> _logger;
    private string? _cachedBaseUrl;

    public AuthApiClient(
        HttpClient httpClient,
        IAppConfigurationService configService,
        ILogger<AuthApiClient> logger)
    {
        _httpClient = httpClient;
        _configService = configService;
        _logger = logger;
    }

    private async Task EnsureBaseUrlAsync()
    {
        if (_httpClient.BaseAddress is null)
        {
            _cachedBaseUrl ??= await _configService.GetValueAsync("AuthService:BaseUrl")
                ?? throw new InvalidOperationException("AuthService:BaseUrl not configured in database");
            _httpClient.BaseAddress = new Uri(_cachedBaseUrl);
        }
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        try
        {
            await EnsureBaseUrlAsync();
            var response = await _httpClient.PostAsJsonAsync("api/auth/login", request);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Login failed: {StatusCode} - {Error}", response.StatusCode, errorContent);
                return null;
            }
            
            return await response.Content.ReadFromJsonAsync<AuthResponse>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to login via Auth microservice");
            return null;
        }
    }

    public async Task<AuthResponse?> RegisterAsync(RegisterRequest request)
    {
        try
        {
            await EnsureBaseUrlAsync();
            var response = await _httpClient.PostAsJsonAsync("api/auth/register", request);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Registration failed: {StatusCode} - {Error}", response.StatusCode, errorContent);
                return null;
            }
            
            return await response.Content.ReadFromJsonAsync<AuthResponse>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register via Auth microservice");
            return null;
        }
    }

    public async Task<AuthResponse?> RefreshTokenAsync(string refreshToken)
    {
        try
        {
            await EnsureBaseUrlAsync();
            var response = await _httpClient.PostAsJsonAsync("api/auth/refresh", new { RefreshToken = refreshToken });
            
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }
            
            return await response.Content.ReadFromJsonAsync<AuthResponse>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh token via Auth microservice");
            return null;
        }
    }
}