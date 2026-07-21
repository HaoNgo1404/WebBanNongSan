using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using WebWeb.Models;

namespace WebWeb.Controllers
{
    public class NotificationController : Controller
    {
        private readonly ECommerceDBContext _context;

        public NotificationController(ECommerceDBContext context)
        {
            _context = context;
        }

        // Helper lấy ID khách hàng từ Cookie Claims để lọc thông tin cá nhân
        private int? GetCurrentKhachHangId()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("KhachHangId")?.Value;
                if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int id))
                {
                    return id;
                }
            }
            return null;
        }

        // =================================================================
        // 1. TRANG THÔNG BÁO ĐẶT HÀNG LẺ / GÓI ĐỊNH KỲ THÀNH CÔNG (Giữ nguyên luồng cũ của bạn)
        // =================================================================
        public IActionResult OrderSuccess(int orderId)
        {
            ViewBag.OrderId = orderId;
            return View();
        }

        public IActionResult OrderPackageSuccess(int orderId)
        {
            ViewBag.GoiId = orderId;
            return View();
        }

        // =================================================================
        // 2. TRANG THÔNG BÁO TIẾN ĐỘ ĐƠN HÀNG (Dành cho khách hàng theo dõi đơn)
        // URL: /Notification/TrackingOrder?orderId=...
        // =================================================================
        [HttpGet]
        public async Task<IActionResult> TrackingOrder(int orderId)
        {
            int? currentUserId = GetCurrentKhachHangId();
            
            if (currentUserId == null)
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập tài khoản để xem thông tin thông báo!";
                return RedirectToAction("Login", "Account");
            }
            // Lấy danh sách tất cả đơn hàng lẻ của khách này, xếp đơn mới nhất lên đầu
            var danhSachDonHang = await _context.DonHangLes
                .Where(d => d.KhachHangId == currentUserId.Value)
                .OrderByDescending(d => d.NgayDat)
                .ToListAsync();

            return View(danhSachDonHang);
        }

        [HttpGet]
        public async Task<IActionResult> GetOrderTrackingPopup(int orderId)
        {
            int? currentUserId = GetCurrentKhachHangId();
            if (currentUserId == null)
            {
                return Content("<div class='alert alert-warning small m-0'>Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại!</div>");
            }

            var donHang = await _context.DonHangLes
                .FirstOrDefaultAsync(d => d.DonHangLeId == orderId && d.KhachHangId == currentUserId.Value);

            if (donHang == null)
            {
                return Content("<div class='alert alert-danger small m-0'>Không tìm thấy thông tin đơn hàng hợp lệ!</div>");
            }

            // Trả về file Partial View kèm theo dữ liệu thật của đơn hàng
            return PartialView("_OrderTrackingPopup", donHang);
        }

        [HttpGet]
        public async Task<IActionResult> GetNotificationsJson()
        {
            int? currentUserId = GetCurrentKhachHangId();
            if (currentUserId == null) 
            {
                return Json(new { success = false, message = "Chưa đăng nhập" }); 
            }

            var listNotifications = new List<object>();

            // 1. LẤY THÔNG BÁO TỪ TIẾN ĐỘ ĐƠN HÀNG (Dữ liệu thật từ DB)
            var donHangs = await _context.DonHangLes
                .Where(d => d.KhachHangId == currentUserId.Value)
                .OrderByDescending(d => d.NgayDat)
                .Take(3)
                .ToListAsync();

            foreach (var d in donHangs)
            {
                listNotifications.Add(new {
                    id = d.DonHangLeId,
                    type = "order",
                    icon = d.TrangThaiDonHang == OrderStatuses.DangGiao ? "bi-truck" : (d.TrangThaiDonHang == OrderStatuses.HoanThanh ? "bi-check-circle-fill" : "bi-box-seam"),
                    badgeClass = d.TrangThaiDonHang == OrderStatuses.DangGiao ? "bg-warning" : "bg-success",
                    title = $"Đơn hàng #{d.DonHangLeId} - {d.TrangThaiDonHang}",
                    content = $"Đơn hàng nông sản của bạn hiện tại đang ở trạng thái: {d.TrangThaiDonHang}.",
                    time = d.NgayDat.ToString("dd/MM/yyyy HH:mm"),
                    url = $"/Order/OrderHistoryDetail?id={d.DonHangLeId}",
                    isPopup = true
                });
            }

            // 2. LẤY THÔNG BÁO KHUYẾN MÃI MỚI (Dữ liệu thật 100% từ DB KhuyenMai)
            var now = DateTime.Now;
            var khuyenMais = await _context.KhuyenMais
                .Where(k => k.TrangThai 
                        && k.NgayKetThuc >= now 
                        && k.NgayBatDau <= now 
                        && k.SoLuotDaDung < k.SoLuotPhatHanh)
                .OrderByDescending(k => k.NgayBatDau)
                .Take(3)
                .ToListAsync();

            foreach (var km in khuyenMais)
            {
                string mucGiamStr = km.LoaiGiamGia == 1 ? $"{km.MucGiam:0}%" : $"{km.MucGiam:N0}đ";
                string voucherCodeStr = string.IsNullOrEmpty(km.VoucherCode) ? "" : $" (Mã: {km.VoucherCode})";

                listNotifications.Add(new {
                    id = km.KhuyenMaiId,
                    type = "promo",
                    icon = "bi-tags-fill",
                    badgeClass = "bg-danger",
                    title = $"🎁 {km.TenChuongTrinh}",
                    content = $"Ưu đãi giảm {mucGiamStr}{voucherCodeStr} cho đơn hàng từ {km.GiaTriDonToiThieu:N0}đ. Hạn dùng đến {km.NgayKetThuc:dd/MM/yyyy}.",
                    time = km.NgayBatDau.ToString("dd/MM/yyyy"),
                    url = "/Notification/Offers",
                    isPopup = false
                });
            }

            // Giả định quy ước TrangThai trong CSDL của bạn: 0 = Mới gửi, 1 = Đang xử lý, 2 = Đã xử lý (Hoàn tất)
            int TRANG_THAI_DA_XU_LY = 1; 

            // ==========================================
            // 3. THÔNG BÁO KHIẾU NẠI / HỖ TRỢ CSKH
            // ==========================================
            var dsKhieuNaiDaXuLy = await _context.KhieuNais
                .Where(k => k.KhachHangId == currentUserId.Value && k.TrangThai == TRANG_THAI_DA_XU_LY)
                .OrderByDescending(k => k.NgayGui)
                .Take(5)
                .ToListAsync();

            foreach (var khieuNai in dsKhieuNaiDaXuLy)
            {
                listNotifications.Add(new {
                    id = khieuNai.KhieuNaiId,
                    type = "support",
                    icon = "bi-headset",
                    badgeClass = "bg-info",
                    title = $"Yêu cầu hỗ trợ #{khieuNai.KhieuNaiId} đã hoàn tất",
                    content = string.IsNullOrEmpty(khieuNai.PhuongAnXuLy) 
                                ? $"Khiếu nại về '{khieuNai.NoiDung}' của bạn đã được xử lý." 
                                : $"Phương án xử lý: {khieuNai.PhuongAnXuLy}",
                    time = khieuNai.NgayGui.ToString("dd/MM/yyyy HH:mm"),
                    url = $"/Account/SupportDetail/{khieuNai.KhieuNaiId}",
                    isPopup = false
                });
            }

            // ==========================================
            // 4. THÔNG BÁO TÍCH ĐIỂM / HẠNG THÀNH VIÊN
            // ==========================================
            var khachHang = await _context.KhachHangs.FindAsync(currentUserId.Value);
            if (khachHang != null)
            {
                listNotifications.Add(new {
                    id = khachHang.KhachHangId,
                    type = "reward",
                    icon = "bi-gem",
                    badgeClass = "bg-warning text-dark",
                    title = "Cập nhật điểm tích lũy thành viên",
                    content = $"Bạn hiện đang có {khachHang.DiemTichLuy:N0} điểm. Tích cực mua sắm để nhận thêm nhiều ưu đãi!",
                    time = DateTime.Now.ToString("dd/MM/yyyy"),
                    url = "/Notification/RewardPoints",
                    isPopup = false
                });
            }

            return Json(new { success = true, data = listNotifications });
        }

        // =================================================================
        // 3. TRANG TỔNG HỢP DANH SÁCH KHUYẾN MÃI / ƯU ĐÃI NỔI BẬT
        // URL: /Notification/Offers
        // =================================================================
        [HttpGet]
        public async Task<IActionResult> Offers()
        {
            // Lấy danh sách các chương trình khuyến mãi còn hạn sử dụng
            var listKhuyenMai = await _context.KhuyenMais
                .Where(k => k.NgayKetThuc >= DateTime.Now) // Hoặc logic điều kiện khuyến mãi của Hào
                .OrderByDescending(k => k.NgayBatDau)
                .ToListAsync();

            return View(listKhuyenMai);
        }

        // =================================================================
        // 4. TRANG THÔNG BÁO SỰ KIỆN TÍCH ĐIỂM THÀNH VIÊN
        // URL: /Notification/RewardPoints
        // =================================================================
        [HttpGet]
        public async Task<IActionResult> RewardPoints()
        {
            int? currentUserId = GetCurrentKhachHangId();
            if (currentUserId == null)
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để xem thông tin điểm tích lũy thành viên!";
                return RedirectToAction("Login", "Account");
            }

            var khachHang = await _context.KhachHangs.FindAsync(currentUserId.Value);
            if (khachHang == null) return NotFound();

            // Truyền thông tin điểm sang View để làm hiệu ứng thăng hạng thành viên (Vàng, Bạc, Đồng)
            return View(khachHang);
        }

        // =================================================================
        // 5. TRANG THÔNG BÁO LỖI HỆ THỐNG / GIAO DỊCH THẤT BẠI
        // =================================================================
        public IActionResult OrderFailed()
        {
            return View();
        }
    }
}