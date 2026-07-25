using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using WebWeb.Models;
using WebWeb.Services;

namespace WebWeb.Controllers
{
    public class CartController : Controller
    {
        private readonly ECommerceDBContext _context;
        private const string CART_SESSION_KEY = "UserCart";
        private readonly KhuyenMaiService _khuyenMaiService;

        public CartController(ECommerceDBContext context, KhuyenMaiService khuyenMaiService)
        {
            _context = context;
            _khuyenMaiService = khuyenMaiService;
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
        public async Task<IActionResult> Index()
{
            var cart = GetCartItems(); // Lấy từ Session

            decimal tongTienHang = 0;

            foreach (var item in cart)
            {
                // 1. Lấy giá gốc từ DB hoặc Session (item.Gia)
                // 2. Tính giá đã giảm thực tế
                item.GiaThucTe = _khuyenMaiService.TinhGiaBanThucTe(item.NongSanId, item.Gia);
                
                // Cộng dồn tổng tiền dựa trên giá thực tế
                tongTienHang += item.ThanhTien;
            }

            // Tính phí ship động từ DB
            var thamSoPhiShip = await _context.ThamSos.FirstOrDefaultAsync(t => t.MaThamSo == "TS5");
            decimal phiShipMacDinh = thamSoPhiShip != null ? thamSoPhiShip.GiaTri : 30000;

            var thamSoNguongFree = await _context.ThamSos.FirstOrDefaultAsync(t => t.MaThamSo == "TS4");
            decimal nguongMienPhiShip = thamSoNguongFree != null ? thamSoNguongFree.GiaTri : 500000;

            decimal phiVanChuyenThucTe = (tongTienHang > 0 && tongTienHang < nguongMienPhiShip) ? phiShipMacDinh : 0;

            ViewBag.PhiVanChuyen = phiVanChuyenThucTe;
            ViewBag.TongTienHang = tongTienHang;
            ViewBag.TongThanhToan = tongTienHang + phiVanChuyenThucTe;

            // KHÔNG gọi SaveCartItems(cart) ở đây nữa để tránh đè Session!
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
            // 1. Lấy danh sách item từ Session
            var cart = GetCartItems(); 

            // 2. Tính toán GiaThucTe cho từng sản phẩm trước khi truyền sang View
            foreach (var item in cart)
            {
                item.GiaThucTe = _khuyenMaiService.TinhGiaBanThucTe(item.NongSanId, item.Gia);
            }
            return PartialView("MiniCart",cart);
        }
    }
}