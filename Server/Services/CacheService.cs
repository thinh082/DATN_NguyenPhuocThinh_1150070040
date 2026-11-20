using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Server.Services;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan ttl);
    Task RemoveAsync(string key);
    Task RemoveByPrefixAsync(string prefix);
}

public class CacheService : ICacheService
{
    private readonly IConnectionMultiplexer? _redis;
    private readonly ILogger<CacheService> _logger;

    public CacheService(IServiceProvider serviceProvider, ILogger<CacheService> logger)
    {
        _redis = serviceProvider.GetService<IConnectionMultiplexer>();
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        if (_redis == null || !_redis.IsConnected) return default;
        try
        {
            var db = _redis.GetDatabase();
            var val = await db.StringGetAsync(key);
            if (val.IsNullOrEmpty) return default;
            return JsonSerializer.Deserialize<T>(val!);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Loi khi doc cache Redis cho key {Key}. Fallback ve CSDL.", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan ttl)
    {
        if (_redis == null || !_redis.IsConnected) return;
        try
        {
            var db = _redis.GetDatabase();
            var json = JsonSerializer.Serialize(value);
            await db.StringSetAsync(key, json, ttl);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Loi khi ghi cache Redis cho key {Key}.", key);
        }
    }

    public async Task RemoveAsync(string key)
    {
        if (_redis == null || !_redis.IsConnected) return;
        try
        {
            var db = _redis.GetDatabase();
            await db.KeyDeleteAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Loi khi xoa cache Redis cho key {Key}.", key);
        }
    }

    public async Task RemoveByPrefixAsync(string prefix)
    {
        if (_redis == null || !_redis.IsConnected) return;
        try
        {
            var endpoints = _redis.GetEndPoints();
            foreach (var ep in endpoints)
            {
                var server = _redis.GetServer(ep);
                if (server.IsConnected)
                {
                    var keys = server.Keys(pattern: $"{prefix}*").ToArray();
                    if (keys.Length > 0)
                    {
                        var db = _redis.GetDatabase();
                        await db.KeyDeleteAsync(keys);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Loi khi xoa cache theo prefix {Prefix}.", prefix);
        }
    }
}
