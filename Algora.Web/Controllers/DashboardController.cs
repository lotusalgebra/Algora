using Microsoft.AspNetCore.Mvc;

namespace Algora.Web.Controllers
{
    public class DashboardController : Controller
    {

        [Route("dashboard")]
        public IActionResult Index()
        {
            return View();
        }
    }
}
