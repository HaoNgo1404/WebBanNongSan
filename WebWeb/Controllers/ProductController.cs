using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Security.Claims;
using WebWeb.Models;

namespace WebWeb.Controllers;

public class ProductController : Controller
{
    private readonly ECommerceDBContext _context;

    public ProductController(ECommerceDBContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Detail(int id)
    {
        var product = await _context.NongSans
            .Include(n => n.NhaVuon)
            .Include(n => n.DanhGiaSanPhams)
            .FirstOrDefaultAsync(n => n.NongSanId == id);

        if (product == null) return NotFound();

        // LOGIC CHECK TIM: Lấy danh sách ID đã thích của User hiện tại
        List<int> likedProductIds = new List<int>();
        
        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            // Nếu đã đăng nhập: Lấy từ Database
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("KhachHangId")?.Value;
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int customerId))
            {
                likedProductIds = await _context.YeuThiches
                    .Where(yt => yt.KhachHangId == customerId)
                    .Select(yt => yt.NongSanId)
                    .ToListAsync();
            }
        }
        else
        {
            // Nếu chưa đăng nhập: Lấy từ Session
            var sessionData = HttpContext.Session.GetString("UserWishlist");
            if (!string.IsNullOrEmpty(sessionData))
            {
                likedProductIds = JsonSerializer.Deserialize<List<int>>(sessionData) ?? new List<int>();
            }
        }

        // Gửi danh sách ID này sang View
        ViewBag.LikedProductIds = likedProductIds;

        return View(product);
    }

    // URL chạy thực tế sẽ dạng: /Product/DanhMuc?id=1 hoặc /Product/DanhMuc/1 tùy cấu hình route
    public async Task<IActionResult> DanhMuc(int id)
    {
        // Tìm danh mục dựa vào DanhMucId truyền từ Navbar qua
        var danhMuc = await _context.DanhMucs
            .FirstOrDefaultAsync(dm => dm.DanhMucId == id);

        if (danhMuc == null)
        {
            return NotFound();
        }

        // Lấy danh sách nông sản thuộc danh mục này
        var dsNongSan = await _context.NongSans
            .Where(ns => ns.DanhMucId == id)
            .ToListAsync();

        // Gửi thông tin sang View hiển thị
        ViewBag.TenDanhMuc = danhMuc.TenDanhMuc;
        ViewData["Title"] = danhMuc.TenDanhMuc;

        return View(dsNongSan);
    }
}