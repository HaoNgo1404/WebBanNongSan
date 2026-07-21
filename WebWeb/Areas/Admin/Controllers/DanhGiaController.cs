using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using WebWeb.Models;

namespace WebWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin", AuthenticationSchemes = "AdminScheme")]
    public class DanhGiaController : Controller
    {
        private readonly ECommerceDBContext _context;

        public DanhGiaController(ECommerceDBContext context)
        {
            _context = context;
        }

        // ==========================================================
        // 1. TAB 1: DANH SÁCH ĐÁNH GIÁ SẢN PHẨM
        // ==========================================================
        public async Task<IActionResult> Index(string searchTerm)
        {
            var query = _context.DanhGiaSanPhams.AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(n => n.NongSan.TenNongSan.Contains(searchTerm));
                ViewBag.SearchTerm = searchTerm;
            }
            var dsNongSanReview = await _context.NongSans
                .Include(ns => ns.DanhGiaSanPhams)
                .ToListAsync();

            var tatCaReviews = await _context.DanhGiaSanPhams.ToListAsync();
            ViewBag.TongSoReviewSanPham = tatCaReviews.Count;
            ViewBag.DiemTrungBinhToanBo = tatCaReviews.Any() ? tatCaReviews.Average(r => r.SoSao) : 0;
            ViewBag.DanhSachSanPham = dsNongSanReview;

            return View();
        }

        // ==========================================================
        // 2. TAB 2: DANH SÁCH ĐÁNH GIÁ ĐƠN HÀNG (ĐÃ KHẮC PHỤC LỖI TRÙNG LẶP)
        // ==========================================================
        public async Task<IActionResult> IndexDonHang(string searchTerm)
        {
            var query = _context.DanhGiaSanPhams.AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(n => n.DonHangLeId.ToString().Contains(searchTerm) ||
                                         n.KhachHang.HoTen.Contains(searchTerm) ||
                                         n.NgayDanhGia.ToString("dd/MM/yyyy").Contains(searchTerm));
                ViewBag.SearchTerm = searchTerm;
            }
            // 1. Lấy danh sách đánh giá từ DB lên
            var tatCaReviewDonHang = await _context.DanhGiaSanPhams
                .Include(r => r.DonHangLe)
                    .ThenInclude(dh => dh.KhachHang)
                .Where(r => r.DonHangLeId == r.DonHangLe.DonHangLeId) // Chỉ lấy các đánh giá có liên kết với đơn hàng
                .ToListAsync();

            // 2. Sử dụng LINQ trên bộ nhớ (LINQ to Objects) GroupBy theo DonHangLeId 
            // để gộp các sản phẩm chung một đơn hàng lại làm một dòng duy nhất
            var dsGopDonHang = tatCaReviewDonHang
                .GroupBy(r => r.DonHangLeId)
                .Select(g => new 
                {
                    DonHangLeId = g.Key,
                    // Lấy thông tin đơn hàng và khách hàng đại diện công tâm
                    DonHang = g.First().DonHangLe,
                    TenKhachHang = g.First().DonHangLe?.KhachHang?.HoTen ?? "Khách vãng lai",
                    NgayDat = g.First().DonHangLe?.NgayDat,
                    
                    // Tính điểm trung bình số sao của tất cả sản phẩm trong đơn hàng này
                    DiemTrungBinhStar = g.Average(r => r.SoSao),
                    
                    // Gom toàn bộ nội dung bình luận của các sản phẩm lại thành một chuỗi duy nhất, phân cách bằng dấu xuống dòng
                    NoiDungBinhLuanGop = string.Join(" | ", g.Select(r => r.BinhLuan).Distinct())
                })
                .OrderByDescending(x => x.NgayDat)
                .ToList();

            // Tính toán tổng số liệu thống kê chung cho toàn tab\r
            ViewBag.TongSoReviewDonHang = dsGopDonHang.Count;
            ViewBag.DiemTrungBinhDonHang = dsGopDonHang.Any() ? dsGopDonHang.Average(x => x.DiemTrungBinhStar) : 0;
            
            // Truyền danh sách đã dọn dẹp sạch trùng lặp sang cho giao diện hiển thị
            ViewBag.DanhSachReviewDonHang = dsGopDonHang;

            return View();
        }

        // Thêm vào trong file DanhGiaController.cs
        public async Task<IActionResult> DetailsDanhGia(int id)
        {
            var chiTietDG = await _context.DanhGiaSanPhams
                .Include(dg => dg.DonHangLe)
                .Include(dg => dg.KhachHang)
                .FirstOrDefaultAsync(dg => dg.DanhGiaId == id);

            if (chiTietDG == null)
            {
                TempData["Error"] = "Không tìm thấy dữ liệu chi tiết của bài đánh giá này!";
                return RedirectToAction(nameof(IndexDonHang));
            }

            return View(chiTietDG);
        }

        // ==========================================================
        // 3. TAB 3: DANH SÁCH KHIẾU NẠI ĐƠN HÀNG
        // ==========================================================
        public async Task<IActionResult> IndexKhieuNai(string searchTerm)
        {
            var query = _context.KhieuNais.AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(n => n.KhieuNaiId.ToString().Contains(searchTerm) ||
                                         n.KhachHang.HoTen.Contains(searchTerm) ||
                                         n.DonHangLeId.ToString().Contains(searchTerm) ||
                                         n.NgayGui.ToString("dd/MM/yyyy").Contains(searchTerm));
                ViewBag.SearchTerm = searchTerm;
            }
            var dsKhieuNai = await _context.KhieuNais
                .Include(kn => kn.KhachHang)
                .Include(kn => kn.DonHangLe)
                .OrderByDescending(kn => kn.NgayGui)
                .ToListAsync();

            return View(dsKhieuNai);
        }

        // ==========================================================
        // 4. CHỨC NĂNG XÓA ĐÁNH GIÁ (Dùng chung cho cả Tab 1 và Tab 2)
        // ==========================================================
        [HttpPost]
        public async Task<IActionResult> DeleteReview(int id, string fromAction = "Index")
        {
            var review = await _context.DanhGiaSanPhams.FindAsync(id);
            if (review == null)
            {
                TempData["Error"] = "Không tìm thấy bài đánh giá cần xóa!";
                return RedirectToAction(fromAction);
            }

            try
            {
                _context.DanhGiaSanPhams.Remove(review);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã xóa bài đánh giá thành công!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi khi xóa dữ liệu: " + ex.Message;
            }

            return RedirectToAction(fromAction);
        }

        // ==========================================================
        // 5. ĐIỀU HƯỚNG VÀ XỬ LÝ ĐƠN KHIẾU NẠI
        // ==========================================================
        public async Task<IActionResult> XuLyKhieuNai(int id)
        {
            var khieuNai = await _context.KhieuNais
                .Include(kn => kn.KhachHang)
                .Include(kn => kn.DonHangLe)
                .FirstOrDefaultAsync(kn => kn.KhieuNaiId == id);

            if (khieuNai == null) return NotFound();

            return View(khieuNai);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateKhieuNai(int khieuNaiId, string phuongAnXuLy, decimal soTienHoan, int trangThai, int? dotGiaoId)
        {
            var khieuNai = await _context.KhieuNais.FindAsync(khieuNaiId);
            if (khieuNai == null)
            {
                TempData["Error"] = "Không tìm thấy đơn khiếu nại tương ứng!";
                return RedirectToAction(nameof(IndexKhieuNai));
            }

            var employeeIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value 
                          ?? User.FindFirst("NhanVienId")?.Value 
                          ?? User.FindFirst("AdminId")?.Value;
            
            if (!string.IsNullOrEmpty(employeeIdClaim) && int.TryParse(employeeIdClaim, out int employeeId))
            {
                khieuNai.NhanVienId = employeeId; // Lưu vết nhân viên xử lý đơn
            }

            if (dotGiaoId.HasValue && dotGiaoId.Value > 0)
            {
                khieuNai.DotGiaoId = dotGiaoId.Value;
            }

            khieuNai.PhuongAnXuLy = string.IsNullOrWhiteSpace(phuongAnXuLy) ? "Đã kiểm tra và xử lý." : phuongAnXuLy.Trim();
            khieuNai.SoTienHoan = soTienHoan;
            khieuNai.TrangThai = trangThai;

            try
            {
                _context.KhieuNais.Update(khieuNai);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Đã xử lý thành công đơn khiếu nại #{khieuNaiId}!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi hệ thống khi cập nhật: " + ex.Message;
            }

            return RedirectToAction(nameof(IndexKhieuNai));
        }
    }
}