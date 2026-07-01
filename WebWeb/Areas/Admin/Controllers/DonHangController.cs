using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebWeb.Models;

namespace WebWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin", AuthenticationSchemes = "AdminScheme")]
    public class DonHangController : Controller
    {
        private readonly ECommerceDBContext _context;

        public DonHangController(ECommerceDBContext context)
        {
            _context = context;
        }

        // 1. DANH SÁCH ĐƠN HÀNG LẺ
        public async Task<IActionResult> Index()
        {
            var danhSachDonHang = await _context.DonHangLes
                .Include(d => d.KhachHang)
                .OrderByDescending(d => d.NgayDat)
                .ToListAsync();
            return View(danhSachDonHang);
        }

        // 2. CHI TIẾT ĐƠN HÀNG LẺ
        public async Task<IActionResult> Details(int id)
        {
            var donHang = await _context.DonHangLes
                .Include(d => d.KhachHang)
                .Include(d => d.DiaChi)
                .Include(d => d.ChiTietDonHangLes).ThenInclude(ct => ct.NongSan)
                .FirstOrDefaultAsync(d => d.DonHangLeId == id);

            if (donHang == null) return NotFound();

            return View(donHang);
        }

        // 3. DANH SÁCH ĐƠN HÀNG ĐỊNH KỲ
        public async Task<IActionResult> IndexDinhKy()
        {
            var danhSachGoi = await _context.GoiDangKyDinhKies
                .Include(g => g.KhachHang)
                .Include(g => g.DiaChi)
                .ToListAsync();
            return View(danhSachGoi);
        }

        // 4. CHI TIẾT ĐƠN HÀNG ĐỊNH KỲ
        public async Task<IActionResult> DetailsDinhKy(int id)
        {
            var goiDangKy = await _context.GoiDangKyDinhKies
                .Include(g => g.KhachHang)
                .Include(g => g.DiaChi)
                .Include(g => g.ChiTietGoiDinhKies).ThenInclude(ct => ct.NongSan)
                .Include(g => g.DotGiaoDinhKies)
                .FirstOrDefaultAsync(g => g.GoiId == id);

            if (goiDangKy == null) return NotFound();

            return View(goiDangKy);
        }

        // =================================================================
        // XÓA ĐƠN HÀNG LẺ (GET - Hiển thị trang xác nhận)
        // =================================================================
        public async Task<IActionResult> Delete(int id)
        {
            // Nạp thêm thông tin khách hàng và địa chỉ để hiển thị trên trang xác nhận
            var donHang = await _context.DonHangLes
                .Include(d => d.KhachHang)
                .Include(d => d.DiaChi)
                .FirstOrDefaultAsync(m => m.DonHangLeId == id);

            if (donHang == null)
            {
                return NotFound();
            }

            return View(donHang);
        }

        // =================================================================
        // XÓA ĐƠN HÀNG LẺ (POST - Thực hiện xóa chính thức)
        // =================================================================
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var donHang = await _context.DonHangLes.FindAsync(id);
            if (donHang == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn hàng cần xóa!";
                return RedirectToAction(nameof(Index));
            }

            // Sử dụng Transaction để đảm bảo an toàn dữ liệu khi xóa nhiều bảng cùng lúc
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // 1. Xóa tất cả Chi tiết đơn hàng thuộc đơn hàng này trước
                    var chiTiets = await _context.ChiTietDonHangLes
                        .Where(ct => ct.DonHangLeId == id)
                        .ToListAsync();
                        
                    if (chiTiets.Any())
                    {
                        _context.ChiTietDonHangLes.RemoveRange(chiTiets);
                    }

                    // 2. Xóa bản ghi đơn hàng chính
                    _context.DonHangLes.Remove(donHang);
                    
                    // 3. Lưu thay đổi và hoàn tất Transaction
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    TempData["SuccessMessage"] = $"Xóa thành công đơn hàng #{id}!";
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    TempData["ErrorMessage"] = "Lỗi hệ thống khi thực hiện xóa đơn hàng: " + ex.Message;
                }
            }

            return RedirectToAction(nameof(Index));
        }
    }
}