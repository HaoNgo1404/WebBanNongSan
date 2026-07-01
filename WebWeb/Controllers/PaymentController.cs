using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using WebWeb.Models;

namespace WebWeb.Controllers
{
    public class PaymentController : Controller
    {
        private readonly ECommerceDBContext _context;

        public PaymentController(ECommerceDBContext context)
        {
            _context = context;
        }

        // =================================================================
        // 1. ĐIỀU HƯỚNG SANG CỔNG VNPAY
        // URL: /Payment/RedirectToVnPay?orderId=...&type=...
        // =================================================================
        [HttpGet]
        public async Task<IActionResult> RedirectToVnPay(int orderId, string type)
        {
            decimal soTienThanhToan = 0;

            if (type == "GoiDinhKy")
            {
                var goiId = await _context.GoiDangKyDinhKies.FindAsync(orderId);
                if (goiId == null) return NotFound("Không tìm thấy gói định kỳ!");
                soTienThanhToan = goiId.TongTienGoi;
            }
            else // DonHangLe
            {
                var donHang = await _context.DonHangLes.FindAsync(orderId);
                if (donHang == null) return NotFound("Không tìm thấy đơn hàng lẻ!");
                soTienThanhToan = donHang.TongTienTamTinh;
            }

            // TODO: Triển khai logic tạo chuỗi URL tích hợp SDK VNPay thực tế ở đây
            // Hiện tại giả lập chuyển hướng thành công sang trang thông báo:
            return RedirectToAction("OrderSuccess", "Notification", new { orderId = orderId });
        }

        // =================================================================
        // 2. ĐIỀU HƯỚNG SANG CỔNG MOMO (MỚI BỔ SUNG)
        // URL: /Payment/RedirectToMoMo?orderId=...&type=...
        // =================================================================
        [HttpGet]
        public async Task<IActionResult> RedirectToMoMo(int orderId, string type)
        {
            decimal soTienThanhToan = 0;

            if (type == "GoiDinhKy")
            {
                var goiId = await _context.GoiDangKyDinhKies.FindAsync(orderId);
                if (goiId == null) return NotFound("Không tìm thấy gói định kỳ!");
                soTienThanhToan = goiId.TongTienGoi;
            }
            else // DonHangLe
            {
                var donHang = await _context.DonHangLes.FindAsync(orderId);
                if (donHang == null) return NotFound("Không tìm thấy đơn hàng lẻ!");
                soTienThanhToan = donHang.TongTienTamTinh;
            }

            // TODO: Triển khai cấu hình tham số MoMo (PartnerCode, AccessKey, SecretKey...)
            // Tạo Chữ ký điện tử (Signature) và gọi API MoMo để lấy QR/URL thanh toán
            // Hiện tại giả lập chuyển hướng thành công sang trang thông báo:
            return RedirectToAction("OrderSuccess", "Notification", new { orderId = orderId });
        }
    }
}