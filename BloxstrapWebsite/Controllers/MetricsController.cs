using BloxstrapWebsite.Models;
using BloxstrapWebsite.Models.Configuration;
using BloxstrapWebsite.Services;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;

using System.Text.RegularExpressions;

namespace BloxstrapWebsite.Controllers
{
    public partial class MetricsController : Controller
    {
        private readonly Credentials _credentials;

        private readonly IMemoryCache _memoryCache;

        private readonly IStatsService _statsService;

        private readonly Dictionary<string, StatPoint> _statPoints = new()
        {
            { 
                "packageDownloadState", new StatPoint 
                { 
                    Values = ["httpSuccess", "httpFail", "retrySuccess"],
                    ProductionOnly = false
                } 
            },
        };

        private readonly List<string> _uaTypes = ["Production", "Artifact", "Build"];


        [GeneratedRegex(@"Bloxstrap\/([0-9\.]+) \((.*)\)")]
        private static partial Regex UARegex();

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
            
            if (ua is null)
                return BadRequest();

            var match = UARegex().Match(ua);

            if (!match.Success || !Version.TryParse(match.Groups[1].Value, out var version) || version > _statsService.Version)
                return BadRequest();

            // validate stats key/value
            if (!_statPoints.TryGetValue(key, out var statPoint) || statPoint is null || !statPoint.Values.Contains(value))
                return BadRequest();

            string info = match.Groups[2].Value;

            if (statPoint.ProductionOnly && info != "Production" || !_uaTypes.Any(info.StartsWith))
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
                    .Tag("version", ua)
                    .Timestamp(DateTime.UtcNow, WritePrecision.Ns);

            writeApi.WritePoint(point, "bloxstrap", "pizza-server");

            return Ok();
        }
    }
}
