using Microsoft.AspNetCore.Mvc;

namespace WebWeb.Controllers
{
    public class FAQController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
