using CachedAPI.Services;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//builder.Services.ConfigureICacheServiceInMemory();
builder.Services.ConfigureICacheServiceRedis(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

public static class ICacheHelper
{
    public static IServiceCollection ConfigureICacheServiceInMemory(
        this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddTransient<ICacheService, InMemoryCacheService>();

        return services;
    }

    public static IServiceCollection ConfigureICacheServiceRedis(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        IConnectionMultiplexer multiplexer;
        var _logger = services.BuildServiceProvider().GetService<ILogger<Program>>();

        var connectionString = configuration
            .GetValue<string>("RedisConnectionString");
        ConfigurationOptions redisOptions = new ConfigurationOptions
        {
            EndPoints = { connectionString },
            AbortOnConnectFail = false
        };

        multiplexer = ConnectionMultiplexer.Connect(redisOptions);

        services.AddSingleton<IConnectionMultiplexer>(multiplexer);
        services.AddTransient<ICacheService, RedisService>();

        //Configure InMemoryCache for runtime fallback
        services.AddMemoryCache();
        services.AddTransient<InMemoryCacheService>();

        _logger?.LogInformation("### Successfully initialised RedisCache!");

        return services;
    }
}
