namespace CachedAPI.Services;

public class CacheMissedException: Exception
{
    public CacheMissedException(string Key) 
        :base($"<<< Cache Missed on Key: {Key}") { }
}
