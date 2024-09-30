using BloxstrapWebsite.Models.Configuration;
using BloxstrapWebsite.Services;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;

namespace BloxstrapWebsite.Controllers
{
    public class MetricsController : Controller
    {
        private readonly Credentials _credentials;

        private readonly IMemoryCache _memoryCache;

        private readonly IStatsService _statsService;

        private readonly Dictionary<string, List<string>> _statPoints = new()
        {
            { "packageDownloadState", new() { "httpSuccess", "httpFail", "retrySuccess" } }
        };

        public MetricsController(IOptions<Credentials> credentials, IMemoryCache memoryCache, IStatsService statsService)
        {
            _credentials = credentials.Value;
            _memoryCache = memoryCache;
            _statsService = statsService;
        }

        public IActionResult Post(string? key, string? value)
        {
            if (key is null || value is null)
                return BadRequest();

            // validate user agent and record version
            Request.Headers.TryGetValue("User-Agent", out var uaHeader);

            if (uaHeader.Count == 0)
                return BadRequest();

            string? ua = uaHeader.First();

            if (ua is null || !ua.StartsWith("pizzaboxer/bloxstrap"))
                return BadRequest();

            var parts = ua.Split("/");

            if (parts.Length != 3 || !Version.TryParse(parts[2], out var version) || version > _statsService.Version)
                return BadRequest();

            // validate stats key/value
            if (!_statPoints.TryGetValue(key, out List<string>? values) || values is null || !values.Contains(value))
                return BadRequest();

            // ratelimit
#if !DEBUG
            var requestIp = Request.HttpContext.Connection.RemoteIpAddress;

            if (requestIp is null)
                return StatusCode(500);

            string cacheKey = $"ratelimit-metrics-{key}-{requestIp}";

            if (_memoryCache.TryGetValue(cacheKey, out _))
                return StatusCode(429);
            
            _memoryCache.Set(cacheKey, DateTime.Now, DateTime.Now.AddMinutes(1));
#endif

            string? token = _credentials.InfluxDBToken;

            if (String.IsNullOrEmpty(token))
                throw new InvalidOperationException();

            using var client = new InfluxDBClient("https://influxdb.internal.pizzaboxer.xyz", token);

            using var writeApi = client.GetWriteApi();

            var point = PointData.Measurement("metrics")
                    .Field(key, value)
                    .Tag("version", version.ToString())
                    .Timestamp(DateTime.UtcNow, WritePrecision.Ns);

            writeApi.WritePoint(point, "bloxstrap", "pizza-server");

            return Ok();
        }
    }
}
