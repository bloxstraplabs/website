﻿using BloxstrapWebsite.Models.Configuration;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;
using RestSharp;

namespace BloxstrapWebsite.Controllers
{
    public class MetricsController : Controller
    {
        private readonly Credentials _credentials;

        private readonly IMemoryCache _memoryCache;

        public MetricsController(IOptions<Credentials> credentials, IMemoryCache memoryCache)
        {
            _credentials = credentials.Value;
            _memoryCache = memoryCache;
        }

        public IActionResult Post(string key, bool value)
        {
            if (key != "httpDownloadSuccess")
                return BadRequest();

            var requestIp = Request.HttpContext.Connection.RemoteIpAddress;

            if (requestIp is null)
                return StatusCode(500);

            string cacheKey = $"ratelimit-metrics-{key}-{requestIp}";

            if (_memoryCache.TryGetValue(cacheKey, out _))
                return StatusCode(429);
            
            _memoryCache.Set(cacheKey, DateTime.Now, DateTime.Now.AddMinutes(1));

            string? token = _credentials.InfluxDBToken;

            if (String.IsNullOrEmpty(token))
                token = Environment.GetEnvironmentVariable("BLOXSTRAP_WEBSITE_TOKEN_INFLUXDB");

            if (String.IsNullOrEmpty(token))
                throw new InvalidOperationException();

            using var client = new InfluxDBClient("https://influxdb.internal.pizzaboxer.xyz", _credentials.InfluxDBToken);

            using var writeApi = client.GetWriteApi();

            var point = PointData.Measurement("metrics")
                    .Field(key, value)
                    .Timestamp(DateTime.UtcNow, WritePrecision.Ns);

            writeApi.WritePoint(point, "bloxstrap", "pizza-server");

            return Ok();
        }
    }
}
