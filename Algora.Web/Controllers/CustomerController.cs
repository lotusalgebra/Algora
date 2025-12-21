using Microsoft.AspNetCore.Mvc;

namespace Algora.Web.Controllers
{
    public class CustomerController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
