using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Security.Claims;
using WebWeb.Models;
using WebWeb.Services;
using WebWeb.Helpers; // 🟢 THÊM NAMESPACE HELPER SLUG

namespace WebWeb.Controllers;

public class ProductController : Controller
{
    private readonly ECommerceDBContext _context;
    private readonly KhuyenMaiService _khuyenMaiService;
    private readonly AISentimentService _aiSentimentService;

    public ProductController(ECommerceDBContext context, KhuyenMaiService khuyenMaiService, AISentimentService aiSentimentService)
    {
        _context = context;
        _khuyenMaiService = khuyenMaiService;
        _aiSentimentService = aiSentimentService;
    }

    // 🟢 ROUTE CHUẨN SEO: /san-pham/dua-hau-long-an-p1
    [HttpGet("san-pham/{slug}-p{id:int}")]
    public async Task<IActionResult> Detail(string slug, int id)
    {
        var product = await _context.NongSans
            .Include(n => n.NhaVuon)
            .Include(n => n.DanhGiaSanPhams)
                .ThenInclude(dg => dg.KhachHang)
            .Include(n => n.LoHangs)
            .Include(n => n.DanhMuc)
            .FirstOrDefaultAsync(n => n.NongSanId == id);

        if (product == null) return NotFound();

        // 🟢 BỔ SUNG / SỬA LẠI: Ép nạp & ghi đè HoTen chuẩn từ DB cho DanhGiaSanPhams
        if (product.DanhGiaSanPhams != null && product.DanhGiaSanPhams.Any())
        {
            var khachHangIds = product.DanhGiaSanPhams.Select(dg => dg.KhachHangId).Distinct().ToList();
            
            // Query lấy danh sách khách hàng trực tiếp từ DB
            var danhSachKhachHang = await _context.KhachHangs
                .Where(kh => khachHangIds.Contains(kh.KhachHangId))
                .ToListAsync();

            var khachHangDict = danhSachKhachHang.ToDictionary(
                kh => kh.KhachHangId, 
                kh => string.IsNullOrWhiteSpace(kh.HoTen) ? "Khách hàng Green Fresh" : kh.HoTen
            );

            // Ép gán đè lại object KhachHang cho từng đánh giá
            foreach (var dg in product.DanhGiaSanPhams)
            {
                if (khachHangDict.ContainsKey(dg.KhachHangId))
                {
                    dg.KhachHang = new KhachHang
                    {
                        KhachHangId = dg.KhachHangId,
                        HoTen = khachHangDict[dg.KhachHangId]
                    };
                }
            }
        }

        // 🟢 Kiểm tra và Redirect 301 nếu Slug trên URL không khớp với tên sản phẩm hiện tại
        string expectedSlug = product.TenNongSan.ToSlug();
        if (string.IsNullOrEmpty(slug) || slug != expectedSlug)
        {
            return RedirectToRoutePermanent(new { slug = expectedSlug, id = product.NongSanId });
        }

        // 🟢 Tối ưu SEO On-Page cho sản phẩm
        ViewData["Title"] = $"{product.TenNongSan} - Green Fresh";
        ViewData["MetaDescription"] = $"{product.TenNongSan} tươi sạch. {product.MoTa}. Giá ưu đãi chỉ {product.GiaBanNiemYet:N0}đ.";
        ViewData["MetaKeywords"] = $"{product.TenNongSan}, {product.DanhMuc?.TenDanhMuc}, nông sản sạch, Green Fresh";

        // 🟢 Tối ưu Open Graph khi Share Facebook
        ViewData["OgType"] = "product";
        ViewData["OgTitle"] = product.TenNongSan;
        ViewData["OgDescription"] = !string.IsNullOrEmpty(product.MoTa) && product.MoTa.Length > 150 
            ? product.MoTa.Substring(0, 150) + "..." 
            : product.MoTa;
            
        // Đường dẫn ảnh tuyệt đối (Absolute URL)
        ViewData["OgImage"] = $"{Request.Scheme}://{Request.Host}/images/products/{product.HinhAnh}";

        decimal giaBanThucTe = _khuyenMaiService.TinhGiaBanThucTe(product.NongSanId, product.GiaBanNiemYet);
        ViewBag.GiaBanThucTe = giaBanThucTe;

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


        // 🟢 BỔ SUNG: PHÂN TÍCH CẢM XÚC BÌNH LUẬN TRƯỚC KHI RENDER VIEW
        var camXucDict = new Dictionary<int, string>();
        if (product.DanhGiaSanPhams != null && product.DanhGiaSanPhams.Any())
        {
            foreach (var review in product.DanhGiaSanPhams)
            {
                string kq = await _aiSentimentService.PhanTichCamXucAsync(review.BinhLuan ?? "");
                camXucDict[review.DanhGiaId] = kq;
            }
        }


        // Gửi danh sách ID này sang View
        ViewBag.LikedProductIds = likedProductIds;
        ViewBag.CamXucDict = camXucDict;

        return View(product);
    }

