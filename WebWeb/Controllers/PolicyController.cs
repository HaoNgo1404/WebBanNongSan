using Microsoft.AspNetCore.Mvc;

namespace WebWeb.Controllers
{
    public class PolicyController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
