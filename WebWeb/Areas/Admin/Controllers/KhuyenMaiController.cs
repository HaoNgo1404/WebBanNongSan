using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebWeb.Models;
using WebWeb.Models.ViewModels;

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
        public async Task<IActionResult> CreateAsync()
        {
            // Nạp danh sách nông sản để chọn mặt hàng giảm giá riêng biệt
            ViewData["NongSanId"] = new SelectList(_context.NongSans.OrderBy(n => n.TenNongSan), "NongSanId", "TenNongSan");
            
            // Nạp danh sách danh mục để chọn giảm giá nguyên một danh mục (ví dụ: Rau củ, Trái cây...)
            ViewData["DanhMucId"] = new SelectList(_context.DanhMucs.OrderBy(d => d.TenDanhMuc), "DanhMucId", "TenDanhMuc");
            return View(new KhuyenMai { NgayBatDau = DateTime.Now, NgayKetThuc = DateTime.Now.AddMonths(1), TrangThai = true });
        }

        // 4. TẠO MỚI (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(KhuyenMai km)
        {
            if (ModelState.IsValid)
            {
                km.VoucherCode = km.VoucherCode?.ToUpper().Trim();
                km.SoLuotDaDung = 0;
                _context.Add(km);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            // NẾU FORM BỊ LỖI, PHẢI NẠP LẠI XUỐNG ĐÂY TRƯỚC KHI RETURN VIEW
            ViewData["NongSanId"] = new SelectList(_context.NongSans.OrderBy(n => n.TenNongSan), "NongSanId", "TenNongSan", km.NongSanId);
            ViewData["DanhMucId"] = new SelectList(_context.DanhMucs.OrderBy(d => d.TenDanhMuc), "DanhMucId", "TenDanhMuc", km.DanhMucId);
            return View(km);
        }

        // 5. CHỈNH SỬA (GET)
        public async Task<IActionResult> Edit(int id)
        {
            var khuyenMai = await _context.KhuyenMais.FindAsync(id);
            if (khuyenMai == null) return NotFound();

            ViewBag.NongSanId = new SelectList(await _context.NongSans.ToListAsync(), "NongSanId", "TenNongSan");
            ViewBag.DanhMucId = new SelectList(await _context.DanhMucs.ToListAsync(), "DanhMucId", "TenDanhMuc");
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

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> CheckVoucher(string code, [FromBody] List<CartItemViewModel> cartItems)
        {
            if (string.IsNullOrEmpty(code) || cartItems == null || !cartItems.Any())
            {
                return Json(new { success = false, message = "Dữ liệu giỏ hàng hoặc mã giảm giá trống!" });
            }

            // 1. Tính tổng tiền tạm tính ban đầu dựa theo đúng thuộc tính của Hào
            decimal tongTienToanGio = cartItems.Sum(item => item.DonGiaThoiDiem * item.SoLuongDat);

            // 2. Tìm mã voucher đang kích hoạt
            var voucher = await _context.KhuyenMais
                .FirstOrDefaultAsync(k => k.VoucherCode.ToUpper() == code.ToUpper().Trim() && k.TrangThai == true);

            if (voucher == null) 
            {
                return Json(new { success = false, message = "Mã giảm giá không tồn tại hoặc đã bị tạm ngưng!" });
            }

            // 3. Kiểm tra các điều kiện cơ bản
            var now = DateTime.Now;
            if (now < voucher.NgayBatDau || now > voucher.NgayKetThuc)
                return Json(new { success = false, message = "Mã giảm giá đã hết hạn hoặc chưa tới thời gian áp dụng!" });

            if (voucher.SoLuotDaDung >= voucher.SoLuotPhatHanh)
                return Json(new { success = false, message = "Mã giảm giá này đã hết lượt sử dụng!" });

            if (tongTienToanGio < voucher.GiaTriDonToiThieu)
                return Json(new { success = false, message = $"Đơn hàng chưa đạt giá trị tối thiểu {voucher.GiaTriDonToiThieu:N0}đ để áp dụng mã này!" });

            // KIỂM TRA ĐIỀU KIỆN NẾU ĐÂY LÀ MÃ CHÀO MỪNG TÀI KHOẢN MỚI
            if (code.ToUpper() == "BANMOI50")
            {
                int currentUserId = GetCurrentUserId(); // Lấy ID người dùng đăng nhập
                if (currentUserId == 0)
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập để sử dụng mã ưu đãi thành viên mới!" });
                }

                // 1. Tìm thông tin khách hàng
                var khachHang = await _context.KhachHangs
                    .Include(kh => kh.DonHangLes) // Nạp kèm danh sách đơn hàng để đếm Count
                    .FirstOrDefaultAsync(kh => kh.KhachHangId == currentUserId);
                if (khachHang == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy thông tin tài khoản!" });
                }

                // 2. TÍNH TOÁN SỐ NGÀY (Cơ chế an toàn chống lệch định dạng MM/dd và dd/MM)
                TimeSpan hieuThoiGian = DateTime.Now.Date - khachHang.NgayDangKy.Date;
                int soNgayDaTroiQua = Math.Abs(hieuThoiGian.Days); // Lấy giá trị tuyệt đối đề phòng ngày bị nhảy tiến/lùi

                // MẸO DEMO DUYỆT BÀI: Nới lỏng điều kiện lên 30 ngày (hoặc Hào có thể comment đoạn if này lại nếu muốn tắt hẳn việc chặn ngày)
                if (soNgayDaTroiQua > 30)
                {
                    return Json(new { 
                        success = false, 
                        message = $"Mã ưu đãi này đã hết hạn nhận! Hệ thống tính toán tài khoản của bạn đã tạo được {soNgayDaTroiQua} ngày (Chỉ áp dụng trong vòng 30 ngày đầu tạo tài khoản)." 
                    });
                }

                // 3. Quét lịch sử đơn hàng lẻ xem đã mua đơn nào chưa (Bỏ qua các đơn đã hủy)
                // Lọc bỏ các đơn đã hủy ra trước, nếu những đơn còn lại mà có số lượng > 0 nghĩa là đã từng mua
                int soDonHangThucTe = khachHang.DonHangLes.Count(d => d.TrangThaiDonHang != OrderStatuses.DaHuy);

                if (soDonHangThucTe > 0)
                {
                    return Json(new { success = false, message = "Mã giảm giá này chỉ áp dụng cho đơn hàng đầu tiên của tài khoản mới!" });
                }
            }

            // ========================================================
            // LOGIC LỌC SẢN PHẨM THEO PHẠM VI KHÓA NGOẠI (NongSan / DanhMuc)
            // ========================================================
            var danhSachHopLe = cartItems.AsQueryable();

            if (voucher.NongSanId.HasValue)
            {
                // Điều kiện 1: Voucher chỉ áp dụng cho 1 Nông sản cụ thể
                danhSachHopLe = danhSachHopLe.Where(i => i.NongSanId == voucher.NongSanId.Value);
            }
            else if (voucher.DanhMucId.HasValue)
            {
                // Điều kiện 2: Voucher áp dụng cho toàn bộ Nông sản thuộc 1 Danh mục cụ thể
                // Lấy danh sách ID nông sản thuộc danh mục đó từ Database
                var dsIdNongSanThuocDanhMuc = await _context.NongSans
                    .Where(n => n.DanhMucId == voucher.DanhMucId.Value)
                    .Select(n => n.NongSanId)
                    .ToListAsync();

                danhSachHopLe = danhSachHopLe.Where(i => dsIdNongSanThuocDanhMuc.Contains(i.NongSanId));
            }

            // Tính tổng tiền của những mặt hàng thỏa mãn điều kiện giảm giá
            decimal tongTienHangDuocGiam = danhSachHopLe.Sum(item => item.DonGiaThoiDiem * item.SoLuongDat);

            if (tongTienHangDuocGiam == 0)
            {
                return Json(new { 
                    success = false, 
                    message = "Mã ưu đãi này không áp dụng cho bất kỳ sản phẩm nào hiện có trong giỏ hàng!" 
                });
            }

            // 4. Tính toán số tiền thực chiết khấu dựa trên nhóm hàng hợp lệ
            decimal soTienGiam = 0;
            if (voucher.LoaiGiamGia == 1) // Giảm theo %
            {
                soTienGiam = tongTienHangDuocGiam * (voucher.MucGiam / 100m);
                // Kiểm tra trần giảm giá tối đa
                if (voucher.SoTienGiamToiDa > 0 && soTienGiam > voucher.SoTienGiamToiDa)
                {
                    soTienGiam = voucher.SoTienGiamToiDa;
                }
            }
            else // Giảm theo số tiền cố định
            {
                soTienGiam = voucher.MucGiam;
            }

            // Phòng ngừa số tiền giảm vượt quá tổng tiền hóa đơn thực tế
            if (soTienGiam > tongTienToanGio) soTienGiam = tongTienToanGio;

            return Json(new { 
                success = true, 
                khuyenMaiId = voucher.KhuyenMaiId,
                soTienGiam = soTienGiam,
                message = $"Áp dụng mã thành công! Bạn được giảm {soTienGiam:N0}đ." 
            });
        }

        private int GetCurrentUserId()
        {
            // 1. Kiểm tra xem người dùng có đang đăng nhập không
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                // 2. Ép hệ thống tìm đúng danh tính thuộc nhóm "Khách Hàng" (Client) 
                // Hào kiểm tra xem tên Authentication Scheme lúc Login của Khách hàng là gì (thường là "Cookies", "Identity.Application" hoặc "ClientScheme")
                var clientIdentity = User.Identities.FirstOrDefault(id => id.AuthenticationType != "AdminScheme");

                if (clientIdentity != null)
                {
                    var userIdClaim = clientIdentity.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                    if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int id))
                    {
                        return id; // Trả về chuẩn xác ID = 3 của Khách hàng, không bị lẫn với Admin ID = 1
                    }
                }
                
                // 3. Phương án dự phòng: Nếu không phân tách được scheme, lấy thẳng NameIdentifier hiện tại
                var backupClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (backupClaim != null && int.TryParse(backupClaim.Value, out int backupId))
                {
                    return backupId;
                }
            }
            return 0;
        }
    }
}