using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json;
using WebWeb.Models;
using WebWeb.Models.ViewModels;

namespace WebWeb.Controllers
{
    public class OrderController : Controller
    {
        private readonly ECommerceDBContext _context;

        public OrderController(ECommerceDBContext context)
        {
            _context = context;
        }

        // Helper lấy ID khách hàng từ Cookie Claims
        private int? GetCurrentKhachHangId()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                // Thử tìm theo Claim chuẩn NameIdentifier
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                
                // Hoặc tìm theo tên Claim tự cấu hình tùy biến khi đăng nhập (ví dụ "KhachHangId")
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    userIdClaim = User.FindFirst("KhachHangId")?.Value;
                }

                if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int id))
                {
                    return id;
                }
            }
            return null;
        }

        // ==========================================
        // TRANG CHECKOUT 1: ĐƠN HÀNG LẺ (UC01) - GET
        // ==========================================
        public IActionResult CheckoutDonLe()
        {
            var model = new CheckoutViewModel();
            
            // Đọc giỏ hàng từ Session
            var sessionData = HttpContext.Session.GetString("UserCart");
            var cartItems = sessionData == null ? new List<GioHang>() : JsonSerializer.Deserialize<List<GioHang>>(sessionData);

            if (cartItems == null || !cartItems.Any())
            {
                TempData["ErrorMessage"] = "Giỏ hàng của bạn đang trống, không thể thanh toán!";
                return RedirectToAction("Index", "Cart");
            }

            // Map dữ liệu giỏ hàng sang ViewModel
            foreach (var item in cartItems)
            {
                model.Items.Add(new CartItemViewModel
                {
                    NongSanId = item.NongSanId,
                    SoLuongDat = item.SoLuong, 
                    DonGiaThoiDiem = item.Gia
                });
            }

            // Trả về View dành riêng cho Đơn lẻ
            return View(model);
        }

        // ==========================================
        // TRANG CHECKOUT 2: ĐĂNG KÝ GÓI ĐỊNH KỲ (UC02) - GET
        // ==========================================
        public IActionResult CheckoutDinhKy()
        {
            // ĐỌC COOKIE: Kiểm tra đăng nhập (Bắt buộc đối với gói định kỳ)
            int? currentUserId = GetCurrentKhachHangId();
            if (currentUserId == null)
            {
                TempData["ErrorMessage"] = "Bạn phải đăng nhập tài khoản thành viên để đăng ký gói định kỳ!";
                
                // Điều hướng sang trang Login phù hợp để tránh vòng lặp đứng im tại trang giỏ hàng
                return RedirectToAction("Login", "Account"); 
            }

            var model = new CheckoutViewModel();
            
            // Đọc giỏ hàng từ Session
            var sessionData = HttpContext.Session.GetString("UserCart");
            var cartItems = sessionData == null ? new List<GioHang>() : JsonSerializer.Deserialize<List<GioHang>>(sessionData);

            if (cartItems == null || !cartItems.Any())
            {
                TempData["ErrorMessage"] = "Giỏ hàng của bạn đang trống!";
                return RedirectToAction("Index", "Cart");
            }

            foreach (var item in cartItems)
            {
                model.Items.Add(new CartItemViewModel
                {
                    NongSanId = item.NongSanId,
                    SoLuongDat = item.SoLuong, 
                    DonGiaThoiDiem = item.Gia
                });
            }

            // Trả về View dành riêng cho Gói định kỳ
            return View(model);
        }

        // =================================================================
        // UC01: ĐẶT ĐƠN HÀNG LẺ TRỰC TUYẾN - POST
        // =================================================================
        // =================================================================
        // UC01: ĐẶT ĐƠN HÀNG LẺ TRỰC TUYẾN - POST (ĐÃ SỬA TOÀN DIỆN)
        // =================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DatDonHangLe(CheckoutViewModel model)
        {
            // 1. Đọc dữ liệu giỏ hàng từ Session
            var sessionData = HttpContext.Session.GetString("UserCart");
            var cart = string.IsNullOrEmpty(sessionData) 
                ? new List<GioHang>() 
                : JsonSerializer.Deserialize<List<GioHang>>(sessionData);

            if (cart == null || !cart.Any())
            {
                ModelState.AddModelError("", "Giỏ hàng của bạn đang trống!");
                return View("CheckoutDonLe", model);
            }

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    int? customerId = GetCurrentKhachHangId(); // Null nếu là khách vãng lai

                    // Khởi tạo đối tượng đơn hàng mới
                    var donHang = new DonHangLe
                    {
                        KhachHangId = customerId, // Lưu NULL nếu là khách vãng lai
                        NgayDat = DateTime.Now,
                        KhungGioGiaoHang = model.KhungGioGiaoHang,
                        PhuongThucThanhToan = model.PhuongThucThanhToan,
                        TrangThaiDonHang = "Chờ xử lý",
                        TongTienTamTinh = cart.Sum(item => item.SoLuong * item.Gia)
                    };

                    // 2. XỬ LÝ ĐỊA CHỈ: GÁN THẲNG VÀO 3 CỘT TEXT CỦA ĐƠN HÀNG LẺ
                    if (customerId != null)
                    {
                        // LUỒNG 1: KHÁCH ĐÃ ĐĂNG NHẬP (Đọc từ sổ địa chỉ)
                        var diaChiSodoch = await _context.SoDiaChis.FindAsync(model.DiaChiId);
                        if (diaChiSodoch != null)
                        {
                            donHang.DiaChiId = diaChiSodoch.DiaChiId;
                            donHang.NameCusNonAccount = diaChiSodoch.TenNguoiNhan;
                            donHang.PhoneNonAccount = diaChiSodoch.SoDienThoaiNhan;
                            donHang.AddressNonAccount = diaChiSodoch.DiaChiGiao;
                        }
                        else
                        {
                            donHang.DiaChiId = null; 
                            donHang.NameCusNonAccount = model.NameCusNonAccount;
                            donHang.PhoneNonAccount = model.PhoneNonAccount;
                            donHang.AddressNonAccount = model.AddressNonAccount;
                        }
                    }
                    else
                    {
                        // LUỒNG 2: KHÁCH VÃNG LAI
                        // LƯU Ý BẮT BUỘC: Ép thuộc tính DiaChiId về hẳn null thay vì để mặc định = 0
                        donHang.DiaChiId = null; 
                        donHang.NameCusNonAccount = model.NameCusNonAccount;
                        donHang.PhoneNonAccount = model.PhoneNonAccount;
                        donHang.AddressNonAccount = model.AddressNonAccount;
                    }

                    // 3. LƯU ĐƠN HÀNG VÀO DATABASE để sinh tự động DonHangLeId
                    _context.DonHangLes.Add(donHang);
                    await _context.SaveChangesAsync();

                    // 4. LƯU CHI TIẾT ĐƠN HÀNG LẺ
                    foreach (var item in cart)
                    {
                        var chiTiet = new ChiTietDonHangLe
                        {
                            DonHangLeId = donHang.DonHangLeId,
                            NongSanId = item.NongSanId,
                            SoLuongDat = item.SoLuong,
                            DonGiaThoiDiem = item.Gia
                        };
                        _context.ChiTietDonHangLes.Add(chiTiet);
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    // Lưu ID đơn hàng vừa tạo vào TempData hoặc Session để trang kết quả đọc được an toàn
                    int newOrderId = donHang.DonHangLeId;

                    // 5. XÓA SẠCH GIỎ HÀNG KHỎI SESSION
                    HttpContext.Session.Remove("UserCart");

                    // 6. ĐIỀU HƯỚNG THANH TOÁN HOẶC THÀNH CÔNG
                    if (donHang.PhuongThucThanhToan == "VNPAY")
                    {
                        return RedirectToAction("RedirectToVnPay", "Payment", new { orderId = newOrderId, type = "DonHangLe" });
                    }
                    if (donHang.PhuongThucThanhToan == "MOMO")
                    {
                        return RedirectToAction("RedirectToMoMo", "Payment", new { orderId = newOrderId, type = "DonHangLe" });
                    }

                    // Truyền chuẩn tham số orderId sang NotificationController
                    return RedirectToAction("OrderSuccess", "Notification", new { orderId = newOrderId });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    // In chi tiết lỗi gốc ra console debug và giao diện
                    var errorMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                    ModelState.AddModelError("", "Lỗi xử lý lưu database: " + errorMsg);
                    return View("CheckoutDonLe", model);
                }
            }
        }

        // =================================================================
        // UC02: ĐĂNG KÝ GÓI NÔNG SẢN ĐỊNH KỲ - POST
        // =================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DatGoiDinhKy(CheckoutViewModel model)
        {
            // ĐỌC COOKIE: Lấy thông tin từ Identity Claim
            int? currentUserId = GetCurrentKhachHangId();
            if (currentUserId == null)
            {
                ModelState.AddModelError("", "Bạn phải đăng nhập tài khoản thành viên để đăng ký gói định kỳ!");
                return View("CheckoutDinhKy", model);
            }

            if (model.Items == null || !model.Items.Any())
            {
                ModelState.AddModelError("", "Vui lòng chọn nông sản cho gói định kỳ!");
                return View("CheckoutDinhKy", model);
            }

            int soThang = model.SoThangDangKy ?? 1;
            decimal tongTienMotDot = model.Items.Sum(i => i.SoLuongDat * i.DonGiaThoiDiem);
            int soDotGiao = model.TanSuatGiao == "HangTuan" ? (soThang * 4) : (soThang * 2);
            decimal tongTienGoi = tongTienMotDot * soDotGiao;

            var goiDinhKy = new GoiDangKyDinhKy
            {
                KhachHangId = currentUserId.Value,
                DiaChiId = model.DiaChiId,
                NgayBatDau = DateTime.Now.AddDays(2), 
                NgayKetThuc = DateTime.Now.AddMonths(soThang),
                TanSuatGiao = model.TanSuatGiao ?? "HangTuan",
                ThuTrongTuan = model.ThuTrongTuan ?? "Thu 2",
                TongTienGoi = tongTienGoi,
                TrangThaiGoi = "Chờ duyệt"
            };

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    _context.GoiDangKyDinhKies.Add(goiDinhKy);
                    await _context.SaveChangesAsync(); 

                    foreach (var item in model.Items)
                    {
                        var chiTietGoi = new ChiTietGoiDinhKy
                        {
                            GoiId = goiDinhKy.GoiId,
                            NongSanId = item.NongSanId,
                            SoLuongMoiDot = (decimal)item.SoLuongDat 
                        };
                        _context.ChiTietGoiDinhKies.Add(chiTietGoi);
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    // Xóa giỏ hàng Session sau khi tạo thực thể dữ liệu thành công
                    HttpContext.Session.Remove("UserCart");

                    // Phân luồng Redirect cho gói định kỳ trực tuyến
                    if (model.PhuongThucThanhToan == "VNPAY")
                    {
                        return RedirectToAction("RedirectToVnPay", "Payment", new { orderId = goiDinhKy.GoiId, type = "GoiDinhKy" });
                    }
                    if (model.PhuongThucThanhToan == "MOMO")
                    {
                        return RedirectToAction("RedirectToMoMo", "Payment", new { orderId = goiDinhKy.GoiId, type = "GoiDinhKy" });
                    }

                    TempData["SuccessMessage"] = "Đăng ký gói nông sản định kỳ thành công! Chờ hệ thống xét duyệt.";
                    return RedirectToAction("LichSuGoiDinhKy", "KhachHang");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    ModelState.AddModelError("", "Đã xảy ra lỗi khi tạo gói định kỳ: " + ex.Message);
                    return View("CheckoutDinhKy", model);
                }
            }
        }

        public async Task<IActionResult> XacNhanDonHang(int id)
        {
            var donHang = await _context.DonHangLes
                .Include(d => d.DiaChi)
                .FirstOrDefaultAsync(d => d.DonHangLeId == id);
            return View(donHang);
        }

        // =================================================================
        // LỊCH SỬ ĐƠN HÀNG LẺ CỦA KHÁCH HÀNG (TRANG USER)
        // =================================================================
        public async Task<IActionResult> OrderHistory()
        {
            // 1. Kiểm tra người dùng đăng nhập chưa
            int? currentUserId = GetCurrentKhachHangId();
            if (currentUserId == null)
            {
                TempData["ErrorMessage"] = "Bạn phải đăng nhập để xem lịch sử đơn hàng!";
                return RedirectToAction("Login", "Account");
            }

            // 2. Lấy danh sách đơn hàng lẻ của người dùng này, sắp xếp đơn mới nhất lên đầu
            var danhSachDonHang = await _context.DonHangLes
                .Where(d => d.KhachHangId == currentUserId)
                .OrderByDescending(d => d.NgayDat)
                .ToListAsync();

            return View(danhSachDonHang);
        }

        // =================================================================
        // CHI TIẾT ĐƠN HÀNG LẺ TRONG LỊCH SỬ
        // =================================================================
        public async Task<IActionResult> OrderHistoryDetail(int id)
        {
            int? currentUserId = GetCurrentKhachHangId();
            if (currentUserId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Lấy chi tiết đơn hàng lẻ kèm thông tin nông sản bên trong
            var donHang = await _context.DonHangLes
                .Include(d => d.DiaChi)
                .Include(d => d.ChiTietDonHangLes).ThenInclude(ct => ct.NongSan)
                .FirstOrDefaultAsync(d => d.DonHangLeId == id && d.KhachHangId == currentUserId);

            if (donHang == null)
            {
                return NotFound();
            }

            return View(donHang);
        }

        // =================================================================
        // UC02: KHÁCH HÀNG TỰ HỦY ĐƠN HÀNG LẺ QUA AJAX
        // =================================================================
        [HttpPost]
        public async Task<IActionResult> HuyDonHangLe(int id)
        {
            // Kiểm tra phiên đăng nhập của tài khoản thành viên
            int? currentUserId = GetCurrentKhachHangId();
            if (currentUserId == null)
            {
                return Json(new { success = false, message = "Phiên đăng nhập của bạn đã hết hạn, vui lòng đăng nhập lại!" });
            }

            // Tìm đơn hàng lẻ thuộc về chính khách hàng đó và đang ở trạng thái "Chờ xử lý"
            var donHang = await _context.DonHangLes
                .FirstOrDefaultAsync(d => d.DonHangLeId == id && d.KhachHangId == currentUserId.Value);

            if (donHang == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đơn hàng hợp lệ của bạn!" });
            }

            // Chỉ cho phép hủy khi đơn hàng đang ở trạng thái "Chờ xử lý"
            if (donHang.TrangThaiDonHang != "Chờ xử lý")
            {
                return Json(new { success = false, message = $"Không thể hủy đơn hàng này vì đơn đang ở trạng thái: {donHang.TrangThaiDonHang}!" });
            }

            try
            {
                // Cập nhật trạng thái hủy
                donHang.TrangThaiDonHang = "Đã hủy";
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = $"Đã hủy thành công đơn hàng #{id}!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi hệ thống khi cập nhật hủy đơn: " + ex.Message });
            }
        }

        // =================================================================
        // 1. HIỂN THỊ DANH SÁCH GÓI ĐỊNH KỲ CỦA CUSTOMER
        // =================================================================
        //[Authorize("Customer")]
        public async Task<IActionResult> LichSuGoiDinhKy()
        {
            int? currentUserId = GetCurrentKhachHangId();
            if (currentUserId == null)
            {
                TempData["ErrorMessage"] = "Bạn phải đăng nhập để xem lịch sử gói định kỳ!";
                return RedirectToAction("Login", "Account");
            }

            // Nạp đầy đủ bảng DiaChi và danh sách GiaoDichThanhToans liên quan
            var danhSachGoi = await _context.GoiDangKyDinhKies
                .Include(g => g.DiaChi)
                .Include(g => g.GiaoDichThanhToans) // Load danh sách giao dịch chứa cột GoiId
                .Where(g => g.KhachHangId == currentUserId.Value)
                .OrderByDescending(g => g.NgayBatDau) // Dùng NgayBatDau thay cho NgayDangKy
                .ToListAsync();

            return View(danhSachGoi);
        }

        // =================================================================
        // 2. XỬ LÝ HỦY GÓI ĐỊNH KỲ QUA AJAX
        // =================================================================
        [HttpPost]
        //[Authorize("Customer")]
        public async Task<IActionResult> HuyGoiDinhKy(int id)
        {
            int? currentUserId = GetCurrentKhachHangId();
            if (currentUserId == null)
            {
                return Json(new { success = false, message = "Phiên đăng nhập đã hết hạn!" });
            }

            var goiId = await _context.GoiDangKyDinhKies
                .FirstOrDefaultAsync(g => g.GoiId == id && g.KhachHangId == currentUserId.Value);

            if (goiId == null)
            {
                return Json(new { success = false, message = "Không tìm thấy gói đăng ký hợp lệ!" });
            }

            // Sửa theo đúng tên thuộc tính thực tế: TrangThaiGoi
            if (goiId.TrangThaiGoi == "Đã hủy")
            {
                return Json(new { success = false, message = "Gói này đã được hủy trước đó rồi!" });
            }

            // Cập nhật trạng thái hủy vào trường TrangThaiGoi
            goiId.TrangThaiGoi = "Đã hủy";
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Bạn đã hủy gói nông sản định kỳ thành công!" });
        }
    }
}