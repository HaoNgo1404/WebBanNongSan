using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System.Threading.Tasks;
using WebWeb.Models; // Sửa lại đúng tên DbContext của dự án Hào
using WebWeb.Areas.Admin.ViewModels;
using System.Collections.Generic;

namespace WebWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin", AuthenticationSchemes = "AdminScheme")]
    public class DashboardController : Controller
    {
        private readonly ECommerceDBContext _context; // Hào thay bằng tên DBContext thật (ví dụ: MyDbContext)

        public DashboardController(ECommerceDBContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int? year)
        {
            int selectedYear = year ?? DateTime.Now.Year;
            var model = new DashboardViewModel();

            // 1. TÍNH TỔNG DOANH THU (Giao dịch thành công, TrangThai = 1)
            var giaoDichThanhCong = await _context.GiaoDichThanhToans
                .Where(g => g.TrangThai == 1)
                .ToListAsync();

            model.TongDoanhThu = giaoDichThanhCong.Sum(g => g.SoTien);

            // 2. ĐẾM SỐ ĐƠN HÀNG MỚI (Ví dụ: Đơn hàng lẻ đặt trong tháng hiện tại)
            model.SoDonHangMoi = await _context.DonHangLes
                .CountAsync(d => d.NgayDat.Month == DateTime.Now.Month && d.NgayDat.Year == DateTime.Now.Year);

            // 3. ĐẾM TỔNG SỐ KHÁCH HÀNG
            model.SoKhachHang = await _context.KhachHangs.CountAsync();

            // 4. THỐNG KÊ DOANH THU 12 THÁNG TRONG NĂM (Dùng cho biểu đồ cột)
            for (int month = 1; month <= 12; month++)
            {
                model.LabelThangs.Add($"Tháng {month}");
                var doanhThuThang = giaoDichThanhCong
                    .Where(g => g.NgayGiaoDich.Month == month && g.NgayGiaoDich.Year == selectedYear)
                    .Sum(g => g.SoTien);
                model.DataDoanhThuThang.Add(doanhThuThang);
            }

            // 5. TOP 5 NÔNG SẢN BÁN CHẠY (Dùng cho biểu đồ tròn)
            var topNongSan = await _context.ChiTietDonHangLes
                .Include(c => c.NongSan)
                .GroupBy(c => new { c.NongSanId, c.NongSan.TenNongSan })
                .Select(g => new
                {
                    TenNongSan = g.Key.TenNongSan,
                    TongSoLuong = g.Sum(c => c.SoLuongDat)
                })
                .OrderByDescending(x => x.TongSoLuong)
                .Take(5)
                .ToListAsync();

            foreach (var item in topNongSan)
            {
                model.TopNongSanNames.Add(item.TenNongSan);
                model.TopNongSanQuantities.Add(item.TongSoLuong);
            }

            // 6. TÍNH TOÁN LỢI NHUẬN TẠM TÍNH 
            // Lấy giá nhập trung bình của từng nông sản để tính giá vốn
            var giaNhapNongSan = await _context.LoHangs
                .GroupBy(l => l.NongSanId)
                .Select(g => new { NongSanId = g.Key, GiaNhapMtr = g.Average(l => l.DonGiaNhap) })
                .ToDictionaryAsync(x => x.NongSanId, x => x.GiaNhapMtr);

            decimal tongGiaVon = 0;
            var chiTietDonHangs = await _context.ChiTietDonHangLes.ToListAsync();
            foreach (var ct in chiTietDonHangs)
            {
                if (giaNhapNongSan.ContainsKey(ct.NongSanId))
                {
                    tongGiaVon += ct.SoLuongDat * giaNhapNongSan[ct.NongSanId];
                }
            }
            model.TongLoiNhuan = model.TongDoanhThu - tongGiaVon;
            if (model.TongLoiNhuan < 0) model.TongLoiNhuan = 0; // Tránh hiển thị âm nếu dữ liệu mẫu chưa khớp

            ViewBag.SelectedYear = selectedYear;
            return View(model);
        }
    }
}