using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebWeb.Models;

namespace WebWeb.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin", AuthenticationSchemes = "AdminScheme")]
    [Area("Admin")]
    public class DanhMucController : Controller
    {
        private readonly ECommerceDBContext _context;

        public DanhMucController(ECommerceDBContext context)
        {
            _context = context;
        }

        // 1. Xem danh sách danh mục (Có tìm kiếm theo tên)
        [HttpGet]
        public async Task<IActionResult> Index(string searchTerm)
        {
            var query = _context.DanhMucs.AsNoTracking().AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(d => d.TenDanhMuc.Contains(searchTerm));
                ViewBag.SearchTerm = searchTerm;
            }

            var list = await query.OrderByDescending(d => d.DanhMucId).ToListAsync();
            return View(list);
        }

        // 2. Chi tiết danh mục
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var danhMuc = await _context.DanhMucs
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.DanhMucId == id);

            if (danhMuc == null) return NotFound();

            return View(danhMuc);
        }

        // 3. Giao diện Thêm mới (GET)
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // Xử lý Thêm mới (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DanhMuc model)
        {
            if (ModelState.IsValid)
            {
                _context.DanhMucs.Add(model);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        // 4. Giao diện Chỉnh sửa (GET)
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var danhMuc = await _context.DanhMucs.FindAsync(id);
            if (danhMuc == null) return NotFound();

            return View(danhMuc);
        }

        // Xử lý Cập nhật (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, DanhMuc model)
        {
            if (id != model.DanhMucId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Entry(model).State = EntityState.Modified;
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.DanhMucs.Any(e => e.DanhMucId == model.DanhMucId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        // 5. Giao diện Xác nhận xóa (GET)
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var danhMuc = await _context.DanhMucs
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.DanhMucId == id);

            if (danhMuc == null) return NotFound();

            return View(danhMuc);
        }

        // Xử lý Thực hiện xóa (POST)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var danhMuc = await _context.DanhMucs.FindAsync(id);
            if (danhMuc != null)
            {
                _context.DanhMucs.Remove(danhMuc);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}