    // 🟢 ROUTE CHUẨN SEO CHO DANH MỤC: /danh-muc/rau-cu-c1
    [HttpGet("danh-muc/{slug}-c{id:int}")]
    public async Task<IActionResult> DanhMuc(string slug, int id)
    {
        // Tìm danh mục dựa vào DanhMucId
        var danhMuc = await _context.DanhMucs
            .FirstOrDefaultAsync(dm => dm.DanhMucId == id);

        if (danhMuc == null)
        {
            return NotFound();
        }

        // 🟢 Kiểm tra và Redirect 301 nếu Slug trên URL không khớp hoặc dùng URL cũ
        string expectedSlug = danhMuc.TenDanhMuc.ToSlug();
        if (string.IsNullOrEmpty(slug) || slug != expectedSlug)
        {
            return RedirectToRoutePermanent(new { slug = expectedSlug, id = danhMuc.DanhMucId });
        }

        // 🟢 TỐI ƯU SEO CHO TRANG DANH MỤC
        ViewData["Title"] = $"{danhMuc.TenDanhMuc} Tươi Sạch - Green Fresh";
        ViewData["MetaDescription"] = $"Danh sách {danhMuc.TenDanhMuc} tươi ngon, an toàn vệ sinh thực phẩm, cam kết chất lượng OCOP từ Green Fresh.";
        ViewData["MetaKeywords"] = $"{danhMuc.TenDanhMuc}, mua {danhMuc.TenDanhMuc}, nông sản tươi, Green Fresh";
        
        ViewData["OgType"] = "website";
        ViewData["OgTitle"] = $"{danhMuc.TenDanhMuc} Tươi Sạch";
        ViewData["OgDescription"] = $"Mua các loại {danhMuc.TenDanhMuc} tươi sạch giá tốt nhất tại Green Fresh.";

        // Lấy danh sách nông sản thuộc danh mục này
        var dsNongSan = await _context.NongSans
            .Include(ns => ns.LoHangs)
            .Include(ns => ns.DanhGiaSanPhams)
            .Include(ns => ns.NhaVuon)
            .Where(ns => ns.DanhMucId == id &&
                            (ns.LoHangs.Sum(l => l.SoLuongTon) > 0 || ns.DanhGiaSanPhams.Any(d => d.SoSao >= 4)))
            .ToListAsync();

        // KHỞI TẠO DICTIONARY ĐỂ CHỨA GIÁ ĐÃ GIẢM
        var giaThucTeDict = new Dictionary<int, decimal>();
        foreach (var product in dsNongSan)
        {
            giaThucTeDict[product.NongSanId] = _khuyenMaiService.TinhGiaBanThucTe(product.NongSanId, product.GiaBanNiemYet);
        }
        ViewBag.GiaThucTeDict = giaThucTeDict;
        ViewBag.TenDanhMuc = danhMuc.TenDanhMuc;

        return View(dsNongSan);
    }

