using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebWeb.Models;

namespace WebWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin", AuthenticationSchemes = "AdminScheme")]
    public class ThanhToanController : Controller
    {
        private readonly ECommerceDBContext _context;

        public ThanhToanController(ECommerceDBContext context)
        {
            _context = context;
        }

        // 1. TRANG DANH SÁCH CHÍNH (Tab 1: Thanh toán đơn hàng)
        public async Task<IActionResult> Index(string searchTerm)
        {
            var query = _context.GiaoDichThanhToans
                .Include(g => g.DonHangLe)
                .ThenInclude(d => d.KhachHang)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                searchTerm = searchTerm.Trim().ToLower();
                query = query.Where(g => g.MaGiaoDichCong.ToLower().Contains(searchTerm) || 
                                    (g.DonHangLe != null && g.DonHangLe.KhachHang != null && g.DonHangLe.KhachHang.HoTen.ToLower().Contains(searchTerm)));
                ViewBag.SearchTerm = searchTerm;
            }

            return View(await query.OrderByDescending(g => g.NgayGiaoDich).ToListAsync());
        }

        // 2. TAB 2: DANH SÁCH CÔNG NỢ NHÀ VƯỜN
        public async Task<IActionResult> DanhSachCongNo(string searchTerm)
        {
            var query = _context.PhieuChiCongNos
                .Include(p => p.NhaVuon)
                .Include(p => p.NhanVien)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                searchTerm = searchTerm.Trim().ToLower();
                query = query.Where(p => p.NhaVuon.TenNhaVuon.ToLower().Contains(searchTerm) || 
                                         (p.MaGiaoDich != null && p.MaGiaoDich.ToLower().Contains(searchTerm)));
                ViewBag.SearchTerm = searchTerm;
            }

            return View(await query.OrderByDescending(p => p.NgayLap).ToListAsync());
        }

        // 3. XEM CHI TIẾT THANH TOÁN ĐƠN HÀNG
        public async Task<IActionResult> DetailsGiaoDich(int id)
        {
            var giaoDich = await _context.GiaoDichThanhToans
                .Include(g => g.DonHangLe)
                .ThenInclude(d => d.KhachHang)
                .FirstOrDefaultAsync(g => g.GiaoDichId == id);

            if (giaoDich == null) return NotFound();
            return View(giaoDich);
        }

        // 4. XEM CHI TIẾT PHIẾU CHI CÔNG NỢ
        public async Task<IActionResult> DetailsCongNo(int id)
        {
            var phieuChi = await _context.PhieuChiCongNos
                .Include(p => p.NhaVuon)
                .Include(p => p.NhanVien)
                .FirstOrDefaultAsync(p => p.PhieuChiId == id);

            if (phieuChi == null) return NotFound();
            return View(phieuChi);
        }

        // 5. XÁC NHẬN THANH TOÁN CÔNG NỢ (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XacNhanThanhToanCongNo(int id, string maGiaoDich)
        {
            var phieuChi = await _context.PhieuChiCongNos.FindAsync(id);
            if (phieuChi == null) return NotFound();

            // Cập nhật mã giao dịch ngân hàng/ví điện tử khi thủ quỹ xác nhận đã chi tiền thành công
            phieuChi.MaGiaoDich = maGiaoDich?.Trim().ToUpper();
            // Nếu hệ thống có trường trạng thái, Hào có thể cập nhật thêm tại đây
            
            _context.Update(phieuChi);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(DanhSachCongNo));
        }
    }
}