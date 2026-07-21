using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebWeb.Models;

namespace WebWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin", AuthenticationSchemes = "AdminScheme")]
    public class ThanhToanController : Controller
    {
        private readonly ECommerceDBContext _context;

        public ThanhToanController(ECommerceDBContext context)
        {
            _context = context;
        }

        // 1. TRANG DANH SÁCH CHÍNH (Tab 1: Thanh toán đơn hàng)
        public async Task<IActionResult> Index(string searchTerm)
        {
            var query = _context.GiaoDichThanhToans
                .Include(g => g.DonHangLe)
                    .ThenInclude(d => d.KhachHang) // Đảm bảo nạp KhachHang của đơn lẻ
                .Include(m => m.GoiDangKy)
                    .ThenInclude(d => d.KhachHang) // Đảm bảo nạp KhachHang của gói định kỳ
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                searchTerm = searchTerm.Trim().ToLower();
                query = query.Where(g => g.MaGiaoDichCong.ToLower().Contains(searchTerm) || 
                                    (g.DonHangLe != null && g.DonHangLe.KhachHang != null && g.DonHangLe.KhachHang.HoTen.ToLower().Contains(searchTerm)) ||
                                    (g.GoiDangKy != null && g.GoiDangKy.KhachHang != null && g.GoiDangKy.KhachHang.HoTen.ToLower().Contains(searchTerm))); // Tìm theo khách định kỳ
                ViewBag.SearchTerm = searchTerm;
            }

            return View(await query.OrderByDescending(g => g.NgayGiaoDich).ToListAsync());
        }

        // 2. TAB 2: DANH SÁCH CÔNG NỢ NHÀ VƯỜN (Tính toán qua trung gian PhieuNhapKho)
        public async Task<IActionResult> DanhSachCongNo(string searchTerm)
        {
            // Lấy danh sách nhà vườn cùng toàn bộ phiếu nhập và phiếu chi liên quan
            var queryNhaVuon = _context.NhaVuons
                .Include(nv => nv.PhieuNhapKhos)
                .Include(nv => nv.PhieuChiCongNos)
                .AsQueryable();

            // Tìm kiếm theo tên nhà vườn nếu có
            if (!string.IsNullOrEmpty(searchTerm))
            {
                searchTerm = searchTerm.Trim().ToLower();
                queryNhaVuon = queryNhaVuon.Where(nv => nv.TenNhaVuon.ToLower().Contains(searchTerm));
                ViewBag.SearchTerm = searchTerm;
            }

            var danhSachNhaVuon = await queryNhaVuon.ToListAsync();

            // Duyệt qua từng nhà vườn để tính toán công nợ tích lũy
            var dsCongNoNhaVuon = danhSachNhaVuon.Select(nv => {
                // 1. Tổng tiền từ tất cả các phiếu nhập kho của nhà vườn này
                decimal tongTienNhapKho = nv.PhieuNhapKhos?.Sum(pn => pn.TongTienNhap) ?? 0;

                // 2. Tổng tiền hệ thống đã làm phiếu chi trả cho nhà vườn này
                decimal tongTienDaChiTra = nv.PhieuChiCongNos?.Sum(pc => pc.SoTienThucChi) ?? 0;

                // 3. Công nợ còn lại = Tổng nhập - Tổng chi
                decimal congNoConLai = tongTienNhapKho - tongTienDaChiTra;

                return new {
                    NhaVuon = nv,
                    CongNo = congNoConLai
                };
            })
            // Chỉ hiển thị những nhà vườn nào hệ thống thực sự đang còn nợ tiền (Công nợ > 0)
            .Where(x => x.CongNo > 0)
            .Select(x => new PhieuChiCongNo
            {
                // Gán tạm vào Model PhieuChiCongNo để không lỗi tầng View đã dựng
                PhieuChiId = x.NhaVuon.NhaVuonId, 
                NhaVuonId = x.NhaVuon.NhaVuonId,
                NhaVuon = x.NhaVuon,
                SoTienThucChi = x.CongNo, // Số tiền còn nợ cần chi trả
                NgayLap = DateTime.Now
            })
            .ToList();

            return View(dsCongNoNhaVuon);
        }

        // 3. XEM CHI TIẾT GIAO DỊCH CONG THANH TOÁN (VNPay / MoMo)
        public async Task<IActionResult> DetailsGiaoDich(int id)
        {
            var giaoDich = await _context.GiaoDichThanhToans
                .Include(g => g.DonHangLe)
                    .ThenInclude(d => d.KhachHang)
                .Include(g => g.GoiDangKy)
                    .ThenInclude(gdk => gdk.KhachHang) // Nạp đầy đủ thông tin gói và khách hàng sở hữu gói
                .FirstOrDefaultAsync(g => g.GiaoDichId == id);

            if (giaoDich == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin giao dịch này trên hệ thống!";
                return RedirectToAction(nameof(Index));
            }

            return View(giaoDich);
        }

        // 4. XEM CHI TIẾT PHIẾU CHI CÔNG NỢ
        public async Task<IActionResult> DetailsCongNo(int id)
        {
            var nhaVuon = await _context.NhaVuons
                .Include(nv => nv.PhieuNhapKhos)
                    .ThenInclude(p => p.NhanVien)
                .Include(nv => nv.PhieuChiCongNos)
                    .ThenInclude(pc => pc.NhanVien) // 🔥 BỔ SUNG: Nạp thêm thông tin Nhân viên lập phiếu chi để hiện tên người chi
                .FirstOrDefaultAsync(nv => nv.NhaVuonId == id);

            if (nhaVuon == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy dữ liệu đối tác tương ứng!";
                return RedirectToAction(nameof(DanhSachCongNo));
            }

            // Tính toán lại số tiền để hiển thị bên trang chi tiết đối soát
            decimal tongTienNhap = nhaVuon.PhieuNhapKhos?.Sum(p => p.TongTienNhap) ?? 0;
            decimal tongTienDaChi = nhaVuon.PhieuChiCongNos?.Sum(p => p.SoTienThucChi) ?? 0;
            
            ViewBag.TenNhaVuon = nhaVuon.TenNhaVuon;
            ViewBag.TongTienNhap = tongTienNhap;
            ViewBag.TongTienDaChi = tongTienDaChi;
            ViewBag.CongNoConLai = tongTienNhap - tongTienDaChi;

            // Trả về danh sách các phiếu nhập kho của nhà vườn để liệt kê chi tiết
            var dsPhieuNhap = nhaVuon.PhieuNhapKhos?.OrderByDescending(p => p.NgayLapPhieu).ToList() ?? new List<PhieuNhapKho>();
            return View(dsPhieuNhap);
        }

        // 🔥 TAB 3: DANH SÁCH LỊCH SỬ PHIẾU CHI CÔNG NỢ NHÀ VƯỜN
        public async Task<IActionResult> LichSuPhieuChi(string searchTerm)
        {
            var query = _context.PhieuChiCongNos
                .Include(pc => pc.NhaVuon)   // Nạp thông tin nhà vườn nhận tiền
                .Include(pc => pc.NhanVien)  // Nạp thông tin nhân viên thực hiện chi
                .AsQueryable();

            // Hỗ trợ tìm kiếm theo Tên nhà vườn hoặc Mã giao dịch
            if (!string.IsNullOrEmpty(searchTerm))
            {
                searchTerm = searchTerm.Trim().ToLower();
                query = query.Where(pc => pc.NhaVuon.TenNhaVuon.ToLower().Contains(searchTerm) || 
                                    pc.MaGiaoDich.ToLower().Contains(searchTerm));
            }

            var dsPhieuChi = await query.OrderByDescending(pc => pc.NgayLap).ToListAsync();
            
            // Lưu lại từ khóa tìm kiếm để hiển thị lại trên ô Input ở View
            ViewBag.SearchTerm = searchTerm;

            return View(dsPhieuChi);
}

        // 5. XÁC NHẬN THANH TOÁN CÔNG NỢ (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XacNhanThanhToanCongNo(int id, string maGiaoDich)
        {
            // id nhận vào ở đây chính là NhaVuonId được truyền từ form modal
            var nhaVuon = await _context.NhaVuons
                .Include(nv => nv.PhieuNhapKhos)
                .Include(nv => nv.PhieuChiCongNos)
                .FirstOrDefaultAsync(nv => nv.NhaVuonId == id);

            if (nhaVuon == null) return NotFound();

            // Tính toán lại số tiền thực tế đang nợ tại thời điểm bấm nút
            decimal tongTienNhapKho = nhaVuon.PhieuNhapKhos?.Sum(pn => pn.TongTienNhap) ?? 0;
            decimal tongTienDaChiTra = nhaVuon.PhieuChiCongNos?.Sum(pc => pc.SoTienThucChi) ?? 0;
            decimal congNoHienTai = tongTienNhapKho - tongTienDaChiTra;

            if (congNoHienTai <= 0)
            {
                TempData["ErrorMessage"] = "Nhà vườn này hiện tại đã hết công nợ!";
                return RedirectToAction(nameof(DanhSachCongNo));
            }

            // Tạo mới một phiếu chi để lưu vào lịch sử đối soát hệ thống
            var phieuChiMoi = new PhieuChiCongNo
            {
                NhaVuonId = id,
                NhanVienId = 1, // Hào thay bằng ID nhân viên/Admin đang đăng nhập thực tế nhé
                NgayLap = DateTime.Now,
                SoTienThucChi = congNoHienTai, // Thanh toán dứt điểm toàn bộ số tiền nợ tích lũy
                PhuongThuc = 1, // Mặc định 1: Chuyển khoản ngân hàng
                MaGiaoDich = maGiaoDich?.Trim().ToUpper()
            };

            try
            {
                _context.PhieuChiCongNos.Add(phieuChiMoi);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã lập phiếu chi tất toán thành công {congNoHienTai.ToString("#,##0")} đ cho đối tác {nhaVuon.TenNhaVuon}!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi phát sinh khi lưu phiếu chi: " + ex.Message;
            }

            return RedirectToAction(nameof(DanhSachCongNo));
        }

        // ==========================================================
        // 6. GIAO DIỆN LẬP PHIẾU TẤT TOÁN CÔNG NỢ (GET)
        // ==========================================================
        public async Task<IActionResult> LapPhieuTatToan(int id)
        {
            var nhaVuon = await _context.NhaVuons
                .Include(nv => nv.PhieuNhapKhos)
                .Include(nv => nv.PhieuChiCongNos)
                .FirstOrDefaultAsync(nv => nv.NhaVuonId == id);

            if (nhaVuon == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin đối tác!";
                return RedirectToAction(nameof(DanhSachCongNo));
            }

            // Tính toán số dư nợ hiện tại cần thanh toán
            decimal tongTienNhap = nhaVuon.PhieuNhapKhos?.Sum(p => p.TongTienNhap) ?? 0;
            decimal tongTienDaChi = nhaVuon.PhieuChiCongNos?.Sum(p => p.SoTienThucChi) ?? 0;
            decimal congNoHienTai = tongTienNhap - tongTienDaChi;

            if (congNoHienTai <= 0)
            {
                TempData["ErrorMessage"] = "Đối tác này hiện tại đã hoàn thành hết công nợ!";
                return RedirectToAction(nameof(DanhSachCongNo));
            }

            ViewBag.DanhSachNhanVien = await _context.NhanViens.ToListAsync();
            ViewBag.NhaVuon = nhaVuon;
            ViewBag.CongNoHienTai = congNoHienTai;

            return View();
        }

        // ==========================================================
        // 7. XỬ LÝ LƯU PHIẾU TẤT TOÁN CÔNG NỢ (POST)
        // ==========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LuuPhieuTatToan(int nhaVuonId, decimal soTienThucChi, int phuongThuc, string maGiaoDich)
        {
            var nhaVuon = await _context.NhaVuons.FindAsync(nhaVuonId);
            if (nhaVuon == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy nhà vườn tương ứng.";
                return RedirectToAction(nameof(DanhSachCongNo));
            }

            if (soTienThucChi <= 0)
            {
                TempData["ErrorMessage"] = "Số tiền tất toán phải lớn hơn 0đ!";
                return RedirectToAction(nameof(LapPhieuTatToan), new { id = nhaVuonId });
            }

            // =========================================================================
            // TỰ ĐỘNG LẤY NHÂN VIÊN ID TỪ PHIÊN ĐĂNG NHẬP (CLAIMS PRINCIPAL)
            // =========================================================================
            int currentNhanVienId = 1; // Giá trị dự phòng (mặc định) nếu xảy ra lỗi lấy claim
            
            // Tìm Claim lưu ID nhân viên (thường lưu dưới dạng ClaimTypes.NameIdentifier hoặc "NhanVienId")
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("NhanVienId");
            
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int parsedId))
            {
                currentNhanVienId = parsedId;
            }

            var phieuChi = new PhieuChiCongNo
            {
                NhaVuonId = nhaVuonId,
                NhanVienId = currentNhanVienId, // Gán ID tự động lấy từ phiên đăng nhập
                NgayLap = DateTime.Now,
                SoTienThucChi = soTienThucChi,
                PhuongThuc = phuongThuc, 
                MaGiaoDich = string.IsNullOrWhiteSpace(maGiaoDich) 
                    ? $"PENDING-{nhaVuonId}-{DateTime.Now.ToString("yyyyMMddHHmm")}" 
                    : maGiaoDich.Trim().ToUpper()
            };

            try
            {
                _context.PhieuChiCongNos.Add(phieuChi);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã tất toán và lập phiếu chi thành công số tiền {soTienThucChi.ToString("#,##0")} đ cho đối tác {nhaVuon.TenNhaVuon}!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi hệ thống không thể lập phiếu chi: " + ex.Message;
                return RedirectToAction(nameof(LapPhieuTatToan), new { id = nhaVuonId });
            }

            return RedirectToAction(nameof(DanhSachCongNo));
        }
    }
}