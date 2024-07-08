using BloxstrapWebsite.Models;
using BloxstrapWebsite.Models.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace BloxstrapWebsite.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly Credentials _credentials;

        public HomeController(ILogger<HomeController> logger, IOptions<Credentials> credentials)
        {
            _logger = logger;
            _credentials = credentials.Value;
        }

        public async Task<IActionResult> Index()
        {
            if (!Stats.Loaded)
                await Stats.Update();

            return View(new IndexViewModel { StarCount = Stats.StarCount, Version = Stats.Version, ReleaseSizeMB = Stats.ReleaseSizeMB });
        }

        public async Task<IActionResult> UpdateStats(string key)
        {
            if (key != _credentials.StatsKey)
                return Unauthorized();

            await Stats.Update();

            return Ok();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
