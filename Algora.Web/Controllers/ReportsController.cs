using Microsoft.AspNetCore.Mvc;

namespace Algora.Web.Controllers
{
    public class ReportsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
