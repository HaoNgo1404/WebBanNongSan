using Microsoft.AspNetCore.Mvc;

namespace WebWeb.Controllers
{
    public class SearchController : Controller
    {
        public IActionResult Index(string keyword)
        {
            return View();
        }

        public IActionResult Suggest(string keyword)
        {
            return View();
        }
    }
}
