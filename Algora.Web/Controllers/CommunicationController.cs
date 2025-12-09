using Microsoft.AspNetCore.Mvc;

namespace Algora.Web.Controllers
{
    public class CommunicationController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
