using Microsoft.AspNetCore.Mvc;

namespace WebWeb.Controllers
{
    public class ContactController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Send()
        {
            return View();
        }
    }
}