    // =================================================================
    // LUỒNG 1: ĐÁNH GIÁ MỘT SẢN PHẨM CỤ THỂ (TỪ TRANG CHI TIẾT SẢN PHẨM)
    // =================================================================
    [HttpPost]
    public async Task<IActionResult> DanhGiaSanPhamLe(int nongSanId, int soSao, string binhLuan)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("KhachHangId")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int customerId))
        {
            return Json(new { success = false, message = "Bạn cần đăng nhập để thực hiện đánh giá này!" });
        }

        // 🟢 2. TRUY VẤN CHÍNH XÁC KhachHangId từ DB (Tránh lệch giữa TaiKhoanId và KhachHangId)
        var khachHang = await _context.KhachHangs
            .FirstOrDefaultAsync(kh => kh.KhachHangId == customerId);

        if (khachHang == null)
        {
            return Json(new { success = false, message = "Không tìm thấy thông tin khách hàng trên hệ thống!" });
        }

        int realKhachHangId = khachHang.KhachHangId;

        if (soSao < 1 || soSao > 5)
        {
            return Json(new { success = false, message = "Số sao đánh giá phải từ 1 đến 5 sao!" });
        }

        var orderId = await _context.DonHangLes
            .Where(dh => dh.KhachHangId == customerId)
            .Select(dh => dh.DonHangLeId)
            .FirstOrDefaultAsync();

        if (orderId == 0)
        {
            orderId = await _context.DonHangLes.Select(dh => dh.DonHangLeId).FirstOrDefaultAsync();
        }

        string nhanCamXuc = await _aiSentimentService.PhanTichCamXucAsync(binhLuan);

        var newReview = new DanhGiaSanPham
        {
            NongSanId = nongSanId,
            SoSao = soSao,
            BinhLuan = string.IsNullOrWhiteSpace(binhLuan) ? "Khách hàng không để lại lời bình." : binhLuan.Trim(),
            NgayDanhGia = DateTime.Now,
            KhachHangId = realKhachHangId,
            DonHangLeId = orderId
        };

        try
        {
            _context.DanhGiaSanPhams.Add(newReview);
            await _context.SaveChangesAsync();
            return Json(new { 
                success = true, 
                message = "Cảm ơn bạn đã gửi đánh giá!",
                sentiment = nhanCamXuc 
            });
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
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("KhachHangId")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int customerId))
        {
            return Json(new { success = false, message = "Bạn cần đăng nhập để đánh giá đơn hàng!" });
        }

        var donHang = await _context.DonHangLes
            .Include(dh => dh.ChiTietDonHangLes)
            .FirstOrDefaultAsync(dh => dh.DonHangLeId == donHangId && dh.KhachHangId == customerId);

        if (donHang == null)
        {
            return Json(new { success = false, message = "Không tìm thấy đơn hàng hợp lệ để đánh giá!" });
        }

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
    // LUỒNG 3: KHÁCH HÀNG GỬI KHIẾU NẠI ĐƠN HÀNG
    // =================================================================
    [HttpPost]
    public async Task<IActionResult> KhieuNaiDonHang(int donHangId, string noiDung, IFormFile? hinhAnh)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("KhachHangId")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int customerId))
        {
            return Json(new { success = false, message = "Vui lòng đăng nhập hệ thống trước khi khiếu nại!" });
        }

        if (string.IsNullOrWhiteSpace(noiDung))
        {
            return Json(new { success = false, message = "Vui lòng nhập nội dung khiếu nại đầy đủ!" });
        }

        var donHang = await _context.DonHangLes.FindAsync(donHangId);
        if (donHang == null)
        {
            return Json(new { success = false, message = "Không tìm thấy đơn hàng tương ứng trên hệ thống!" });
        }

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

        string? fileNameSaved = null;
        if (hinhAnh != null && hinhAnh.Length > 0)
        {
            try
            {
                string uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "khieunai");
                
                if (!Directory.Exists(uploadFolder))
                {
                    Directory.CreateDirectory(uploadFolder);
                }

                string extension = Path.GetExtension(hinhAnh.FileName);
                fileNameSaved = $"khieunai_{donHangId}_{DateTime.Now.Ticks}{extension}";
                
                string filePath = Path.Combine(uploadFolder, fileNameSaved);

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

        var khieuNai = new KhieuNai
        {
            DonHangLeId = donHangId,
            KhachHangId = customerId,
            NoiDung = noiDung.Trim(),
            NgayGui = DateTime.Now,
            TrangThai = 0,
            PhuongAnXuLy = null,
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
            return Json(new { success = false, message = "Lỗi lưu dữ liệu: " + ex.InnerException?.Message });
        }
    }
}