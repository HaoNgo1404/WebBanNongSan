using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebWeb.Models;

namespace WebWeb.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin", AuthenticationSchemes = "AdminScheme")]
    [Area("Admin")]
    public class NhaVuonController : Controller
    {
        private readonly ECommerceDBContext _context;

        public NhaVuonController(ECommerceDBContext context)
        {
            _context = context;
        }

        // 1. Xem danh sách nhà vườn (Có tìm kiếm theo Tên hoặc Số điện thoại)
        [HttpGet]
        public async Task<IActionResult> Index(string searchTerm)
        {
            var query = _context.NhaVuons.AsNoTracking().AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(n => n.TenNhaVuon.Contains(searchTerm) || n.SoDienThoai.Contains(searchTerm));
                ViewBag.SearchTerm = searchTerm;
            }

            var list = await query.OrderByDescending(n => n.NhaVuonId).ToListAsync();
            return View(list);
        }

        // 2. Chi tiết nhà vườn
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var nhaVuon = await _context.NhaVuons
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.NhaVuonId == id);

            if (nhaVuon == null) return NotFound();

            return View(nhaVuon);
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
        public async Task<IActionResult> Create(NhaVuon model)
        {
            if (ModelState.IsValid)
            {
                _context.NhaVuons.Add(model);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        // 4. Giao diện Chỉnh sửa (GET)
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var nhaVuon = await _context.NhaVuons.FindAsync(id);
            if (nhaVuon == null) return NotFound();

            return View(nhaVuon);
        }

        // Xử lý Cập nhật (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, NhaVuon model)
        {
            if (id != model.NhaVuonId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Entry(model).State = EntityState.Modified;
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.NhaVuons.Any(e => e.NhaVuonId == model.NhaVuonId)) return NotFound();
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
            var nhaVuon = await _context.NhaVuons
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.NhaVuonId == id);

            if (nhaVuon == null) return NotFound();

            return View(nhaVuon);
        }

        // Xử lý Thực hiện xóa (POST)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var nhaVuon = await _context.NhaVuons.FindAsync(id);
            if (nhaVuon != null)
            {
                _context.NhaVuons.Remove(nhaVuon);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}