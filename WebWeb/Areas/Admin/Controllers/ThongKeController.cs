using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Linq;
using System.Threading.Tasks;
using WebWeb.Models; // Đảm bảo đúng namespace chứa DbContext của bạn

namespace WebWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin", AuthenticationSchemes = "AdminScheme")]
    public class ThongKeController : Controller
    {
        private readonly ECommerceDBContext _context; // Thay YourDbContext bằng tên DbContext thực tế của bạn

        public ThongKeController(ECommerceDBContext context)
        {
            _context = context;
        }

        // Trang tổng quan thống kê
        public async Task<IActionResult> Index(DateTime? tuNgay, DateTime? denNgay)
        {
            // Thiết lập ngày mặc định nếu không truyền vào (ví dụ: tháng hiện tại)
            var nats = DateTime.Now;
            var start = tuNgay ?? new DateTime(nats.Year, nats.Month, 1);
            var end = denNgay ?? nats;

            ViewBag.TuNgay = start.ToString("yyyy-MM-dd");
            ViewBag.DenNgay = end.ToString("yyyy-MM-dd");

            // 1. Thống kê tổng doanh thu đơn hàng lẻ (đã thanh toán thành công)
            var doanhThuDonLe = await _context.DonHangLes
                .Where(d => d.NgayDat >= start && d.NgayDat <= end && d.TrangThaiThanhToan == "Đã thanh toán")
                .SumAsync(d => (decimal?)d.TongTienThucTe ?? d.TongTienTamTinh);

            // 2. Thống kê tổng doanh thu gói đăng ký định kỳ
            var doanhThuGoiDinhKy = await _context.GoiDangKyDinhKies
                .Where(g => g.NgayBatDau >= start && g.NgayBatDau <= end && g.TrangThaiGoi == "Đã thanh toán")
                .SumAsync(g => g.TongTienGoi);

            // 3. Tổng số lượng khách hàng mới đăng ký trong khoảng thời gian
            var tongKhachHang = await _context.KhachHangs
                .CountAsync(k => k.NgayDangKy >= start && k.NgayDangKy <= end);

            // 4. Lấy danh sách nông sản bán chạy nhất (Đơn lẻ)
            var topNongSan = await _context.ChiTietDonHangLes
                .Where(ct => ct.DonHangLe.NgayDat >= start && ct.DonHangLe.NgayDat <= end && ct.DonHangLe.TrangThaiThanhToan == "Đã thanh toán")
                .GroupBy(ct => new { ct.NongSanId, ct.NongSan.TenNongSan })
                .Select(g => new TopNongSanViewModel
                {
                    TenNongSan = g.Key.TenNongSan,
                    SoLuongBan = g.Sum(x => x.SoLuongDat)
                })
                .OrderByDescending(x => x.SoLuongBan)
                .Take(5)
                .ToListAsync();

            // Truyền dữ liệu sang View thông qua ViewBag hoặc ViewModel
            ViewBag.DoanhThuDonLe = doanhThuDonLe;
            ViewBag.DoanhThuGoiDinhKy = doanhThuGoiDinhKy;
            ViewBag.TongDoanhThu = doanhThuDonLe + doanhThuGoiDinhKy;
            ViewBag.TongKhachHang = tongKhachHang;
            ViewBag.TopNongSan = topNongSan;

            return View();
        }
    }

    // Dùng ViewModel bổ trợ để hứng dữ liệu nông sản bán chạy
    public class TopNongSanViewModel
    {
        public string TenNongSan { get; set; }
        public int SoLuongBan { get; set; }
    }
}