using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace TodoApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

        private readonly ILogger<WeatherForecastController> _logger;
        private readonly IDistributedCache _distributedCache;
        private readonly string WeatherForecastKey = "WeatherForecast";

        public WeatherForecastController(ILogger<WeatherForecastController> logger, IDistributedCache distributedCache)
        {
            _logger = logger;
            _distributedCache = distributedCache;
        }
        [HttpGet(Name = "GetWeatherForecast")]
        public async Task<IEnumerable<WeatherForecast>> Get()
        {
            try
            {
                // Find cached item
                byte[] objectFromCache = await _distributedCache.GetAsync(WeatherForecastKey);

                if (objectFromCache != null)
                {
                    // Deserialize it
                    var jsonToDeserialize = System.Text.Encoding.UTF8.GetString(objectFromCache);
                    var cachedResult = JsonSerializer.Deserialize<IEnumerable<WeatherForecast>>(jsonToDeserialize);
                    if (cachedResult != null)
                    {
                        HttpContext.Response.Cookies.Append(WeatherForecastKey, jsonToDeserialize);
                        // If found, then return it
                        return cachedResult;
                    }
                }

                // If not found, then recalculate response
                var result = Enumerable.Range(1, 5).Select(index => new WeatherForecast
                {
                    Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    TemperatureC = Random.Shared.Next(-20, 55),
                    Summary = Summaries[Random.Shared.Next(Summaries.Length)]
                })
                .ToArray();

                // Serialize the response
                byte[] objectToCache = JsonSerializer.SerializeToUtf8Bytes(result);


                // Cache it
                await _distributedCache.SetAsync(WeatherForecastKey, objectToCache);
                //await _distributedCache.SetAsync(WeatherForecastKey, objectToCache, cacheEntryOptions);
                //HttpContext.Session.Set(WeatherForecastKey, objectToCache);
                

                return result;
            }
            catch (Exception ex)
            {
                System.IO.File.WriteAllText(@"C:\source\repos\log\ControllerLog.log", ex.ToString());
                throw ex;
            }
        }
    }
}