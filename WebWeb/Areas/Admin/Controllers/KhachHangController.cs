using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebWeb.Models;

namespace WebWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin", AuthenticationSchemes = "AdminScheme")]
    public class KhachHangController : Controller
    {
        private readonly ECommerceDBContext _context;

        public KhachHangController(ECommerceDBContext context)
        {
            _context = context;
        }

        // Hiển thị danh sách khách hàng (Có hỗ trợ tìm kiếm theo tên hoặc SĐT)
        public async Task<IActionResult> Index(string searchTerm)
        {
            var query = _context.KhachHangs.AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(k => k.HoTen.Contains(searchTerm) || 
                                         k.SoDienThoai.Contains(searchTerm) || 
                                         k.Email.Contains(searchTerm));
                ViewBag.SearchTerm = searchTerm;
            }

            var danhSach = await query.OrderByDescending(k => k.NgayDangKy).ToListAsync();
            return View(danhSach);
        }

        // Xem chi tiết hồ sơ khách hàng, sổ địa chỉ và lịch sử đơn hàng
        public async Task<IActionResult> Details(int id)
        {
            var khachHang = await _context.KhachHangs
                .Include(k => k.SoDiaChis)
                .Include(k => k.DonHangLes)
                .Include(k => k.GoiDangKyDinhKies)
                .FirstOrDefaultAsync(k => k.KhachHangId == id);

            if (khachHang == null)
            {
                return NotFound();
            }

            return View(khachHang);
        }

        // 3. CHỈNH SỬA HỒ SƠ KHÁCH HÀNG (GET)
        public async Task<IActionResult> Edit(int id)
        {
            var khachHang = await _context.KhachHangs.FindAsync(id);
            if (khachHang == null) return NotFound();
            return View(khachHang);
        }

        // 4. CHỈNH SỬA HỒ SƠ KHÁCH HÀNG (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, KhachHang kh)
        {
            if (id != kh.KhachHangId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var existing = await _context.KhachHangs.FindAsync(id);
                    if (existing == null) return NotFound();

                    // Chỉ cập nhật các trường quản trị được phép sửa
                    existing.HoTen = kh.HoTen;
                    existing.SoDienThoai = kh.SoDienThoai;
                    existing.Email = kh.Email;
                    existing.DiemTichLuy = kh.DiemTichLuy;

                    _context.Update(existing);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.KhachHangs.Any(e => e.KhachHangId == kh.KhachHangId)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(kh);
        }

        // =================================================================
        // 5. XÓA KHÁCH HÀNG (GET - Hiển thị trang xác nhận xóa nếu cần)
        // =================================================================
        public async Task<IActionResult> Delete(int id)
        {
            var khachHang = await _context.KhachHangs
                .Include(k => k.DonHangLes) // Bao gồm đơn hàng để kiểm tra
                .FirstOrDefaultAsync(m => m.KhachHangId == id);

            if (khachHang == null)
            {
                return NotFound();
            }

            return View(khachHang);
        }

        // =================================================================
        // 6. XÓA KHÁCH HÀNG (POST - Thực hiện xóa chính thức)
        // =================================================================
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var khachHang = await _context.KhachHangs.FindAsync(id);
            if (khachHang == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy khách hàng cần xóa!";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                // Kiểm tra ràng buộc dữ liệu: Nếu khách hàng này đã có đơn hàng lẻ, 
                // việc xóa trực tiếp có thể gây lỗi khóa ngoại (Foreign Key Constraint) trong SQL Server
                var coDonHang = await _context.DonHangLes.AnyAsync(d => d.KhachHangId == id);
                var coGoiDinhKy = await _context.GoiDangKyDinhKies.AnyAsync(g => g.KhachHangId == id);

                if (coDonHang || coGoiDinhKy)
                {
                    // Cách 1: Chặn không cho xóa (An toàn dữ liệu kế toán/doanh thu)
                    TempData["ErrorMessage"] = $"Không thể xóa khách hàng {khachHang.HoTen} vì tài khoản này đã có lịch sử đặt hàng/gói định kỳ!";
                    return RedirectToAction(nameof(Index));

                    /* // Cách 2: Nếu Hào vẫn muốn ép xóa bằng được, bạn phải giải phóng khóa ngoại trước:
                    var donHangs = await _context.DonHangLes.Where(d => d.KhachHangId == id).ToListAsync();
                    foreach(var dh in donHangs) {
                        dh.KhachHangId = null; // Chuyển các đơn hàng đó thành đơn vãng lai (ẩn danh)
                    }
                    */
                }

                // Xóa các dữ liệu phụ thuộc khác trước (ví dụ: Sổ địa chỉ)
                var danhSachDiaChi = await _context.SoDiaChis.Where(s => s.KhachHangId == id).ToListAsync();
                if (danhSachDiaChi.Any())
                {
                    _context.SoDiaChis.RemoveRange(danhSachDiaChi);
                }

                // Thực hiện xóa tài khoản khách hàng khỏi DB
                _context.KhachHangs.Remove(khachHang);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Xóa tài khoản khách hàng thành công!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi hệ thống khi thực hiện xóa: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }
    }
}