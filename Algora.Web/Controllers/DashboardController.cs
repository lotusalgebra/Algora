using Microsoft.AspNetCore.Mvc;

namespace Algora.Web.Controllers
{
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
