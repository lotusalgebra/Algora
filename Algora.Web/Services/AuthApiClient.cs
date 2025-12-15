using System.Net.Http.Json;
using Algora.Application.Interfaces;

namespace Algora.Web.Services;

public class AuthApiClient : IAuthApiClient
{
    private readonly HttpClient _httpClient;
    private readonly IAppConfigurationService _configService;
    private readonly ILogger<AuthApiClient> _logger;

    public AuthApiClient(
        HttpClient httpClient, 
        IAppConfigurationService configService,
        ILogger<AuthApiClient> logger)
    {
        _httpClient = httpClient;
        _configService = configService;
        _logger = logger;
    }

    private async Task<string> GetBaseUrlAsync()
    {
        var baseUrl = await _configService.GetValueAsync("AuthService:BaseUrl");
        return baseUrl ?? throw new InvalidOperationException("AuthService:BaseUrl not configured in database");
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        try
        {
            var baseUrl = await GetBaseUrlAsync();
            _httpClient.BaseAddress = new Uri(baseUrl);
            
            var response = await _httpClient.PostAsJsonAsync("api/auth/login", request);
            response.EnsureSuccessStatusCode();
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
            var baseUrl = await GetBaseUrlAsync();
            _httpClient.BaseAddress = new Uri(baseUrl);
            
            var response = await _httpClient.PostAsJsonAsync("api/auth/register", request);
            response.EnsureSuccessStatusCode();
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
            var baseUrl = await GetBaseUrlAsync();
            _httpClient.BaseAddress = new Uri(baseUrl);
            
            var response = await _httpClient.PostAsJsonAsync("api/auth/refresh", new { RefreshToken = refreshToken });
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<AuthResponse>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh token via Auth microservice");
            return null;
        }
    }
}