using BloxstrapWebsite.Models;
using BloxstrapWebsite.Models.Configuration;
using BloxstrapWebsite.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace BloxstrapWebsite.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly Credentials _credentials;
        private readonly IStatsService _statsService;

        public HomeController(ILogger<HomeController> logger, IOptions<Credentials> credentials, IStatsService statsService)
        {
            _logger = logger;
            _credentials = credentials.Value;
            _statsService = statsService;
        }

        public async Task<IActionResult> Index()
        {
            if (!_statsService.Loaded)
                await _statsService.Update();

            return View(new IndexViewModel { 
                StarCount = _statsService.StarCount, 
                Version = _statsService.Version, 
                ReleaseSizeMB = _statsService.ReleaseSizeMB 
            });
        }

        public async Task<IActionResult> UpdateStats(string key)
        {
            if (key != _credentials.StatsKey)
                return Unauthorized();

            await _statsService.Update();

            return Ok();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
