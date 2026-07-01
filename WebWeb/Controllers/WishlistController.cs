using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using WebWeb.Models;

namespace WebWeb.Controllers
{
    public class WishlistController : Controller
    {
        private readonly ECommerceDBContext _context;
        private const string WISHLIST_SESSION_KEY = "UserWishlist";

        public WishlistController(ECommerceDBContext context)
        {
            _context = context;
        }

        // Helper lấy ID khách hàng từ Cookie Claims giống bên OrderController
        private int? GetCurrentKhachHangId()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    userIdClaim = User.FindFirst("KhachHangId")?.Value;
                }

                if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int id))
                {
                    return id;
                }
            }
            return null;
        }

        // Đọc danh sách ID sản phẩm từ Session (Chỉ dùng cho khách vãng lai)
        private List<int> GetWishlistSessionItems()
        {
            var sessionData = HttpContext.Session.GetString(WISHLIST_SESSION_KEY);
            return sessionData == null ? new List<int>() : JsonSerializer.Deserialize<List<int>>(sessionData);
        }

        // Lưu danh sách ID vào Session (Chỉ dùng cho khách vãng lai)
        private void SaveWishlistSessionItems(List<int> wishlist)
        {
            HttpContext.Session.SetString(WISHLIST_SESSION_KEY, JsonSerializer.Serialize(wishlist));
        }

        // =================================================================
        // TRANG DANH SÁCH YÊU THÍCH
        // =================================================================
        public async Task<IActionResult> Index()
        {
            int? currentUserId = GetCurrentKhachHangId();
            List<NongSan> favoriteProducts = new List<NongSan>();

            if (currentUserId != null)
            {
                // NẾU ĐÃ ĐĂNG NHẬP: Lấy từ Database
                favoriteProducts = await _context.YeuThiches
                    .Where(yt => yt.KhachHangId == currentUserId.Value)
                    .Select(yt => yt.NongSan)
                    .ToListAsync();
            }
            else
            {
                // NẾU CHƯA ĐĂNG NHẬP: Lấy từ Session
                var productIds = GetWishlistSessionItems();
                favoriteProducts = await _context.NongSans
                    .Where(ns => productIds.Contains(ns.NongSanId))
                    .ToListAsync();
            }

            return View(favoriteProducts);
        }

        // =================================================================
        // HÀNH ĐỘNG THÊM SẢN PHẨM VÀO YÊU THÍCH
        // =================================================================
        public async Task<IActionResult> Add(int id)
        {
            var productExists = await _context.NongSans.AnyAsync(ns => ns.NongSanId == id);
            if (!productExists) return NotFound();

            int? currentUserId = GetCurrentKhachHangId();

            if (currentUserId != null)
            {
                // NẾU ĐÃ ĐĂNG NHẬP: Lưu vào Database
                var alreadyExists = await _context.YeuThiches
                    .AnyAsync(yt => yt.KhachHangId == currentUserId.Value && yt.NongSanId == id);

                if (!alreadyExists)
                {
                    var yeuThich = new YeuThich
                    {
                        KhachHangId = currentUserId.Value,
                        NongSanId = id,
                        NgayThem = DateTime.Now
                    };
                    _context.YeuThiches.Add(yeuThich);
                    await _context.SaveChangesAsync();
                }
            }
            else
            {
                // NẾU CHƯA ĐĂNG NHẬP: Lưu tạm vào Session
                var wishlist = GetWishlistSessionItems();
                if (!wishlist.Contains(id))
                {
                    wishlist.Add(id);
                    SaveWishlistSessionItems(wishlist);
                }
            }

            return RedirectToAction(nameof(Index));
        }

        // =================================================================
        // HÀNH ĐỘNG XÓA SẢN PHẨM KHỎI YÊU THÍCH
        // =================================================================
        public async Task<IActionResult> Remove(int id)
        {
            int? currentUserId = GetCurrentKhachHangId();

            if (currentUserId != null)
            {
                // NẾU ĐÃ ĐĂNG NHẬP: Xóa khỏi Database
                var yeuThichItem = await _context.YeuThiches
                    .FirstOrDefaultAsync(yt => yt.KhachHangId == currentUserId.Value && yt.NongSanId == id);
                
                if (yeuThichItem != null)
                {
                    _context.YeuThiches.Remove(yeuThichItem);
                    await _context.SaveChangesAsync();
                }
            }
            else
            {
                // NẾU CHƯA ĐĂNG NHẬP: Xóa khỏi Session
                var wishlist = GetWishlistSessionItems();
                if (wishlist.Contains(id))
                {
                    wishlist.Remove(id);
                    SaveWishlistSessionItems(wishlist);
                }
            }

            return RedirectToAction(nameof(Index));
        }

        // =================================================================
        // XÓA SẠCH DANH SÁCH YÊU THÍCH
        // =================================================================
        public async Task<IActionResult> Clear()
        {
            int? currentUserId = GetCurrentKhachHangId();

            if (currentUserId != null)
            {
                // NẾU ĐÃ ĐĂNG NHẬP: Xóa toàn bộ dòng dữ liệu của user đó trong DB
                var userWishlist = await _context.YeuThiches
                    .Where(yt => yt.KhachHangId == currentUserId.Value)
                    .ToListAsync();

                if (userWishlist.Any())
                {
                    _context.YeuThiches.RemoveRange(userWishlist);
                    await _context.SaveChangesAsync();
                }
            }
            else
            {
                // NẾU CHƯA ĐĂNG NHẬP: Clear Session
                HttpContext.Session.Remove(WISHLIST_SESSION_KEY);
            }

            return RedirectToAction(nameof(Index));
        }
    }
}