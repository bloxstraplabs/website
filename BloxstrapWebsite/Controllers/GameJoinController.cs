using Microsoft.AspNetCore.Mvc;

namespace BloxstrapWebsite.Controllers
{
    public class GameJoinController : Controller
    {
        public IActionResult Index()
        {
            var placeId = HttpContext.Request.Query["placeId"].ToString();
            var instanceId = HttpContext.Request.Query["gameInstanceId"].ToString();
            var url = $"roblox://experiences/start?placeId={placeId}";

            if (!string.IsNullOrEmpty(instanceId))
            {
                url += $"&gameInstanceId={instanceId}";
            }

            return Redirect(url);
        }
    }
}
