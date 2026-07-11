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
        // 1. ĐIỀU HƯỚNG SANG CỔNG VNPAY (XỬ LÝ CẢ ĐƠN LẺ VÀ ĐỊNH KỲ)
        // =================================================================
        [HttpGet]
        public async Task<IActionResult> RedirectToVnPay(int orderId, string type = "le")
        {
            string thoiGianHienTai = DateTime.Now.ToString("yyyyMMddHHmmss");
            string maGiaoDichVnPay = "VNPAY" + thoiGianHienTai;

            if (type == "dinhky")
            {
                // Xử lý cho Gói Đăng Ký Định Kỳ
                var goiKy = await _context.GoiDangKyDinhKies.FindAsync(orderId);
                if (goiKy == null) return NotFound("Không tìm thấy gói đăng ký định kỳ tương ứng với giao dịch này.");

                // Cập nhật trạng thái thanh toán của gói định kỳ
                goiKy.TrangThaiGoi = OrderStatuses.HoatDong;
                _context.GoiDangKyDinhKies.Update(goiKy);

                // Khởi tạo bản ghi giao dịch
                var giaoDich = new GiaoDichThanhToan
                {
                    MaGiaoDichCong = maGiaoDichVnPay,
                    GoiDangKyId = goiKy.GoiId,
                    SoTien = goiKy.TongTienGoi,
                    PhuongThuc = "VNPAY",
                    TrangThai = 1,
                    NgayGiaoDich = DateTime.Now
                };
                _context.GiaoDichThanhToans.Add(giaoDich);
                await _context.SaveChangesAsync();

                return RedirectToAction("OrderPackageSuccess", "Notification", new {orderId = goiKy.GoiId, platform = "VNPAY", amount = goiKy.TongTienGoi, type = "dinhky" });
            }
            else
            {
                // Xử lý mặc định cho Đơn Hàng Lẻ
                var donHang = await _context.DonHangLes.FindAsync(orderId);
                if (donHang == null) return NotFound("Không tìm thấy đơn hàng lẻ tương ứng với giao dịch này.");

                donHang.TrangThaiThanhToan = OrderStatuses.DaThanhToan;
                _context.DonHangLes.Update(donHang);

                var giaoDich = new GiaoDichThanhToan
                {
                    MaGiaoDichCong = maGiaoDichVnPay,
                    DonHangLeId = donHang.DonHangLeId,
                    SoTien = donHang.TongTienTamTinh,
                    PhuongThuc = "VNPAY",
                    TrangThai = 1,
                    NgayGiaoDich = DateTime.Now
                };
                _context.GiaoDichThanhToans.Add(giaoDich);
                await _context.SaveChangesAsync();

                return RedirectToAction("OrderSuccess", "Notification", new {orderId = donHang.DonHangLeId, platform = "VNPAY", amount = donHang.TongTienTamTinh, type = "le" });
            }
        }

        // =================================================================
        // 2. ĐIỀU HƯỚNG SANG CỔNG MOMO (XỬ LÝ CẢ ĐƠN LẺ VÀ ĐỊNH KỲ)
        // =================================================================
        [HttpGet]
        public async Task<IActionResult> RedirectToMoMo(int orderId, string type = "le")
        {
            string thoiGianHienTai = DateTime.Now.ToString("yyyyMMddHHmmss");
            string maGiaoDichMoMo = "MOMO" + thoiGianHienTai;

            if (type == "dinhky")
            {
                // Xử lý cho Gói Đăng Ký Định Kỳ
                var goiKy = await _context.GoiDangKyDinhKies.FindAsync(orderId);
                if (goiKy == null) return NotFound("Không tìm thấy gói đăng ký định kỳ tương ứng với giao dịch này.");

                goiKy.TrangThaiGoi = OrderStatuses.HoatDong;
                _context.GoiDangKyDinhKies.Update(goiKy);

                var giaoDich = new GiaoDichThanhToan
                {
                    MaGiaoDichCong = maGiaoDichMoMo,
                    GoiDangKyId = goiKy.GoiId,
                    SoTien = goiKy.TongTienGoi,
                    PhuongThuc = "MOMO",
                    TrangThai = 1,
                    NgayGiaoDich = DateTime.Now
                };
                _context.GiaoDichThanhToans.Add(giaoDich);
                await _context.SaveChangesAsync();

                return RedirectToAction("OrderPackageSuccess", "Notification", new {orderId = goiKy.GoiId, platform = "MOMO", amount = goiKy.TongTienGoi, type = "dinhky" });
            }
            else
            {
                // Xử lý mặc định cho Đơn Hàng Lẻ
                var donHang = await _context.DonHangLes.FindAsync(orderId);
                if (donHang == null) return NotFound("Không tìm thấy đơn hàng lẻ tương ứng với giao dịch này.");

                donHang.TrangThaiThanhToan = OrderStatuses.DaThanhToan;
                _context.DonHangLes.Update(donHang);

                var giaoDich = new GiaoDichThanhToan
                {
                    MaGiaoDichCong = maGiaoDichMoMo,
                    DonHangLeId = donHang.DonHangLeId,
                    SoTien = donHang.TongTienTamTinh,
                    PhuongThuc = "MOMO",
                    TrangThai = 1,
                    NgayGiaoDich = DateTime.Now
                };
                _context.GiaoDichThanhToans.Add(giaoDich);
                await _context.SaveChangesAsync();

                return RedirectToAction("OrderSuccess", "Notification", new {orderId = donHang.DonHangLeId, platform = "MOMO", amount = donHang.TongTienTamTinh, type = "le" });
            }
        }
    }
}