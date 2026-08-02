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
        // 🟢 TỐI ƯU SEO TRANG CHỦ
        ViewData["Title"] = "Green Fresh - Nông Sản Sạch & Thực Phẩm Tươi Ngon";
        ViewData["MetaDescription"] = "Green Fresh chuyên cung cấp rau củ quả tươi sạch, đạt chuẩn OCOP, nguồn gốc rõ ràng, giao hàng tận nơi nhanh chóng.";
        ViewData["MetaKeywords"] = "green fresh, nông sản sạch, rau củ tươi, trái cây OCOP, thực phẩm sạch";
        
        // Open Graph cho Facebook
        ViewData["OgType"] = "website";
        ViewData["OgTitle"] = "Green Fresh - Nông Sản Sạch Cho Gia Đình";
        ViewData["OgDescription"] = "Cung cấp nông sản sạch, đạt chuẩn an toàn thực phẩm, giao nhanh trong ngày.";
        ViewData["OgImage"] = $"{Request.Scheme}://{Request.Host}/images/banner-home.jpg"; // Đảm bảo bạn có file ảnh này trong wwwroot/images/

        var now = DateTime.Now;
        var khuyenMais = await _context.KhuyenMais
            .Where(k => k.NgayKetThuc >= now)
            .OrderByDescending(k => k.KhuyenMaiId)
            .Take(5)
            .ToListAsync();

        ViewBag.KhuyenMais = khuyenMais;

        // 🟢 Lấy danh sách đánh giá thực tế từ Database (Ưu tiên đánh giá cao >= 4 sao)
        var realReviews = await _context.DanhGiaSanPhams
            .Include(d => d.KhachHang)
            .Where(d => d.SoSao >= 4 && !string.IsNullOrEmpty(d.BinhLuan))
            .OrderByDescending(d => d.NgayDanhGia)
            .Take(12) // Lấy tối đa 12 đánh giá thật
            .ToListAsync();

        ViewBag.RealReviews = realReviews;

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