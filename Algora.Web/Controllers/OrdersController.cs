using Microsoft.AspNetCore.Mvc;

namespace Algora.Web.Controllers
{
    [Route("Orders")]
    public class OrdersController : Controller
    {
        [HttpGet("")] // matches GET /Orders
        public IActionResult Index()
        {
            return View(); // make sure Views/Orders/Index.cshtml exists
        }
    }
}
