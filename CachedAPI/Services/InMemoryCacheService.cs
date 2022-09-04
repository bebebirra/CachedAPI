using Microsoft.Extensions.Caching.Memory;

namespace CachedAPI.Services;

public class InMemoryCacheService : ICacheService
{
    private readonly ILogger<InMemoryCacheService> logger;
    private readonly IMemoryCache cache;

    public InMemoryCacheService(
        IMemoryCache cache,
        ILogger<InMemoryCacheService> logger)
    {
        this.cache = cache;
        this.logger = logger;
    }
    public Task<T> GetCacheValueAsync<T>(
        string Key,
        CancellationToken cancellationToken = default)
    {
        var result = cache.Get<T>(Key);

        if (result is null)
        {
            throw new CacheMissedException(Key);
        }
        else
        {
            logger.LogInformation(">>> Cache Hit on Key:{Key}", Key);
        }

        return Task.FromResult(result);
    }


    public Task SetCacheValueAsync<T>(
        string Key, 
        T Value, 
        CancellationToken cancellationToken = default) where T : class
    {
        cache.Set<T>(Key,
            Value,
            new MemoryCacheEntryOptions
            {
                AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(
                    ICacheService.ExpirationMinutes),
            });

        logger.LogInformation(">>> Cache set for Key:{Key}", Key);

        return Task.CompletedTask;
    }
}
