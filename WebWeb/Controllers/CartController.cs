using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using WebWeb.Models;

namespace WebWeb.Controllers
{
    public class CartController : Controller
    {
        private readonly ECommerceDBContext _context;
        private const string CART_SESSION_KEY = "UserCart";

        public CartController(ECommerceDBContext context)
        {
            _context = context;
        }

        // 1. Đọc danh sách giỏ hàng từ Session
        private List<GioHang> GetCartItems()
        {
            var sessionData = HttpContext.Session.GetString(CART_SESSION_KEY);
            return sessionData == null ? new List<GioHang>() : JsonSerializer.Deserialize<List<GioHang>>(sessionData);
        }

        // 2. Lưu danh sách giỏ hàng vào Session
        private void SaveCartItems(List<GioHang> cart)
        {
            HttpContext.Session.SetString(CART_SESSION_KEY, JsonSerializer.Serialize(cart));
        }

        // TRANG GIỎ HÀNG CHÍNH
        public IActionResult Index()
        {
            var cart = GetCartItems();
            return View(cart);
        }

        // THÊM SẢN PHẨM VÀO GIỎ HÀNG
        public async Task<IActionResult> Add(int id, int quantity = 1)
        {
            var product = await _context.NongSans.FirstOrDefaultAsync(n => n.NongSanId == id);
            if (product == null) return NotFound();

            var cart = GetCartItems();
            var existingItem = cart.FirstOrDefault(c => c.NongSanId == id);

            if (existingItem != null)
            {
                existingItem.SoLuong += quantity;
            }
            else
            {
                cart.Add(new GioHang
                {
                    NongSanId = product.NongSanId,
                    TenNongSan = product.TenNongSan,
                    HinhAnh = product.HinhAnh ?? "",
                    Gia = product.GiaBanNiemYet,
                    DonViTinh = product.DonViTinh ?? "bó",
                    SoLuong = quantity
                });
            }

            SaveCartItems(cart);
            return RedirectToAction(nameof(Index));
        }

        // CẬP NHẬT SỐ LƯỢNG (Dùng cho nút + - hoặc nhập số)
        public IActionResult Update(int id, int quantity)
        {
            var cart = GetCartItems();
            var item = cart.FirstOrDefault(c => c.NongSanId == id);

            if (item != null)
            {
                if (quantity <= 0)
                {
                    cart.Remove(item);
                }
                else
                {
                    item.SoLuong = quantity;
                }
                SaveCartItems(cart);
            }
            return RedirectToAction(nameof(Index));
        }

        // XÓA SẢN PHẨM KHỎI GIỎ HÀNG
        public IActionResult Remove(int id)
        {
            var cart = GetCartItems();
            var item = cart.FirstOrDefault(c => c.NongSanId == id);

            if (item != null)
            {
                cart.Remove(item);
                SaveCartItems(cart);
            }
            return RedirectToAction(nameof(Index));
        }

        // XÓA SẠCH GIỎ HÀNG
        public IActionResult Clear()
        {
            HttpContext.Session.Remove(CART_SESSION_KEY);
            return RedirectToAction(nameof(Index));
        }

        // PHẦN MINI CART GÓC MÀN HÌNH (PartialView)
        public IActionResult MiniCart()
        {
            var cart = GetCartItems();
            return PartialView(cart);
        }
    }
}