using Microsoft.AspNetCore.Mvc;

namespace Algora.Web.Controllers
{
    public class AuthController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
