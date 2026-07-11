using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Security.Claims;
using WebWeb.Models;

namespace WebWeb.Controllers;

public class ProductController : Controller
{
    private readonly ECommerceDBContext _context;

    public ProductController(ECommerceDBContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Detail(int id)
    {
        var product = await _context.NongSans
            .Include(n => n.NhaVuon)
            .Include(n => n.DanhGiaSanPhams)
            .FirstOrDefaultAsync(n => n.NongSanId == id);

        if (product == null) return NotFound();

        // LOGIC CHECK TIM: Lấy danh sách ID đã thích của User hiện tại
        List<int> likedProductIds = new List<int>();
        
        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            // Nếu đã đăng nhập: Lấy từ Database
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("KhachHangId")?.Value;
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int customerId))
            {
                likedProductIds = await _context.YeuThiches
                    .Where(yt => yt.KhachHangId == customerId)
                    .Select(yt => yt.NongSanId)
                    .ToListAsync();
            }
        }
        else
        {
            // Nếu chưa đăng nhập: Lấy từ Session
            var sessionData = HttpContext.Session.GetString("UserWishlist");
            if (!string.IsNullOrEmpty(sessionData))
            {
                likedProductIds = JsonSerializer.Deserialize<List<int>>(sessionData) ?? new List<int>();
            }
        }

        // Gửi danh sách ID này sang View
        ViewBag.LikedProductIds = likedProductIds;

        return View(product);
    }

    // URL chạy thực tế sẽ dạng: /Product/DanhMuc?id=1 hoặc /Product/DanhMuc/1 tùy cấu hình route
    public async Task<IActionResult> DanhMuc(int id)
    {
        // Tìm danh mục dựa vào DanhMucId truyền từ Navbar qua
        var danhMuc = await _context.DanhMucs
            .FirstOrDefaultAsync(dm => dm.DanhMucId == id);

        if (danhMuc == null)
        {
            return NotFound();
        }

        // Lấy danh sách nông sản thuộc danh mục này
        var dsNongSan = await _context.NongSans
            .Where(ns => ns.DanhMucId == id)
            .ToListAsync();

        // Gửi thông tin sang View hiển thị
        ViewBag.TenDanhMuc = danhMuc.TenDanhMuc;
        ViewData["Title"] = danhMuc.TenDanhMuc;

        return View(dsNongSan);
    }

    // =================================================================
    // LUỒNG 1: ĐÁNH GIÁ MỘT SẢN PHẨM CỤ THỂ (TỪ TRANG CHI TIẾT SẢN PHẨM)
    // =================================================================
    [HttpPost]
    public async Task<IActionResult> DanhGiaSanPhamLe(int nongSanId, int soSao, string binhLuan)
    {
        // 1. Kiểm tra trạng thái đăng nhập của khách hàng
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("KhachHangId")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int customerId))
        {
            return Json(new { success = false, message = "Bạn cần đăng nhập để thực hiện đánh giá này!" });
        }

        // 2. Kiểm tra tính hợp lệ của số sao
        if (soSao < 1 || soSao > 5)
        {
            return Json(new { success = false, message = "Số sao đánh giá phải từ 1 đến 5 sao!" });
        }

        // 3. Tìm 1 ID đơn hàng bất kỳ của khách để lót lỗi khóa ngoại DonHangLeId cứng trong DB
        var orderId = await _context.DonHangLes
            .Where(dh => dh.KhachHangId == customerId)
            .Select(dh => dh.DonHangLeId)
            .FirstOrDefaultAsync();

        if (orderId == 0)
        {
            orderId = await _context.DonHangLes.Select(dh => dh.DonHangLeId).FirstOrDefaultAsync();
        }

        // 4. Tạo thực thể lưu trữ dữ liệu đánh giá sản phẩm lẻ
        var newReview = new DanhGiaSanPham
        {
            NongSanId = nongSanId,
            SoSao = soSao,
            BinhLuan = string.IsNullOrWhiteSpace(binhLuan) ? "Khách hàng không để lại lời bình." : binhLuan.Trim(),
            NgayDanhGia = DateTime.Now,
            KhachHangId = customerId,
            DonHangLeId = orderId
        };

        try
        {
            _context.DanhGiaSanPhams.Add(newReview);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Cảm ơn bạn đã gửi đánh giá sản phẩm!" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Lỗi hệ thống khi lưu đánh giá: " + ex.Message });
        }
    }

    // =================================================================
    // LUỒNG 2: ĐÁNH GIÁ TOÀN BỘ ĐƠN HÀNG (DÀNH CHO ĐƠN "ĐÃ GIAO THÀNH CÔNG")
    // =================================================================
    [HttpPost]
    public async Task<IActionResult> DanhGiaDonHang(int donHangId, int soSao, string binhLuan)
    {
        // 1. Kiểm tra trạng thái đăng nhập
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("KhachHangId")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int customerId))
        {
            return Json(new { success = false, message = "Bạn cần đăng nhập để đánh giá đơn hàng!" });
        }

        // 2. Kiểm tra xem đơn hàng có thực sự thuộc về khách hàng và đã ở trạng thái Hoàn thành ("Đã giao") chưa
        var donHang = await _context.DonHangLes
            .Include(dh => dh.ChiTietDonHangLes)
            .FirstOrDefaultAsync(dh => dh.DonHangLeId == donHangId && dh.KhachHangId == customerId);

        if (donHang == null)
        {
            return Json(new { success = false, message = "Không tìm thấy đơn hàng hợp lệ để đánh giá!" });
        }

        // Kiểm tra điều kiện trạng thái "HoanThanh" (Đã giao thành công) từ Shipper
        if (donHang.TrangThaiDonHang != "HoanThanh" && donHang.TrangThaiDonHang != OrderStatuses.HoanThanh)
        {
            return Json(new { success = false, message = "Đơn hàng chưa giao thành công, không thể thực hiện đánh giá!" });
        }

        if (donHang.ChiTietDonHangLes == null || !donHang.ChiTietDonHangLes.Any())
        {
            return Json(new { success = false, message = "Đơn hàng trống, không có sản phẩm để đánh giá!" });
        }

        try
        {
            // 3. Đánh giá tự động cho tất cả các sản phẩm có mặt trong đơn hàng này
            foreach (var chiTiet in donHang.ChiTietDonHangLes)
            {
                var review = new DanhGiaSanPham
                {
                    NongSanId = chiTiet.NongSanId,
                    SoSao = soSao,
                    BinhLuan = string.IsNullOrWhiteSpace(binhLuan) ? "Đánh giá theo đơn hàng thành công." : binhLuan.Trim(),
                    NgayDanhGia = DateTime.Now,
                    KhachHangId = customerId,
                    DonHangLeId = donHangId
                };
                _context.DanhGiaSanPhams.Add(review);
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Hệ thống đã ghi nhận đánh giá cho toàn bộ đơn hàng của bạn!" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Có lỗi xảy ra trong quá trình lưu đánh giá đơn hàng: " + ex.Message });
        }
    }

    // =================================================================
    // LUỒNG 3: KHÁCH HÀNG GỬI KHIẾU NẠI ĐƠN HÀNG (ĐÃ KHỚP CHUẨN MODEL KHIEUNAI)
    // =================================================================
    [HttpPost]
    public async Task<IActionResult> KhieuNaiDonHang(int donHangId, string noiDung, IFormFile? hinhAnh) // 1. Chuyển donHangId sang kiểu int
    {
        // Lấy ID khách hàng từ phiên đăng nhập
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("KhachHangId")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int customerId))
        {
            return Json(new { success = false, message = "Vui lòng đăng nhập hệ thống trước khi khiếu nại!" });
        }

        if (string.IsNullOrWhiteSpace(noiDung))
        {
            return Json(new { success = false, message = "Vui lòng nhập nội dung khiếu nại đầy đủ!" });
        }

        // 2. Kiểm tra xem đơn hàng lẻ này có thực sự tồn tại không
        var donHang = await _context.DonHangLes.FindAsync(donHangId);
        if (donHang == null)
        {
            return Json(new { success = false, message = "Không tìm thấy đơn hàng tương ứng trên hệ thống!" });
        }

        // 3. Logic chặn thời gian khiếu nại (Giữ nguyên logic tính toán của Hào)
        int soGioHanDinh = 24; 
        var thamSoTg = await _context.ThamSos.FirstOrDefaultAsync(t => t.MaThamSo == "TS6");
        if (thamSoTg != null)
        {
            soGioHanDinh = (int)thamSoTg.GiaTri;
        }

        DateTime thoiDiemGiaoHang = donHang.NgayDat; 
        if ((DateTime.Now - thoiDiemGiaoHang).TotalHours > soGioHanDinh)
        {
            return Json(new { success = false, message = $"Đơn hàng đã quá hạn thời gian khiếu nại hỗ trợ ({soGioHanDinh} giờ kể từ khi đặt/giao)!" });
        }

        // ==========================================
        // LOGIC XỬ LÝ LƯU FILE ẢNH MINH CHỨNG
        // ==========================================
        string? fileNameSaved = null;
        if (hinhAnh != null && hinhAnh.Length > 0)
        {
            try
            {
                // Định nghĩa thư mục lưu file: wwwroot/uploads/khieunai
                string uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "khieunai");
                
                // Nếu thư mục chưa tồn tại thì tự động tạo mới
                if (!Directory.Exists(uploadFolder))
                {
                    Directory.CreateDirectory(uploadFolder);
                }

                // Đổi tên file để tránh trùng lặp (Ví dụ: khieunai_125_63784920.jpg)
                string extension = Path.GetExtension(hinhAnh.FileName);
                fileNameSaved = $"khieunai_{donHangId}_{DateTime.Now.Ticks}{extension}";
                
                string filePath = Path.Combine(uploadFolder, fileNameSaved);

                // Lưu file xuống ổ đĩa cứng server
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await hinhAnh.CopyToAsync(stream);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi trong quá trình upload ảnh: " + ex.Message });
            }
        }

        // 4. Khởi tạo đối tượng khớp chính xác với Entity Model
        var khieuNai = new KhieuNai
        {
            DonHangLeId = donHangId, // Giờ đã là kiểu int nên gán cực kỳ an toàn
            KhachHangId = customerId,
            NoiDung = noiDung.Trim(),
            NgayGui = DateTime.Now,
            TrangThai = 0, // 0: Chờ tiếp nhận
            PhuongAnXuLy = null, // Ban đầu để null, khi nào Admin duyệt mới cập nhật chuỗi chữ để tránh lỗi độ dài DB
            SoTienHoan = 0,
            HinhAnhMinhChung = fileNameSaved
        };

        try
        {
            _context.KhieuNais.Add(khieuNai);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Gửi đơn khiếu nại thành công! Ban quản trị sẽ sớm xử lý." });
        }
        catch (Exception ex)
        {
            // Trả về lỗi chi tiết nếu DB bị lỗi ràng buộc
            return Json(new { success = false, message = "Lỗi lưu dữ liệu: " + ex.InnerException?.Message });
        }
    }
}