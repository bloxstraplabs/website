using BloxstrapWebsite.Enums;
using BloxstrapWebsite.Models;
using BloxstrapWebsite.Services;

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

        private readonly IStatsService _statsService;

        private readonly IHttpClientFactory _httpClientFactory;

        private readonly List<StatPoint> _statPoints = 
        [
            new StatPoint 
            { 
                Name = "packageDownloadState",
                Values = ["httpSuccess", "httpFail", "retrySuccess"],
                ProductionOnly = false,
                RatelimitInterval = 60,
                RatelimitCount = 18
            },

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

        [GeneratedRegex(@"Bloxstrap\/([0-9\.]+) \((.*)\)")]
        private static partial Regex UARegex();

        public MetricsController(IInfluxDBClient influxDBClient, IMemoryCache memoryCache, 
            IStatsService statsService, IHttpClientFactory httpClientFactory)
        {
            _influxDBClient = influxDBClient;
            _memoryCache = memoryCache;
            _statsService = statsService;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IActionResult> Post(string? key, string? value)
        {
            if (String.IsNullOrEmpty(key) || String.IsNullOrEmpty(value))
                return BadRequest();

            // validate user agent and record version
            Request.Headers.TryGetValue("User-Agent", out var uaHeader);

            if (uaHeader.Count == 0)
                return BadRequest();

            string? ua = uaHeader.First();
            
            if (ua is null)
                return BadRequest();

            var match = UARegex().Match(ua);

            if (!match.Success || !Version.TryParse(match.Groups[1].Value, out var version) || version > _statsService.Version)
                return BadRequest();

            var statPoint = _statPoints.Find(x => x.Name == key);

            // validate stats key/value
            if (statPoint is null || statPoint.Values is not null && !statPoint.Values.Contains(value))
                return BadRequest();

            string info = match.Groups[2].Value;

            if (statPoint.ProductionOnly && info != "Production" || !_uaTypes.Any(info.StartsWith))
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
                    .Tag("version", ua)
                    .Timestamp(DateTime.UtcNow, WritePrecision.Ns);

            await _influxDBClient.GetWriteApiAsync().WritePointAsync(point, statPoint.Bucket, "pizza-server");

            return Ok();
        }
    }
}
