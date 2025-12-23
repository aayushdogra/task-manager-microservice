using System.Text.Json;
using StackExchange.Redis;

namespace TaskManager.Services;

public class RedisCacheService(IConnectionMultiplexer redis, ILogger<RedisCacheService> logger): ICacheService
{
    private readonly IDatabase _db = redis.GetDatabase();
    private readonly IConnectionMultiplexer _redis = redis;
    private readonly ILogger<RedisCacheService> _logger = logger;

    public async Task<T?> GetAsync<T>(string key)
    {
        try
        {
            var value = await _db.StringGetAsync(key);

            if (!value.HasValue)
                return default;

            return JsonSerializer.Deserialize<T>(value!.ToString());
        }
        catch(Exception ex)
        {
            _logger.LogWarning(ex, "Redis GET failed for key {Key}", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan ttl)
    {
        try
        {
            var json = JsonSerializer.Serialize(value);
            await _db.StringSetAsync(key, json, ttl);
        }
        catch(Exception ex)
        {
            _logger.LogWarning(ex, "Redis SET failed for key {Key}", key);
        }
    }

    public async Task RemoveByPatternAsync(string pattern)
    {
        try
        {
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            var keys = server.Keys(pattern: pattern).ToArray();

            foreach(var key in keys)
                await _db.KeyDeleteAsync(key);

            _logger.LogInformation($"Cache invalidated for pattern {pattern}");
        }
        catch(Exception ex)
        {
            _logger.LogWarning(ex, $"Cache invalidation failed for pattern {pattern}");
        }
    }
}