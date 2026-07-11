using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using WebWeb.Models;

namespace WebWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    // Hào có thể thêm Authorize nếu hệ thống yêu cầu bảo mật đăng nhập Admin
    public class ThamSoController : Controller
    {
        private readonly ECommerceDBContext _context;

        public ThamSoController(ECommerceDBContext context)
        {
            _context = context;
        }

        // 1. TRANG DANH SÁCH THAM SỐ HỆ THỐNG
        public async Task<IActionResult> Index()
        {
            var dsThamSo = await _context.ThamSos.ToListAsync();
            return View(dsThamSo);
        }

        // 2. GET: TRANG CHỈNH SỬA GIÁ TRỊ THAM SỐ
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var thamSo = await _context.ThamSos.FirstOrDefaultAsync(t => t.MaThamSo == id);
            if (thamSo == null)
            {
                return NotFound();
            }

            return View(thamSo);
        }

        // 3. POST: XỬ LÝ LƯU THAM SỐ KHI ADMIN BẤM LƯU
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, decimal giaTri, string ghiChu)
        {
            var thamSo = await _context.ThamSos.FirstOrDefaultAsync(t => t.MaThamSo == id);
            if (thamSo == null)
            {
                TempData["Error"] = "Không tìm thấy tham số cần chỉnh sửa!";
                return RedirectToAction(nameof(Index));
            }

            // Tiến hành cập nhật dữ liệu mới khớp với Model ThamSo.cs của Hào
            thamSo.GiaTri = giaTri;
            thamSo.GhiChu = ghiChu; // Cho phép Admin sửa lại ghi chú giải thích nếu muốn

            try
            {
                _context.ThamSos.Update(thamSo);
                await _context.SaveChangesAsync();
                
                TempData["Success"] = $"Cập nhật thành công cấu hình tham số: {id}";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["Error"] = "Có lỗi xảy ra trong quá trình cập nhật database!";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}