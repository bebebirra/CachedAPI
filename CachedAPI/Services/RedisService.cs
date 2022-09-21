using StackExchange.Redis;
using System.Text.Json;

namespace CachedAPI.Services;

public class RedisService : ICacheService, IDisposable
{
    private readonly IDatabase db;
    private readonly IConnectionMultiplexer connectionMultiplexer;

    private readonly ILogger<RedisService> logger;
    private readonly InMemoryCacheService inMemoryCacheService;
    private bool IsFallbackEnabled = false;

    public RedisService(
        IConnectionMultiplexer connectionMultiplexer,
        InMemoryCacheService inMemoryCacheService,
        ILogger<RedisService> logger)
    {
        this.connectionMultiplexer = connectionMultiplexer;
        db = connectionMultiplexer.GetDatabase();
        this.inMemoryCacheService = inMemoryCacheService;
        this.logger = logger;

        IsFallbackEnabled = !this.connectionMultiplexer.IsConnected;

        this.connectionMultiplexer.ConnectionFailed += FailedConnection;
        this.connectionMultiplexer.ConnectionRestored += RestoredConnection;
    }

    private void RestoredConnection(object? sender, ConnectionFailedEventArgs e)
    {
        IsFallbackEnabled = false;
    }

    private void FailedConnection(object? sender, ConnectionFailedEventArgs e)
    {
        IsFallbackEnabled = true;
    }

    public async Task<T> GetCacheValueAsync<T>(
        string Key,
        CancellationToken cancellationToken = default)
    {
        if (IsFallbackEnabled)
        {
            return await inMemoryCacheService
                .GetCacheValueAsync<T>(Key, cancellationToken);
        }

        RedisValue redisValue = new();

        try
        {
            redisValue = await db.StringGetAsync(Key);
        }
        catch (Exception e)
        {
            logger.LogError(e, "### Redis Cache unreachable !!!");
            IsFallbackEnabled = true;
            return default;
        }

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
        if (IsFallbackEnabled)
        {
            await inMemoryCacheService
                .SetCacheValueAsync<T>(
                    Key,
                    Value,
                    cancellationToken);
            return;
        }


        if (string.IsNullOrEmpty(Key))
        {
            throw new ArgumentNullException(nameof(Key));
        }

        if (Value is null)
        {
            throw new ArgumentNullException(nameof(Value));
        }

        var json = JsonSerializer.Serialize(Value);
        try
        {
            await db.StringSetAsync(
                Key,
                json,
                TimeSpan.FromMinutes(ICacheService.ExpirationMinutes));

            logger.LogInformation(">>> Cache set for Key:{Key}", Key);
        }
        catch (Exception e)
        {
            logger.LogError(e, "### Redis Cache unreachable !!!");
            IsFallbackEnabled = true;
            await inMemoryCacheService.SetCacheValueAsync(
                Key,
                Value,
                cancellationToken);
        }
    }

    public void Dispose()
    {
        connectionMultiplexer.ConnectionFailed -= FailedConnection;
        connectionMultiplexer.ConnectionRestored -= RestoredConnection;
    }
}
