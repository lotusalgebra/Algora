using Microsoft.AspNetCore.Mvc;

namespace Algora.Web.Controllers
{
    public class OrdersController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
