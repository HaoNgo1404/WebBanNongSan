using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebWeb.Models;

namespace WebWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin", AuthenticationSchemes = "AdminScheme")]
    public class NhanVienController : Controller
    {
        private readonly ECommerceDBContext _context;

        public NhanVienController(ECommerceDBContext context)
        {
            _context = context;
        }

        // 1. DANH SÁCH NHÂN VIÊN (INDEX)
        public async Task<IActionResult> Index(string searchTerm)
        {
            var query = _context.NhanViens.AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(k => k.HoTen.Contains(searchTerm) || 
                                         k.VaiTro.TenVaiTro.Contains(searchTerm) || 
                                         k.Email.Contains(searchTerm));
                ViewBag.SearchTerm = searchTerm;
            }
            var danhSachNhanVien = await _context.NhanViens
                .Include(n => n.VaiTro) // Nạp thông tin vai trò để hiển thị tên quyền
                .OrderByDescending(n => n.NhanVienId)
                .ToListAsync();
            return View(danhSachNhanVien);
        }

        public async Task<IActionResult> Details(int id)
        {
            var nhanVien = await _context.NhanViens
                .Include(n => n.VaiTro) // Đổ thông tin bảng quyền hạn liên kết
                .Include(n => n.PhieuNhapKhos) // Nạp thêm lịch sử lập phiếu nhập kho (nếu cần)
                .Include(n => n.DonHangLes) // Nạp thêm lịch sử xử lý đơn hàng
                .FirstOrDefaultAsync(m => m.NhanVienId == id);

            if (nhanVien == null)
            {
                return NotFound();
            }

            return View(nhanVien);
        }

        // 2. THÊM MỚI NHÂN VIÊN (GET)
        public async Task<IActionResult> Create()
        {
            // Lấy danh sách vai trò để đổ vào thẻ Select Dropdown trên giao diện
            ViewBag.VaiTroId = new SelectList(await _context.VaiTroPhanQuyens.ToListAsync(), "VaiTroId", "TenVaiTro");
            return View();
        }

        // 3. THÊM MỚI NHÂN VIÊN (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(NhanVien nhanVien)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra trùng lặp email hệ thống
                var emailExists = await _context.NhanViens.AnyAsync(n => n.Email == nhanVien.Email);
                if (emailExists)
                {
                    ModelState.AddModelError("Email", "Email này đã được sử dụng bởi một tài khoản khác!");
                    ViewBag.VaiTroId = new SelectList(await _context.VaiTroPhanQuyens.ToListAsync(), "VaiTroId", "TenVaiTro", nhanVien.VaiTroId);
                    return View(nhanVien);
                }

                // Mặc định tài khoản mới tạo sẽ có trạng thái hoạt động (true)
                nhanVien.TrangThai = true;

                _context.NhanViens.Add(nhanVien);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã thêm thành công nhân viên: {nhanVien.HoTen}!";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.VaiTroId = new SelectList(await _context.VaiTroPhanQuyens.ToListAsync(), "VaiTroId", "TenVaiTro", nhanVien.VaiTroId);
            return View(nhanVien);
        }

        // 4. CHỈNH SỬA THÔNG TIN (GET)
        public async Task<IActionResult> Edit(int id)
        {
            var nhanVien = await _context.NhanViens.FindAsync(id);
            if (nhanVien == null) return NotFound();

            ViewBag.VaiTroId = new SelectList(await _context.VaiTroPhanQuyens.ToListAsync(), "VaiTroId", "TenVaiTro", nhanVien.VaiTroId);
            return View(nhanVien);
        }

        // 5. CHỈNH SỬA THÔNG TIN (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, NhanVien model)
        {
            if (id != model.NhanVienId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var nhanVienGoc = await _context.NhanViens.FindAsync(id);
                    if (nhanVienGoc == null) return NotFound();

                    // Cập nhật các thông tin cho phép thay đổi
                    nhanVienGoc.HoTen = model.HoTen;
                    nhanVienGoc.Email = model.Email;
                    nhanVienGoc.VaiTroId = model.VaiTroId;
                    nhanVienGoc.TrangThai = model.TrangThai;

                    // Nếu Admin có nhập mật khẩu mới thì thay đổi, không thì giữ nguyên mật khẩu cũ
                    if (!string.IsNullOrEmpty(model.MatKhau))
                    {
                        nhanVienGoc.MatKhau = model.MatKhau; 
                    }

                    _context.Update(nhanVienGoc);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Đã cập nhật thông tin nhân viên {model.HoTen} thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.NhanViens.AnyAsync(e => e.NhanVienId == model.NhanVienId)) return NotFound();
                    else throw;
                }
            }

            ViewBag.VaiTroId = new SelectList(await _context.VaiTroPhanQuyens.ToListAsync(), "VaiTroId", "TenVaiTro", model.VaiTroId);
            return View(model);
        }

        // 6. XÓA NHÂN VIÊN (Dùng AJAX POST ngầm để không mất dữ liệu liên kết cứng)
        public async Task<IActionResult> Delete(int id)
        {
            var nhanVien = await _context.NhanViens
                .Include(n => n.VaiTro)
                .FirstOrDefaultAsync(m => m.NhanVienId == id);

            if (nhanVien == null)
            {
                return NotFound();
            }

            return View(nhanVien);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var nhanVien = await _context.NhanViens.FindAsync(id);
            if (nhanVien == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy nhân viên cần xóa trên hệ thống!";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                // Thử nghiệm xóa trực tiếp khỏi Cơ sở dữ liệu
                _context.NhanViens.Remove(nhanVien);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = $"Đã xóa vĩnh viễn tài khoản nhân viên {nhanVien.HoTen} khỏi hệ thống!";
            }
            catch (Exception)
            {
                // Xử lý cơ chế dự phòng: Nếu dính dữ liệu lịch sử (Khóa ngoại), tự động khóa tài khoản
                nhanVien.TrangThai = false;
                _context.NhanViens.Update(nhanVien);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Do nhân viên {nhanVien.HoTen} đã có lịch sử chứng từ/đơn hàng liên quan, hệ thống tự động chuyển tài khoản sang trạng thái 'Ngừng hoạt động' để bảo toàn dữ liệu!";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}