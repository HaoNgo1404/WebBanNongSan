using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebWeb.Models;

namespace WebWeb.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ECommerceDBContext _context;

    public HomeController(ILogger<HomeController> logger, ECommerceDBContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var products = await _context.NongSans
            .Include(n => n.LoHangs)
            .Include(n => n.DanhGiaSanPhams)
            .Include(n => n.DanhMuc)
            .Include(n => n.NhaVuon)
            .Where(n => n.LoHangs.Sum(l => l.SoLuongTon) > 0 || n.DanhGiaSanPhams.Any(d => d.SoSao >= 4))
            .ToListAsync();

        return View(products);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}