namespace Algora.Application.Interfaces;

public interface IAppConfigurationService
{
    Task<string?> GetValueAsync(string key);
    Task SetValueAsync(string key, string value, string? description = null);
    Task<Dictionary<string, string?>> GetAllAsync();
}