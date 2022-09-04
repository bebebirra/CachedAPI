namespace CachedAPI.Services;

public class CacheDeserializationException: Exception
{
    public CacheDeserializationException(Exception e, string Key) 
        :base($"!!! Cache Deserialization failed on Key: {Key}", e) { }
}
