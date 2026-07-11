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

            // Lấy tối đa 5 đơn hàng mới nhất để làm thông báo tiến độ thực tế
            var danhSachDonHang = await _context.DonHangLes
                .Where(d => d.KhachHangId == currentUserId.Value)
                .OrderByDescending(d => d.NgayDat)
                .Take(5)
                .Select(d => new {
                    orderId = d.DonHangLeId,
                    trangThai = d.TrangThaiDonHang,
                    ngayDat = d.NgayDat.ToString("dd/MM/yyyy")
                })
                .ToListAsync();

            return Json(new { success = true, data = danhSachDonHang });
        }

        // =================================================================
        // 3. TRANG TỔNG HỢP DANH SÁCH KHUYẾN MÃI / ƯU ĐÃI NỔI BẬT
        // URL: /Notification/Offers
        // =================================================================
        [HttpGet]
        public IActionResult Offers()
        {
            // Trong thực tế, bạn có thể bổ sung bảng `KhuyenMai` trong DB để nạp dữ liệu lên.
            // Hiện tại, ta giả lập dữ liệu tĩnh để Hào thiết kế giao diện View đẹp mắt:
            ViewBag.Message = "Danh sách chương trình ưu đãi tri ân khách hàng tháng này!";
            
            return View();
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