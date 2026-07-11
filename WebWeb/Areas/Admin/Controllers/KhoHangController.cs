using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using WebWeb.Models;
using WebWeb.ViewModels;

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
        // MỤC 1: DANH SÁCH TỒN KHO GỘP THEO TỪNG NÔNG SẢN
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Lấy tất cả lô hàng để tiến hành gộp nhóm theo Nông sản ở bộ nhớ (In-Memory)
            var allLoHangs = await _context.LoHangs
                .Include(l => l.NongSan)
                .AsNoTracking()
                .ToListAsync();

            // Gộp nhóm theo NongSanId để tính tổng số lượng tồn kho hiện tại
            var listTonKhoGop = allLoHangs
                .GroupBy(l => l.NongSanId)
                .Select(g => new NongSanTonKhoViewModel
                {
                    NongSanId = g.Key,
                    TenNongSan = g.First().NongSan?.TenNongSan ?? "Không rõ",
                    TongSoLuongTon = g.Sum(l => l.SoLuongTon),
                    SoLuongLoHangActive = g.Count(l => l.SoLuongTon > 0)
                })
                .OrderBy(x => x.TenNongSan)
                .ToList();

            return View(listTonKhoGop);
        }

        // ==========================================
        // MỤC 1.2: CHI TIẾT CÁC LÔ HÀNG CỦA MỘT NÔNG SẢN (Phục vụ Báo cáo hao hụt)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var nongSan = await _context.NongSans.FindAsync(id);
            if (nongSan == null) return NotFound();

            // Lấy danh sách toàn bộ các lô hàng thuộc nông sản này, kể cả lô đã hết hàng để đối soát
            var danhSachLoHang = await _context.LoHangs
                .Include(l => l.PhieuNhapKho)
                    .ThenInclude(p => p.NhaVuon)
                .Where(l => l.NongSanId == id)
                .OrderByDescending(l => l.NgayNhapKho)
                .ToListAsync();

            ViewBag.TenNongSan = nongSan.TenNongSan;
            ViewBag.NongSanId = id;

            return View(danhSachLoHang);
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
                        TrangThaiHsd = ProductStatuses.ConHan
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
                    entity.TrangThaiHsd = model.SoLuongTon == 0 ? ProductStatuses.HetHang : (model.HanSuDung < DateTime.Now ? ProductStatuses.HetHan  : ProductStatuses.ConHan );

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
            
            if (loHang == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy lô hàng cần xóa!";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                _context.LoHangs.Remove(loHang);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã xóa thành công lô hàng #LO-{id} khỏi hệ thống.";
            }
            catch (DbUpdateException)
            {
                // Bắt lỗi ràng buộc dữ liệu khi lô hàng này đã có người mua hoặc có trong báo cáo
                TempData["ErrorMessage"] = $"Không thể xóa lô hàng #LO-{id} vì lô hàng này đã phát sinh dữ liệu hóa đơn hoặc kiểm kê! Vui lòng dùng chức năng 'Sửa' để chỉnh số lượng tồn về 0 thay vì xóa cứng.";
            }

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
            
            if (loHang == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy lô hàng dữ liệu!";
                return RedirectToAction(nameof(LoHangList));
            }

            try
            {
                _context.LoHangs.Remove(loHang);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã xóa thành công lô hàng #LO-{id} khỏi nhật ký.";
            }
            catch (DbUpdateException)
            {
                TempData["ErrorMessage"] = $"Hệ thống từ chối xóa! Lô hàng #LO-{id} đã vướng ràng buộc lịch sử giao dịch nông sản.";
            }

            return RedirectToAction(nameof(LoHangList));
        }

        // ==========================================
        // MỤC 4: QUẢN LÝ BÁO CÁO HAO HỤT / HƯ HỎNG
        // ==========================================

        // 4.1. Lấy danh sách lịch sử các báo cáo hao hụt
        [HttpGet]
        public async Task<IActionResult> DepletionList()
        {
            var listBaoCao = await _context.BaoCaoHaoHuts
                .Include(b => b.NhanVien)
                .Include(b => b.ChiTietBaoCaoHaoHuts)
                    .ThenInclude(ct => ct.LoHang)
                        .ThenInclude(l => l.NongSan)
                .AsNoTracking()
                .OrderByDescending(b => b.NgayLap)
                .ToListAsync();

            return View(listBaoCao);
        }

        // 4.2. Xem chi tiết một chứng từ báo cáo hao hụt
        [HttpGet]
        public async Task<IActionResult> DetailsBaoCao(int id)
        {
            var baoCao = await _context.BaoCaoHaoHuts
                .Include(b => b.NhanVien)
                .Include(b => b.ChiTietBaoCaoHaoHuts)
                    .ThenInclude(ct => ct.LoHang)
                        .ThenInclude(l => l.NongSan)
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.BaoCaoId == id);

            if (baoCao == null) return NotFound();

            return View(baoCao);
        }

        // 4.3. Giao diện Lập báo cáo hao hụt nông sản
        [HttpGet]
        public async Task<IActionResult> CreateBaoCao()
        {
            // Lấy danh sách nhân viên đang hoạt động để gán người lập
            ViewData["NhanVienId"] = new SelectList(_context.NhanViens.Where(n => n.TrangThai == true), "NhanVienId", "HoTen");
            
            // Chỉ lấy các lô hàng còn tồn kho thực tế > 0 để tiến hành báo cáo hao hụt
            var danhSachLoHangActive = await _context.LoHangs
                .Include(l => l.NongSan)
                .Where(l => l.SoLuongTon > 0)
                .OrderByDescending(l => l.NgayNhapKho)
                .Select(l => new
                {
                    LoHangId = l.LoHangId,
                    HienThi = $"Mã Lô: {l.LoHangId} - {l.NongSan.TenNongSan} (Còn tồn: {l.SoLuongTon} - Đơn giá nhập: {l.DonGiaNhap:N0}đ)"
                })
                .ToListAsync();

            ViewData["LoHangId"] = new SelectList(danhSachLoHangActive, "LoHangId", "HienThi");
            return View();
        }

        // 4.4. Xử lý lưu báo cáo hao hụt và cập nhật số lượng tồn kho
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBaoCao(int nhanVienId, string lyDoHaoHut, int loHangId, decimal soLuongHaoHut)
        {
            if (string.IsNullOrWhiteSpace(lyDoHaoHut))
            {
                ModelState.AddModelError("", "Vui lòng nhập lý do hao hụt (Ví dụ: dập nát, hết hạn, biến chất...).");
            }

            var loHang = await _context.LoHangs.FindAsync(loHangId);
            if (loHang == null)
            {
                ModelState.AddModelError("", "Lô hàng được chọn không tồn tại.");
            }
            else if (soLuongHaoHut <= 0 || soLuongHaoHut > loHang.SoLuongTon)
            {
                ModelState.AddModelError("", $"Số lượng hao hụt phải lớn hơn 0 và không được vượt quá số lượng tồn hiện tại ({loHang.SoLuongTon}).");
            }

            if (!ModelState.IsValid)
            {
                // Nạp lại dữ liệu nếu có lỗi dữ liệu đầu vào
                ViewData["NhanVienId"] = new SelectList(_context.NhanViens.Where(n => n.TrangThai == true), "NhanVienId", "HoTen", nhanVienId);
                var danhSachLoHangActive = await _context.LoHangs
                    .Include(l => l.NongSan)
                    .Where(l => l.SoLuongTon > 0)
                    .Select(l => new { l.LoHangId, HienThi = $"Mã Lô: {l.LoHangId} - {l.NongSan.TenNongSan} (Còn tồn: {l.SoLuongTon})" })
                    .ToListAsync();
                ViewData["LoHangId"] = new SelectList(danhSachLoHangActive, "LoHangId", "HienThi", loHangId);
                return View();
            }

            // Tính toán tổng giá trị thiệt hại của đợt hao hụt dựa trên Đơn giá nhập của lô đó
            decimal donGiaHaoHut = loHang!.DonGiaNhap;
            decimal giaTriThietHai = soLuongHaoHut * donGiaHaoHut;

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // 1. Khởi tạo và lưu master chứng từ BaoCaoHaoHut
                    var baoCao = new BaoCaoHaoHut
                    {
                        NhanVienId = nhanVienId,
                        NgayLap = DateTime.Now,
                        LyDoHaoHut = lyDoHaoHut,
                        TongGiaTriThietHai = giaTriThietHai
                    };
                    _context.BaoCaoHaoHuts.Add(baoCao);
                    await _context.SaveChangesAsync(); // Lưu để sinh BaoCaoId tự động

                    // 2. Khởi tạo ChiTietBaoCaoHaoHut tương ứng
                    var chiTiet = new ChiTietBaoCaoHaoHut
                    {
                        BaoCaoId = baoCao.BaoCaoId,
                        LoHangId = loHangId,
                        SoLuongHaoHut = soLuongHaoHut,
                        DonGiaHaoHut = donGiaHaoHut
                    };
                    _context.ChiTietBaoCaoHaoHuts.Add(chiTiet);

                    // 3. Trừ trực tiếp số lượng tồn kho của Lô hàng hiện tại
                    loHang.SoLuongTon -= soLuongHaoHut;
                    
                    // Cập nhật lại trạng thái hạn sử dụng/tồn kho dựa trên logic có sẵn của hệ thống
                    if (loHang.SoLuongTon == 0)
                    {
                        loHang.TrangThaiHsd = "Hết hàng";
                    }

                    _context.LoHangs.Update(loHang);
                    await _context.SaveChangesAsync();

                    // Xác nhận hoàn tất thành công
                    await transaction.CommitAsync();

                    TempData["SuccessMessage"] = $"Đã lập thành công báo cáo hao hụt #{baoCao.BaoCaoId} và trừ kho thành công.";
                    return RedirectToAction(nameof(DepletionList));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    ModelState.AddModelError("", "Đã xảy ra lỗi hệ thống khi xử lý báo cáo: " + ex.Message);
                }
            }

            // Nạp lại dữ liệu nếu transaction thất bại
            ViewData["NhanVienId"] = new SelectList(_context.NhanViens.Where(n => n.TrangThai == true), "NhanVienId", "HoTen", nhanVienId);
            var activeLoHangs = await _context.LoHangs.Include(l => l.NongSan).Where(l => l.SoLuongTon > 0).ToListAsync();
            ViewData["LoHangId"] = new SelectList(activeLoHangs, "LoHangId", "LoHangId", loHangId);
            return View();
        }
    }
}