using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using WebWeb.Models;

namespace WebWeb.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin", AuthenticationSchemes = "AdminScheme")]
    [Area("Admin")]
    public class KhoHangController : Controller
    {
        private readonly ECommerceDBContext _context;

        public KhoHangController(ECommerceDBContext context)
        {
            _context = context;
        }

        // ==========================================
        // MỤC 1: DANH SÁCH NÔNG SẢN TỒN KHO HÌNH THỨC HIỆN TẠI
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var listTonKho = await _context.LoHangs
                .Include(l => l.NongSan)
                .Include(l => l.PhieuNhapKho)
                    .ThenInclude(p => p.NhaVuon)
                .AsNoTracking()
                .OrderByDescending(l => l.LoHangId)
                .ToListAsync();

            return View(listTonKho);
        }

        // ==========================================
        // MỤC 2: NHẬT KÝ CHI TIẾT CÁC LÔ HÀNG ĐÃ NHẬP
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> LoHangList()
        {
            var listLoHang = await _context.LoHangs
                .Include(l => l.NongSan)
                .Include(l => l.PhieuNhapKho)
                    .ThenInclude(p => p.NhaVuon)
                .AsNoTracking()
                .OrderByDescending(l => l.LoHangId)
                .ToListAsync();

            return View(listLoHang);
        }

        // Xem hồ sơ chi tiết một lô hàng
        [HttpGet]
        public async Task<IActionResult> DetailsLoHang(int id)
        {
            var loHang = await _context.LoHangs
                .Include(l => l.NongSan)
                .Include(l => l.PhieuNhapKho)
                    .ThenInclude(p => p.NhaVuon)
                .Include(l => l.PhieuNhapKho)
                    .ThenInclude(p => p.NhanVien)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.LoHangId == id);

            if (loHang == null) return NotFound();

            return View(loHang);
        }

        // ==========================================
        // MỤC 3: TIẾN HÀNH LẬP CHỨNG TỪ & XÁC NHẬN NHẬP KHO
        // ==========================================
        [HttpGet]
        public IActionResult NhapKho()
        {
            // Load dữ liệu đổ vào các SelectList trên giao diện form nhập kho
            ViewData["NhanVienId"] = new SelectList(_context.NhanViens.Where(n => n.TrangThai == true), "NhanVienId", "HoTen");
            ViewData["NhaVuonId"] = new SelectList(_context.NhaVuons, "NhaVuonId", "TenNhaVuon");
            ViewData["NongSanId"] = new SelectList(_context.NongSans, "NongSanId", "TenNongSan");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NhapKho(int nhanVienId, int nhaVuonId, int nongSanId, decimal soLuongNhap, decimal donGiaNhap, DateTime ngayNhap, DateTime? ngayHetHan)
        {
            if (soLuongNhap <= 0 || donGiaNhap <= 0)
            {
                ModelState.AddModelError("", "Số lượng nhập và đơn giá nhập phải lớn hơn 0.");
                ViewData["NhanVienId"] = new SelectList(_context.NhanViens.Where(n => n.TrangThai == true), "NhanVienId", "HoTen", nhanVienId);
                ViewData["NhaVuonId"] = new SelectList(_context.NhaVuons, "NhaVuonId", "TenNhaVuon", nhaVuonId);
                ViewData["NongSanId"] = new SelectList(_context.NongSans, "NongSanId", "TenNongSan", nongSanId);
                return View();
            }

            // Tính toán tổng tiền của phiếu nhập kho này
            decimal tongTien = soLuongNhap * donGiaNhap;

            // Sử dụng Transaction để đảm bảo tính toàn vẹn (Hoặc tạo cả hai đồng thời qua Entity Framework)
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // 1. Khởi tạo và lưu Phiếu Nhập Kho trước
                    var phieuNhap = new PhieuNhapKho
                    {
                        NhaVuonId = nhaVuonId,
                        NhanVienId = nhanVienId,
                        NgayLapPhieu = ngayNhap,
                        TongTienNhap = tongTien
                    };
                    _context.PhieuNhapKhos.Add(phieuNhap);
                    await _context.SaveChangesAsync(); // Lưu để sinh ra tự động PhieuNhapId

                    // 2. Khởi tạo Lô Hàng liên kết với Phiếu Nhập Kho vừa tạo
                    var loHang = new LoHang
                    {
                        PhieuNhapId = phieuNhap.PhieuNhapId, // Khóa ngoại kết nối
                        NongSanId = nongSanId,
                        DonGiaNhap = donGiaNhap,
                        SoLuongNhap = soLuongNhap,
                        SoLuongTon = soLuongNhap, // Lúc mới nhập thì Tồn kho thực tế = Số lượng nhập vào
                        NgayNhapKho = ngayNhap,
                        NgayThuHoach = ngayNhap.AddDays(-2), // Mặc định lùi lại 2 ngày làm mẫu hoặc để trống
                        HanSuDung = ngayHetHan ?? ngayNhap.AddDays(7), // Nếu không nhập HSD thì mặc định là 7 ngày
                        TrangThaiHsd = "Còn hạn"
                    };
                    _context.LoHangs.Add(loHang);
                    await _context.SaveChangesAsync();

                    // Xác nhận transaction thành công hoàn toàn
                    await transaction.CommitAsync();

                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    ModelState.AddModelError("", "Đã xảy ra lỗi hệ thống trong quá trình lưu dữ liệu: " + ex.Message);
                }
            }

            // Nếu thất bại, nạp lại dữ liệu cho DropdownList để người dùng không bị mất form
            ViewData["NhanVienId"] = new SelectList(_context.NhanViens.Where(n => n.TrangThai == true), "NhanVienId", "HoTen", nhanVienId);
            ViewData["NhaVuonId"] = new SelectList(_context.NhaVuons, "NhaVuonId", "TenNhaVuon", nhaVuonId);
            ViewData["NongSanId"] = new SelectList(_context.NongSans, "NongSanId", "TenNongSan", nongSanId);
            return View();
        }

        // ==========================================
        // CÁC THAO TÁC PHỤ: CHỈNH SỬA NHANH & XÓA LÔ HÀNG
        // ==========================================
        
        // Chỉnh sửa nhanh thông tin số lượng tồn kho hoặc HSD
        [HttpGet]
        public async Task<IActionResult> EditTonKho(int id)
        {
            var loHang = await _context.LoHangs
                .Include(l => l.PhieuNhapKho)
                .FirstOrDefaultAsync(m => m.LoHangId == id);

            if (loHang == null) return NotFound();

            ViewData["NongSanId"] = new SelectList(_context.NongSans, "NongSanId", "TenNongSan", loHang.NongSanId);
            return View(loHang);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTonKho(int id, LoHang model)
        {
            if (id != model.LoHangId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var entity = await _context.LoHangs.FindAsync(id);
                    if (entity == null) return NotFound();

                    // Cập nhật các trường cho phép chỉnh sửa nhanh
                    entity.SoLuongTon = model.SoLuongTon;
                    entity.DonGiaNhap = model.DonGiaNhap;
                    entity.HanSuDung = model.HanSuDung;
                    entity.TrangThaiHsd = model.SoLuongTon == 0 ? "Hết hàng" : (model.HanSuDung < DateTime.Now ? "Hết hạn" : "Còn hạn");

                    _context.Update(entity);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.LoHangs.Any(e => e.LoHangId == model.LoHangId)) return NotFound();
                    else throw;
                }
            }
            ViewData["NongSanId"] = new SelectList(_context.NongSans, "NongSanId", "TenNongSan", model.NongSanId);
            return View(model);
        }

        // GET: Admin/KhoHang/DeleteTonKho/5
        [HttpGet]
        public async Task<IActionResult> DeleteTonKho(int id)
        {
            var loHang = await _context.LoHangs
                .Include(l => l.NongSan)
                .Include(l => l.PhieuNhapKho)
                    .ThenInclude(p => p.NhaVuon)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.LoHangId == id);

            if (loHang == null) return NotFound();

            return View(loHang);
        }

        // POST: Admin/KhoHang/DeleteTonKho/5
        [HttpPost, ActionName("DeleteTonKho")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTonKhoConfirmed(int id)
        {
            var loHang = await _context.LoHangs.FindAsync(id);
            if (loHang != null)
            {
                _context.LoHangs.Remove(loHang);
                await _context.SaveChangesAsync();
            }
            // Xóa xong thì quay về danh sách tồn kho Mục 1
            return RedirectToAction(nameof(Index)); 
        }

        // Thao tác xóa lô hàng khỏi hệ thống quản lý
        [HttpGet]
        public async Task<IActionResult> DeleteLoHang(int id)
        {
            var loHang = await _context.LoHangs
                .Include(l => l.NongSan)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.LoHangId == id);
            if (loHang == null) return NotFound();

            return View(loHang);
        }

        [HttpPost, ActionName("DeleteLoHang")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteLoHangConfirmed(int id)
        {
            var loHang = await _context.LoHangs.FindAsync(id);
            if (loHang != null)
            {
                _context.LoHangs.Remove(loHang);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(LoHangList));
        }
    }
}