using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebWeb.Models;

namespace WebWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin", AuthenticationSchemes = "AdminScheme")]
    public class KhuyenMaiController : Controller
    {
        private readonly ECommerceDBContext _context;

        public KhuyenMaiController(ECommerceDBContext context)
        {
            _context = context;
        }

        // 1. DANH SÁCH CHƯƠNG TRÌNH KHUYẾN MÃI
        public async Task<IActionResult> Index(string searchTerm)
        {
            var query = _context.KhuyenMais.AsQueryable();
            if (!string.IsNullOrEmpty(searchTerm))
            {
                searchTerm = searchTerm.Trim().ToLower();
                query = query.Where(k => k.TenChuongTrinh.ToLower().Contains(searchTerm) || 
                                         k.VoucherCode.ToLower().Contains(searchTerm));
                ViewBag.SearchTerm = searchTerm;
            }
            return View(await query.OrderByDescending(k => k.NgayBatDau).ToListAsync());
        }

        // 2. CHI TIẾT
        public async Task<IActionResult> Details(int id)
        {
            var khuyenMai = await _context.KhuyenMais.FirstOrDefaultAsync(k => k.KhuyenMaiId == id);
            if (khuyenMai == null) return NotFound();
            return View(khuyenMai);
        }

        // 3. TẠO MỚI (GET)
        public IActionResult Create()
        {
            return View(new KhuyenMai { NgayBatDau = DateTime.Now, NgayKetThuc = DateTime.Now.AddMonths(1), TrangThai = true });
        }

        // 4. TẠO MỚI (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(KhuyenMai km)
        {
            if (ModelState.IsValid)
            {
                km.VoucherCode = km.VoucherCode.ToUpper().Trim();
                km.SoLuotDaDung = 0;
                _context.Add(km);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(km);
        }

        // 5. CHỈNH SỬA (GET)
        public async Task<IActionResult> Edit(int id)
        {
            var khuyenMai = await _context.KhuyenMais.FindAsync(id);
            if (khuyenMai == null) return NotFound();
            return View(khuyenMai);
        }

        // 6. CHỈNH SỬA (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, KhuyenMai km)
        {
            if (id != km.KhuyenMaiId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    km.VoucherCode = km.VoucherCode.ToUpper().Trim();
                    _context.Update(km);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.KhuyenMais.Any(e => e.KhuyenMaiId == km.KhuyenMaiId)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(km);
        }

        // 7. XÓA (GET)
        public async Task<IActionResult> Delete(int id)
        {
            var khuyenMai = await _context.KhuyenMais.FirstOrDefaultAsync(k => k.KhuyenMaiId == id);
            if (khuyenMai == null) return NotFound();
            return View(khuyenMai);
        }

        // 8. XÓA (POST)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var khuyenMai = await _context.KhuyenMais.FindAsync(id);
            if (khuyenMai != null)
            {
                _context.KhuyenMais.Remove(khuyenMai);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}