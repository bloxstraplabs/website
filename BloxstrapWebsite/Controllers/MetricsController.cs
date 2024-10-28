using BloxstrapWebsite.Data;
using BloxstrapWebsite.Enums;
using BloxstrapWebsite.Models;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;

using System.Text.Json;
using System.Text.RegularExpressions;

namespace BloxstrapWebsite.Controllers
{
    public partial class MetricsController : Controller
    {
        private readonly IInfluxDBClient _influxDBClient;
        private readonly IMemoryCache _memoryCache;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<MetricsController> _logger;

        private readonly List<StatPoint> _statPoints = 
        [
            new StatPoint
            {
                Name = "installAction",
                Values = ["install", "upgrade", "uninstall"],
                ProductionOnly = false,
                RatelimitInterval = 3600,
                RatelimitType = RatelimitType.KeyValue
            },

            new StatPoint
            {
                Name = "robloxChannel",
                ProductionOnly = false,
                Bucket = "bloxstrap-eph-7d",
                RatelimitInterval = 3600
            }
        ];

        private readonly List<string> _uaTypes = ["Production", "Artifact", "Build"];

        [GeneratedRegex(@"Bloxstrap\/([0-9\.]+) \((Production|Build [a-zA-Z0-9=+\/]+|Artifact [0-9a-f]{40}, [a-zA-Z0-9\/\-]+)\)")]
        private static partial Regex UARegex();

        public MetricsController(IInfluxDBClient influxDBClient, IMemoryCache memoryCache, 
            IHttpClientFactory httpClientFactory, ApplicationDbContext dbContext,
            ILogger<MetricsController> logger)
        {
            _influxDBClient = influxDBClient;
            _memoryCache = memoryCache;
            _httpClientFactory = httpClientFactory;
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<IActionResult> Post(string? key, string? value)
        {
            if (String.IsNullOrEmpty(key) || String.IsNullOrEmpty(value))
                return BadRequest();

            var uaDetails = ValidateUA();

            if (uaDetails is null)
                return BadRequest();

            var statPoint = _statPoints.Find(x => x.Name == key);

            // validate stats key/value
            if (statPoint is null || statPoint.Values is not null && !statPoint.Values.Contains(value))
                return BadRequest();

            string info = uaDetails.Groups[2].Value;

            if (statPoint.ProductionOnly && info != "Production")
                return BadRequest();

            if (statPoint.Name == "robloxChannel")
            {
                value = value.ToLowerInvariant();

                if (value[0] != 'z')
                    return BadRequest();
            }

            // ratelimit
#if !DEBUG
            var requestIp = Request.HttpContext.Connection.RemoteIpAddress;

            if (requestIp is null)
                return StatusCode(500);

            string cacheKey = $"ratelimit-metrics-{key}-{requestIp}";

            if (statPoint.RatelimitType == RatelimitType.KeyValue)
                cacheKey += $"-{value}";

            _memoryCache.TryGetValue(cacheKey, out int count);

            if (count >= statPoint.RatelimitCount)
                return StatusCode(429);
            
            if (count == 0)
                _memoryCache.Set(cacheKey, ++count, DateTime.Now.AddSeconds(statPoint.RatelimitInterval));
            else
                _memoryCache.Set(cacheKey, ++count);
#endif

            if (statPoint.Name == "robloxChannel")
            {
                string validationCacheKey = $"validation-channel-{value}";

                if (!_memoryCache.TryGetValue(validationCacheKey, out bool exists))
                {
                    var httpClient = _httpClientFactory.CreateClient("Global");

                    var response = await httpClient.GetFromJsonAsync<JsonDocument>($"https://clientsettings.roblox.com/v2/settings/application/PCClientBootstrapper/bucket/{value}");
                    
                    exists = response!.RootElement.GetProperty("applicationSettings").ValueKind != JsonValueKind.Null;

                    _memoryCache.Set(validationCacheKey, exists, DateTime.Now.AddDays(1));
                }

                if (!exists)
                    return BadRequest();
            }

            var point = PointData.Measurement("metrics")
                    .Field(key, value)
                    .Tag("version", uaDetails.Value)
                    .Timestamp(DateTime.UtcNow, WritePrecision.Ns);

            await _influxDBClient.GetWriteApiAsync().WritePointAsync(point, statPoint.Bucket, "pizza-server");

            return Ok();
        }

        [HttpPost]
        [ActionName("post-exception")]
        public async Task<IActionResult> PostException()
        {
            if (ValidateUA() is null)
                return BadRequest();

            using var sr = new StreamReader(Request.Body);

            string trace = await sr.ReadToEndAsync();

            if (String.IsNullOrEmpty(trace))
                return BadRequest();

            if (trace.Length >= 1024 * 50)
            {
                _logger.LogInformation("Exception post dropped because it was too big ({Bytes} bytes)", trace.Length);
                return BadRequest();
            }

            // ratelimit
#if !DEBUG
            var requestIp = Request.HttpContext.Connection.RemoteIpAddress;

            if (requestIp is null)
                return StatusCode(500);

            string cacheKey = $"ratelimit-metrics-exception-{requestIp}";

            _memoryCache.TryGetValue(cacheKey, out int count);

            if (count >= 1)
                return StatusCode(429);
            
            if (count == 0)
                _memoryCache.Set(cacheKey, ++count, DateTime.Now.AddHours(1));
            else
                _memoryCache.Set(cacheKey, ++count);
#endif

            await _dbContext.ExceptionReports.AddAsync(new()
            {
                Timestamp = DateTime.UtcNow,
                Trace = trace
            });

            await _dbContext.SaveChangesAsync();

            return Ok();
        }

        private Match? ValidateUA()
        {
            // validate user agent and record version
            Request.Headers.TryGetValue("User-Agent", out var uaHeader);

            if (uaHeader.Count == 0)
                return null;

            string? ua = uaHeader.First();

            if (ua is null)
                return null;

            var match = UARegex().Match(ua);

            if (!match.Success)
                return null;

            string info = match.Groups[2].Value;

            if (!_uaTypes.Any(info.StartsWith) || info.Length > 128)
                return null;

            return match;
        }
    }
}
