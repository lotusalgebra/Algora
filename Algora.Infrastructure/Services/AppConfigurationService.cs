using Algora.Application.Interfaces;
using Algora.Domain.Entities;
using Algora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Algora.Infrastructure.Services;

public class AppConfigurationService : IAppConfigurationService
{
    private readonly AppDbContext _db;
    private readonly IMemoryCache _cache;
    private const string CachePrefix = "AppConfig_";

    public AppConfigurationService(AppDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<string?> GetValueAsync(string key)
    {
        var cacheKey = $"{CachePrefix}{key}";
        
        if (_cache.TryGetValue(cacheKey, out string? cachedValue))
            return cachedValue;

        var config = await _db.AppConfigurations
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Key == key);

        var value = config?.Value;
        
        _cache.Set(cacheKey, value, TimeSpan.FromMinutes(5));
        
        return value;
    }

    public async Task SetValueAsync(string key, string value, string? description = null)
    {
        var config = await _db.AppConfigurations.FirstOrDefaultAsync(c => c.Key == key);

        if (config is null)
        {
            config = new AppConfiguration
            {
                Key = key,
                Value = value,
                Description = description
            };
            _db.AppConfigurations.Add(config);
        }
        else
        {
            config.Value = value;
            config.UpdatedAt = DateTime.UtcNow;
            if (description is not null)
                config.Description = description;
        }

        await _db.SaveChangesAsync();
        
        _cache.Remove($"{CachePrefix}{key}");
    }

    public async Task<Dictionary<string, string?>> GetAllAsync()
    {
        return await _db.AppConfigurations
            .AsNoTracking()
            .ToDictionaryAsync(c => c.Key, c => c.Value);
    }
}