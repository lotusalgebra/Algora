using Microsoft.AspNetCore.Mvc;

namespace Algora.Web.Controllers
{
    public class WebhookController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
