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
            // 1. Include LoHangs để kiểm tra HSD và tồn kho lô hàng
            var product = await _context.NongSans
                .Include(n => n.LoHangs)
                .FirstOrDefaultAsync(n => n.NongSanId == id);

            if (product == null) return NotFound();

            // 2. Kiểm tra điều kiện Hết hàng / Hết hạn sử dụng
            bool isConHan = product.LoHangs == null || !product.LoHangs.Any() || 
                            product.LoHangs.Any(l => l.SoLuongTon > 0 && l.HanSuDung >= DateTime.Now);

            if (product.SoLuongTon <= 0 || !isConHan)
            {
                TempData["ErrorMessage"] = "Sản phẩm này hiện đã hết hàng hoặc hết hạn sử dụng!";
                return RedirectToAction(nameof(Index)); // Hoặc Redirect về trang trước đó
            }

            var cart = GetCartItems();
            var existingItem = cart.FirstOrDefault(c => c.NongSanId == id);
            int currentInCart = existingItem != null ? existingItem.SoLuong : 0;

            // 3. Kiểm tra số lượng đặt không vượt quá số lượng tồn kho
            if (currentInCart + quantity > product.SoLuongTon)
            {
                TempData["ErrorMessage"] = $"Rất tiếc, chỉ còn lại {product.SoLuongTon} sản phẩm khả dụng!";
                return RedirectToAction(nameof(Index));
            }

            // 4. Tiến hành thêm vào giỏ hàng
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
            TempData["SuccessMessage"] = "Đã thêm sản phẩm vào giỏ hàng!";
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