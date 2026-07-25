using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Linq;
using System.Threading.Tasks;
using WebWeb.Models;

namespace WebWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin", AuthenticationSchemes = "AdminScheme")]
    public class ThongKeController : Controller
    {
        private readonly ECommerceDBContext _context;

        public ThongKeController(ECommerceDBContext context)
        {
            _context = context;
        }

        // 1. Trang tổng quan thống kê
        public async Task<IActionResult> Index(DateTime? tuNgay, DateTime? denNgay)
        {
            var nats = DateTime.Now;
            var start = tuNgay ?? new DateTime(nats.Year, nats.Month, 1);
            var end = denNgay ?? nats;

            ViewBag.TuNgay = start.ToString("yyyy-MM-dd");
            ViewBag.DenNgay = end.ToString("yyyy-MM-dd");

            // Doanh thu đơn lẻ
            var doanhThuDonLe = await _context.DonHangLes
                .Where(d => d.NgayDat >= start && d.NgayDat <= end && d.TrangThaiThanhToan == OrderStatuses.DaThanhToan
                        && d.TrangThaiDonHang == OrderStatuses.HoanThanh)
                .SumAsync(d => (decimal?)d.TongTienThucTe ?? d.TongTienTamTinh);

            // Doanh thu gói định kỳ
            var doanhThuGoiDinhKy = await _context.GoiDangKyDinhKies
                .Where(g => g.NgayBatDau >= start && g.NgayBatDau <= end && g.TrangThaiGoi == OrderStatuses.DaThanhToan)
                .SumAsync(g => g.TongTienGoi);

            // Số khách hàng mới
            var tongKhachHang = await _context.KhachHangs
                .CountAsync(k => k.NgayDangKy >= start && k.NgayDangKy <= end);

            // Top nông sản bán chạy (Bổ sung NongSanId & TongTien)
            var topNongSan = await _context.ChiTietDonHangLes
                .Where(ct => ct.DonHangLe.NgayDat >= start 
                        && ct.DonHangLe.NgayDat <= end 
                        && ct.DonHangLe.TrangThaiThanhToan == OrderStatuses.DaThanhToan
                        && ct.DonHangLe.TrangThaiDonHang == OrderStatuses.HoanThanh)
                .GroupBy(ct => new { ct.NongSanId, ct.NongSan.TenNongSan })
                .Select(g => new TopNongSanViewModel
                {
                    NongSanId = g.Key.NongSanId,
                    TenNongSan = g.Key.TenNongSan,
                    SoLuongBan = g.Sum(x => x.SoLuongDat),
                    TongTien = g.Sum(x => x.SoLuongDat * x.DonGiaThoiDiem) // Bổ sung tính tổng tiền
                })
                .OrderByDescending(x => x.SoLuongBan)
                .Take(5)
                .ToListAsync();

            // Tỷ lệ đơn hàng thành công
            var tongSoDonHang = await _context.DonHangLes.CountAsync(d => d.NgayDat >= start && d.NgayDat <= end);
            var soDonThanhCong = await _context.DonHangLes.CountAsync(d => d.NgayDat >= start && d.NgayDat <= end && d.TrangThaiThanhToan == OrderStatuses.DaThanhToan && d.TrangThaiDonHang == OrderStatuses.HoanThanh);

            double tyLeHoanThanh = 0;
            if (tongSoDonHang > 0)
            {
                tyLeHoanThanh = Math.Round(((double)soDonThanhCong / tongSoDonHang) * 100, 1);
            }

            ViewBag.TyLeHoanThanh = tyLeHoanThanh;  
            ViewBag.DoanhThuDonLe = doanhThuDonLe;
            ViewBag.DoanhThuGoiDinhKy = doanhThuGoiDinhKy;
            ViewBag.TongDoanhThu = doanhThuDonLe + doanhThuGoiDinhKy;
            ViewBag.TongKhachHang = tongKhachHang;
            ViewBag.TopNongSan = topNongSan;

            return View();
        }

        // 2. Action AJAX lấy danh sách đơn hàng chứa Nông sản được click
        [HttpGet]
        public async Task<IActionResult> GetDonHangByNongSan(int nongSanId, DateTime? tuNgay, DateTime? denNgay)
        {
            var start = tuNgay ?? new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var end = denNgay ?? DateTime.Now;

            var dsDonHang = await _context.ChiTietDonHangLes
                .Where(ct => ct.NongSanId == nongSanId && 
                             ct.DonHangLe.NgayDat >= start && 
                             ct.DonHangLe.NgayDat <= end &&
                             ct.DonHangLe.TrangThaiDonHang == OrderStatuses.HoanThanh)
                .Select(ct => new 
                {
                    DonHangLeId = ct.DonHangLeId,
                    MaDonHang = "DH-" + ct.DonHangLeId.ToString("D5"),
                    TenKhachHang = ct.DonHangLe.KhachHang != null ? ct.DonHangLe.KhachHang.HoTen : ct.DonHangLe.NameCusNonAccount,
                    NgayDat = ct.DonHangLe.NgayDat.ToString("dd/MM/yyyy HH:mm"),
                    SoLuongMua = ct.SoLuongDat,
                    ThanhTienItem = ct.SoLuongDat * ct.DonGiaThoiDiem,
                    TrangThaiDonHang = ct.DonHangLe.TrangThaiDonHang,
                    TrangThaiThanhToan = ct.DonHangLe.TrangThaiThanhToan
                })
                .OrderByDescending(x => x.DonHangLeId)
                .ToListAsync();

            return Json(dsDonHang);
        }
    }

    // ViewModel bổ trợ cho Top Nông sản
    public class TopNongSanViewModel
    {
        public int NongSanId { get; set; } // Bổ sung thêm Id để định danh khi click
        public string? TenNongSan { get; set; }
        public int SoLuongBan { get; set; }
        public decimal TongTien { get; set; } // Bổ sung tổng tiền
    }
}