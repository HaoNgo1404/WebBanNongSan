using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using WebWeb.Models;

namespace WebWeb.Controllers
{
    [Area("Shipper")]
    [Authorize(Roles = "Shipper", AuthenticationSchemes = "ShipperScheme")]
    public class ShipperController : Controller
    {
        private readonly ECommerceDBContext _context;

        public ShipperController(ECommerceDBContext context)
        {
            _context = context;
        }

        // ==========================================================
        // 1. DANH SÁCH ĐƠN HÀNG LẺ VÀ ĐỢT GIAO ĐỊNH KỲ "CHỜ XỬ LÝ"
        // ==========================================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Lấy các đơn hàng lẻ đang chờ giao
            var danhSachDonHang = await _context.DonHangLes
                .Where(d => d.TrangThaiDonHang == OrderStatuses.ChoXuLy)
                .OrderByDescending(d => d.NgayDat)
                .ToListAsync();

            // Lấy các đợt giao định kỳ đang chờ giao (Hào kiểm tra lại tên bảng DotGiaoDinhKies hoặc DotGiaoDinhKy nhé)
            var danhSachDotGiao = await _context.DotGiaoDinhKies
                .Include(dg => dg.Goi) // Để lấy thông tin địa chỉ/sđt gói tổng nếu cần
                .Where(dg => dg.TrangThaiGiao == OrderStatuses.HoatDong)
                .OrderBy(dg => dg.NgayGiaoThucTe)
                .ToListAsync();

            // Đẩy cả 2 danh sách ra View qua ViewBag để làm giao diện Tab phân loại
            ViewBag.DanhSachDonHangLe = danhSachDonHang;
            ViewBag.DanhSachDotGiaoDinhKy = danhSachDotGiao;

            return View();
        }

        // ==========================================================
        // 2. DANH SÁCH ĐƠN HÀNG MÀ SHIPPER NÀY ĐÃ NHẬN GIAO (ĐANG GIAO)
        // ==========================================================
        [HttpGet]
        public async Task<IActionResult> MyOrders()
        {
            string shipperIdClaim = User.FindFirst("ShipperId")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(shipperIdClaim) || !int.TryParse(shipperIdClaim, out int shipperId))
            {
                return RedirectToAction("Login", "Account");
            }

            // Lấy đơn lẻ đang đi giao của shipper này
            var donHangLeDangGiao = await _context.DonHangLes
                .Where(d => d.NhanVienId == shipperId && d.TrangThaiDonHang == OrderStatuses.DangGiao)
                .OrderByDescending(d => d.NgayDat)
                .ToListAsync();

            // Lấy đợt giao định kỳ đang đi giao của shipper này
            var dotGiaoDangGiao = await _context.DotGiaoDinhKies
                .Include(dg => dg.Goi)
                .Where(dg => dg.NhanVienId == shipperId && dg.TrangThaiGiao == OrderStatuses.DangGiao)
                .OrderBy(dg => dg.NgayGiaoThucTe)
                .ToListAsync();

            ViewBag.DonHangLeDangGiao = donHangLeDangGiao;
            ViewBag.DotGiaoDangGiao = dotGiaoDangGiao;

