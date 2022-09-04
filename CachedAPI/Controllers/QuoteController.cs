using Microsoft.AspNetCore.Mvc;

using Jeffsum;
using static Jeffsum.Goldblum;
using CachedAPI.Services;

namespace CachedAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class QuoteController: ControllerBase
{
    private readonly ILogger<QuoteController> _logger;
	private ICacheService _cache;
	public QuoteController(
		ILogger<QuoteController> logger,
        ICacheService cache)
	{
		_logger = logger;
		_cache = cache;
	}

	[HttpGet]
	public async Task<IEnumerable<string>> GetQuotes()
	{
		IEnumerable<string>? quotes = null;

		try
		{
			quotes = await _cache
				.GetCacheValueAsync<IEnumerable<string>?>(
					nameof(QuoteController));
		}
		catch(CacheMissedException ce)
		{
			_logger.LogInformation("{message}", ce.Message);
		}
		
		quotes ??= await GetNewQuotes();

		return quotes;

		async Task<IEnumerable<string>> GetNewQuotes()
		{
			var count = 1 + Random.Shared.Next(4);

			var quotes = ReceiveTheJeff(
				count,
				JeffsumType.Quotes).ToList();

			await Task.Delay(500);

			_logger.LogInformation(
				$"=== {nameof(Jeffsum.Goldblum)}:: New set of quotes provided!!");

			await _cache.SetCacheValueAsync(
				nameof(QuoteController),
				quotes);

			return quotes;
		}
	}
}
