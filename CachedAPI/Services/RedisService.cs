using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;

namespace CachedAPI.Services;

public class RedisService : ICacheService
{
    private readonly IDatabase db;

    private readonly ILogger<RedisService> logger;

    public RedisService(
        IConnectionMultiplexer connectionMultiplexer,
        ILogger<RedisService> logger)
    {
        db = connectionMultiplexer.GetDatabase();
        this.logger = logger;
    }

    public async Task<T> GetCacheValueAsync<T>(
        string Key, 
        CancellationToken cancellationToken = default)
    {
        var redisValue = await db.StringGetAsync(Key);

        if (redisValue.IsNullOrEmpty)
        {
            throw new CacheMissedException(Key);
        }

        T result;
        try
        {
            var json = redisValue.ToString();
            result = JsonSerializer.Deserialize<T>(json);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deserializing response");
            throw new CacheDeserializationException(ex, Key);
        }

        logger.LogInformation(">>> Cache Hit on Key:{Key}", Key);
        return result;
    }

    public async Task SetCacheValueAsync<T>(
        string Key, 
        T Value, CancellationToken cancellationToken = default) where T : class
    {
        if (string.IsNullOrEmpty(Key))
        {
            throw new ArgumentNullException(nameof(Key));
        }

        if (Value is null)
        {
            throw new ArgumentNullException(nameof(Value));
        }

        var json = JsonSerializer.Serialize(Value);
        
        await db.StringSetAsync(
            Key, 
            json,
            TimeSpan.FromMinutes(ICacheService.ExpirationMinutes));

        logger.LogInformation(">>> Cache set for Key:{Key}", Key);
    }
}
