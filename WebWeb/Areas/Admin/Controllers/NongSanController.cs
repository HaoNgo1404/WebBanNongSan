using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebWeb.Models;

namespace WebWeb.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin", AuthenticationSchemes = "AdminScheme")]
    [Area("Admin")]
    public class NongSanController : Controller
    {
        private readonly ECommerceDBContext _context;

        public NongSanController(ECommerceDBContext context)
        {
            _context = context;
        }

        // 1. Xem danh sách nông sản
        [HttpGet]
        public async Task<IActionResult> Index(string searchTerm)
        {
            var query = _context.NongSans
                .AsNoTracking()
                .Include(n => n.NhaVuon)
                .Include(n => n.DanhMuc)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(n => n.TenNongSan.Contains(searchTerm));
                ViewBag.SearchTerm = searchTerm;
            }

            var list = await query.OrderByDescending(n => n.NongSanId).ToListAsync();
            return View(list);
        }

        // 2. Chi tiết nông sản
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var nongSan = await _context.NongSans
                .Include(n => n.NhaVuon)
                .Include(n => n.DanhMuc)
                .FirstOrDefaultAsync(m => m.NongSanId == id);

            if (nongSan == null) return NotFound();

            return View(nongSan);
        }

        // 3. Giao diện Thêm mới (GET)
        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.NhaVuonId = new SelectList(_context.NhaVuons, "NhaVuonId", "TenNhaVuon");
            ViewBag.DanhMucId = new SelectList(_context.DanhMucs, "DanhMucId", "TenDanhMuc");
            return View();
        }

        // Xử lý Thêm mới (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(NongSan model)
        {

            if (ModelState.IsValid)
            {
                _context.NongSans.Add(model);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.NhaVuonId = new SelectList(_context.NhaVuons, "NhaVuonId", "TenNhaVuon", model.NhaVuonId);
            ViewBag.DanhMucId = new SelectList(_context.DanhMucs, "DanhMucId", "TenDanhMuc", model.DanhMucId);
            return View(model);
        }

        // 4. Giao diện Chỉnh sửa (GET)
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var nongSan = await _context.NongSans.FindAsync(id);
            if (nongSan == null) return NotFound();

            ViewBag.NhaVuonId = new SelectList(_context.NhaVuons, "NhaVuonId", "TenNhaVuon", nongSan.NhaVuonId);
            ViewBag.DanhMucId = new SelectList(_context.DanhMucs, "DanhMucId", "TenDanhMuc", nongSan.DanhMucId);
            return View(nongSan);
        }

        // Xử lý Cập nhật (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, NongSan model)
        {
            if (id != model.NongSanId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Entry(model).State = EntityState.Modified;
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.NongSans.Any(e => e.NongSanId == model.NongSanId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewBag.NhaVuonId = new SelectList(_context.NhaVuons, "NhaVuonId", "TenNhaVuon", model.NhaVuonId);
            ViewBag.DanhMucId = new SelectList(_context.DanhMucs, "DanhMucId", "TenDanhMuc", model.DanhMucId);
            return View(model);
        }

        // 5. Giao diện Xác nhận xóa
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var nongSan = await _context.NongSans
                .Include(n => n.NhaVuon)
                .Include(n => n.DanhMuc)
                .FirstOrDefaultAsync(m => m.NongSanId == id);

            if (nongSan == null) return NotFound();

            return View(nongSan);
        }

        // Xử lý Thực hiện xóa
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var nongSan = await _context.NongSans.FindAsync(id);
            if (nongSan != null)
            {
                _context.NongSans.Remove(nongSan);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}