            return View();
        }

        // ==========================================================
        // 3. CHI TIẾT ĐƠN HÀNG LẺ
        // ==========================================================
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var donHang = await _context.DonHangLes
                .Include(d => d.KhachHang)
                .Include(d => d.ChiTietDonHangLes)
                .ThenInclude(c => c.NongSan)
                .FirstOrDefaultAsync(m => m.DonHangLeId == id);

            if (donHang == null) return NotFound();

            return View(donHang);
        }

        // ==========================================================
        // 4. CHI TIẾT ĐỢT GIAO ĐỊNH KỲ
        // ==========================================================
        [HttpGet]
        public async Task<IActionResult> DetailsDotGiao(int id)
        {
            var dotGiao = await _context.DotGiaoDinhKies
                .Include(dg => dg.Goi)
                .ThenInclude(g => g.KhachHang)
                .FirstOrDefaultAsync(dg => dg.DotGiaoId == id);

            if (dotGiao == null) return NotFound();

            return View(dotGiao);
        }

        // ==========================================================
        // 5. SHIPPER NHẬN ĐƠN (HỖ TRỢ CẢ HAI LOẠI ĐƠN)
        // ==========================================================
        [HttpPost]
        public async Task<IActionResult> NhanDonHang(int id, string type)
        {
            // Lấy ID tài xế đăng nhập
            string shipperIdClaim = User.FindFirst("ShipperId")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(shipperIdClaim) || !int.TryParse(shipperIdClaim, out int shipperId))
            {
                return RedirectToAction("Login", "Account");
            }

            if (type == "dinhky")
            {
                // 1. LUỒNG ĐỢT GIAO ĐỊNH KỲ
                var dotGiao = await _context.DotGiaoDinhKies.FindAsync(id);
                if (dotGiao == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy đợt giao định kỳ này!";
                    return RedirectToAction(nameof(Index));
                }

                dotGiao.NhanVienId = shipperId;
                dotGiao.TrangThaiGiao = OrderStatuses.DangGiao; // Hoặc trạng thái chuẩn của hệ thống

                _context.DotGiaoDinhKies.Update(dotGiao);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Bạn đã nhận lịch đợt giao định kỳ #{id} thành công!";
                // SỬA TẠI ĐÂY: Quay lại trang danh sách thay vì return Content
                return RedirectToAction(nameof(Index));
            }
            else
            {
                // 2. LUỒNG ĐƠN HÀNG LẺ
                var donHang = await _context.DonHangLes.FindAsync(id);
                if (donHang == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy đơn hàng lẻ này!";
                    return RedirectToAction(nameof(Index));
                }

                donHang.NhanVienId = shipperId;
                donHang.TrangThaiDonHang = OrderStatuses.DangGiao; // Hoặc OrderStatuses.DangGiao hàng

                _context.DonHangLes.Update(donHang);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Bạn đã nhận đơn hàng lẻ #{id} thành công!";
                // SỬA TẠI ĐÂY: Quay lại trang danh sách thay vì return Content
                return RedirectToAction(nameof(Index));
            }
        }

        // ==========================================================
        // 6. CẬP NHẬT TRẠNG THÁI GIAO HÀNG (CÓ TÍCH ĐIỂM THƯỞNG CHO ĐỊNH KỲ)
        // ==========================================================
        [HttpPost]
        public async Task<IActionResult> CapNhatTrangThai(int id, string trangThaiMoi, string trangThaiThanhToanMoi, string type = "le")
        {
            if (type == "dinhky")
            {
                var dotGiao = await _context.DotGiaoDinhKies
                    .Include(dg => dg.Goi)
                    .FirstOrDefaultAsync(dg => dg.DotGiaoId == id);

                if (dotGiao == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy đợt giao định kỳ!";
                    return RedirectToAction(nameof(MyOrders));
                }

                dotGiao.TrangThaiGiao = trangThaiMoi;

                // TÍCH ĐIỂM THƯỞNG KHI ĐỢT GIAO HOÀN THÀNH
                if (trangThaiMoi == OrderStatuses.HoanThanh && dotGiao.Goi != null)
                {
                    var khachHang = await _context.KhachHangs.FindAsync(dotGiao.Goi.KhachHangId);
                    if (khachHang != null)
                    {
                        // Giả sử mỗi đợt giao hoàn thành của gói định kỳ được thưởng điểm cố định (ví dụ: 5 điểm)
                        // Hoặc tính dựa trên giá trị gói tổng tùy Hào thiết kế:
                        int diemThuong = (int)(dotGiao.Goi.TongTienGoi / 100000); 
                        if (diemThuong > 0)
                        {
                            khachHang.DiemTichLuy += diemThuong;
                            _context.KhachHangs.Update(khachHang);
                        }
                    }
                }

                _context.DotGiaoDinhKies.Update(dotGiao);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã cập nhật đợt giao định kỳ #{id} thành công.";
                return RedirectToAction(nameof(DetailsDotGiao), new { id = id });
            }
            else
            {
                // LUỒNG ĐƠN HÀNG LẺ (Giữ nguyên của Hào và tối ưu hóa điều hướng)
                var donHang = await _context.DonHangLes.FindAsync(id);
                if (donHang == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy đơn hàng lẻ!";
                    return RedirectToAction(nameof(MyOrders));
                }

                if(donHang.PhuongThucThanhToan == "COD")
                {
                    donHang.TrangThaiDonHang = trangThaiMoi;
                    donHang.TrangThaiThanhToan = trangThaiThanhToanMoi;
                }
                else
                {
                    donHang.TrangThaiDonHang = trangThaiMoi;
                    
                }

                if (trangThaiMoi == OrderStatuses.HoanThanh && trangThaiThanhToanMoi == OrderStatuses.DaThanhToan)
                {
                    var khachHang = await _context.KhachHangs.FindAsync(donHang.KhachHangId);
                    if (khachHang != null)
                    {
                        int diemDuocCong = (int)(donHang.TongTienTamTinh / 100000);
                        if (diemDuocCong > 0)
                        {
                            khachHang.DiemTichLuy += diemDuocCong;
                            _context.KhachHangs.Update(khachHang);
                        }
                    }
                }

                _context.DonHangLes.Update(donHang);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã cập nhật trạng thái đơn hàng #{id} thành công.";
                return RedirectToAction(nameof(Details), new { id = id });
            }
        }
        
        // ==========================================================
        // 7. LỊCH SỬ GIAO HÀNG (ĐƠN LẺ & ĐỢT ĐỊNH KỲ ĐÃ HOÀN THÀNH / HỦY)
        // ==========================================================
        [HttpGet]
        public async Task<IActionResult> LichSuGiao()
        {
            string shipperIdClaim = User.FindFirst("ShipperId")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(shipperIdClaim) || !int.TryParse(shipperIdClaim, out int shipperId))
            {
                return RedirectToAction("Login", "Account");
            }

            // Lấy danh sách lịch sử đơn lẻ (Hoàn thành hoặc Đã hủy) của shipper này
            var lichSuDonLe = await _context.DonHangLes
                .Where(d => d.NhanVienId == shipperId && 
                    (d.TrangThaiDonHang == OrderStatuses.HoanThanh || d.TrangThaiDonHang == OrderStatuses.DaHuy))
                .OrderByDescending(d => d.NgayDat)
                .ToListAsync();

            // Lấy danh sách lịch sử đợt định kỳ (Hoàn thành hoặc Đã hủy) của shipper này
            var lichSuDinhKy = await _context.DotGiaoDinhKies
                .Include(dg => dg.Goi)
                .ThenInclude(g => g.KhachHang)
                .Where(dg => dg.NhanVienId == shipperId && 
                    (dg.TrangThaiGiao == OrderStatuses.HoanThanh || dg.TrangThaiGiao == OrderStatuses.DaHuy))
                .OrderByDescending(dg => dg.NgayGiaoThucTe)
                .ToListAsync();

            ViewBag.LichSuDonLe = lichSuDonLe;
            ViewBag.LichSuDinhKy = lichSuDinhKy;

            return View();
        }
    }
}