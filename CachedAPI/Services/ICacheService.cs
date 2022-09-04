namespace CachedAPI.Services;

public interface ICacheService
{
    public const int ExpirationMinutes = 1;
    public Task<T> GetCacheValueAsync<T>(
        string Key, 
        CancellationToken cancellationToken = default);
    public Task SetCacheValueAsync<T>(
        string Key, 
        T Value, 
        CancellationToken cancellationToken = default) 
        where T : class;
}
