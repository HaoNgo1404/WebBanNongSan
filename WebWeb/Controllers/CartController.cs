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

        // TRANG GIỎ HÀNG CHÍNH (ĐÃ CẬP NHẬT THAM SỐ PHÍ SHIP ĐỘNG)
        public async Task<IActionResult> Index()
        {
            // 1. Lấy danh sách sản phẩm trong giỏ từ Session hiện tại
            var cart = GetCartItems();

            foreach (var item in cart)
            {
                // Gọi Service dùng chung tính giá thực tế tại thời điểm xem giỏ hàng
                decimal giaThucTe = _khuyenMaiService.TinhGiaBanThucTe(item.NongSanId, item.Gia);
                
                item.Gia = giaThucTe; 
            }
            
            SaveCartItems(cart);

            // Tính tổng tiền các mặt hàng có trong giỏ (Dùng kiểu decimal để khớp tính toán)
            decimal tongTienHang = cart.Sum(item => (decimal)item.ThanhTien);

            // 2. ĐỌC THAM SỐ ĐỘNG TỪ DATABASE ĐỂ TÍNH PHÍ VẬN CHUYỂN
            var thamSoPhiShip = await _context.ThamSos.FirstOrDefaultAsync(t => t.MaThamSo == "TS5");
            decimal phiShipMacDinh = thamSoPhiShip != null ? thamSoPhiShip.GiaTri : 30000; // Backup 30k nếu trống DB

            var thamSoNguongFree = await _context.ThamSos.FirstOrDefaultAsync(t => t.MaThamSo == "TS4");
            decimal nguongMienPhiShip = thamSoNguongFree != null ? thamSoNguongFree.GiaTri : 500000; // Backup 500k nếu trống DB

            // 3. Logic kiểm tra điều kiện tính phí ship thực tế
            decimal phiVanChuyenThucTe = 0;
            if (tongTienHang > 0)
            {
                // Nếu tổng hóa đơn lớn hơn hoặc bằng ngưỡng quy định -> Miễn phí (0đ), ngược lại tính phí mặc định
                phiVanChuyenThucTe = tongTienHang >= nguongMienPhiShip ? 0 : phiShipMacDinh;
            }

            // 4. Bỏ vào ViewBag để chuyển giao dữ liệu ra file Index.cshtml hứng dùng
            ViewBag.PhiVanChuyen = phiVanChuyenThucTe;
            ViewBag.TongTienHang = tongTienHang;
            ViewBag.TongThanhToan = tongTienHang + phiVanChuyenThucTe;

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