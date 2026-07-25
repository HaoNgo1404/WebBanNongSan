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
using WebWeb.Services;
using WebWeb.Models.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace WebWeb.Controllers
{
    public class OrderController : Controller
    {
        private readonly ECommerceDBContext _context;
        private readonly KhuyenMaiService _khuyenMaiService;
        private readonly IEmailService _emailService;

        public OrderController(ECommerceDBContext context, KhuyenMaiService khuyenMaiService, IEmailService emailService)
        {
            _context = context;
            _khuyenMaiService = khuyenMaiService;
            _emailService = emailService;
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
        public async Task<IActionResult> CheckoutDonLe()
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

            foreach (var item in cartItems)
            {
                // Lấy thông tin nông sản trong DB để lấy chính xác Đơn Giá Niêm Yết gốc
                var product = await _context.NongSans.FindAsync(item.NongSanId);
                decimal giaGoc = product != null ? product.GiaBanNiemYet : item.Gia;

                model.Items.Add(new CartItemViewModel
                {
                    NongSanId = item.NongSanId,
                    SoLuongDat = item.SoLuong, 
                    // Tính giá thực tế từ GIÁ GỐC NIÊM YẾT
                    DonGiaThoiDiem = _khuyenMaiService.TinhGiaBanThucTe(item.NongSanId, giaGoc)
                });
            }

            // ==========================================
            // ĐOẠN XỬ LÝ ĐỊA CHỈ MẶC ĐỊNH ĐỂ FIX LỖI BINDING
            // ==========================================
            int? currentUserId = GetCurrentKhachHangId();
            if (currentUserId != null)
            {
                var listDiaChi = _context.SoDiaChis
                    .Where(d => d.KhachHangId == currentUserId.Value)
                    .OrderByDescending(d => d.IsDefault)
                    .ToList();

                var diaChiMacDinh = listDiaChi.FirstOrDefault(d => d.IsDefault == true);
                if (diaChiMacDinh != null)
                {
                    // ÉP CHÍNH XÁC GIÁ TRỊ VÀO MODEL ĐỂ ĐÈ LÊN SỐ 0 MẶC ĐỊNH
                    model.DiaChiId = diaChiMacDinh.DiaChiId;
                }

                ViewBag.DanhSachDiaChi = new SelectList(listDiaChi.Select(d => new {
                    Id = d.DiaChiId,
                    Text = $"{(string.IsNullOrEmpty(d.LoaiDiaChi) ? "Địa chỉ" : d.LoaiDiaChi)} - {d.DiaChiGiao}"
                }), "Id", "Text", model.DiaChiId);

                ViewBag.DiaChiJson = JsonSerializer.Serialize(listDiaChi.Select(d => new {
                    d.DiaChiId,
                    d.TenNguoiNhan,
                    d.SoDienThoaiNhan,
                    d.DiaChiGiao,
                    IsDefault = d.IsDefault ? 1 : 0
                }));
            }
            else
            {
                ViewBag.DanhSachDiaChi = new SelectList(new List<object>(), "Id", "Text");
                ViewBag.DiaChiJson = "[]";
            }

            if (currentUserId > 0)
            {
                // 2. Lấy thông tin ngày tạo tài khoản của khách hàng
                var khachHang = await _context.KhachHangs.FindAsync(currentUserId);
                ViewBag.DiemTichLuy = khachHang?.DiemTichLuy ?? 0;
                
                if (khachHang != null)
                {
                    // Kiểm tra xem tài khoản tạo mới trong vòng 7 ngày gần đây không
                    bool laTaiKhoanMoi = khachHang.NgayDangKy >= DateTime.Now.AddDays(-7);

                    if (laTaiKhoanMoi)
                    {
                        // 3. Kiểm tra xem tài khoản này đã từng có đơn hàng nào hợp lệ chưa
                        bool daTungMuaHang = await _context.DonHangLes
                            .AnyAsync(d => d.KhachHangId == currentUserId && d.TrangThaiDonHang != OrderStatuses.DaHuy);

                        // Nếu đúng là tài khoản mới và CHƯA từng mua hàng lần nào
                        if (!daTungMuaHang)
                        {
                            // Đẩy thẳng tên mã chào mừng ra ngoài View để gợi ý dùng ngay
                            ViewBag.VoucherGoiY = "BANMOI50"; 
                        }
                    }
                }
            }

            // =========================================================================
            // 💥 BỔ SUNG: GÁN PHÍ VẬN CHUYỂN VÀO MODEL CHO _CheckoutSummary CÓ DỮ LIỆU
            // =========================================================================
            model.PhiVanChuyen = 30000; 
            
            // Nếu trong Controller có dùng ViewBag.PhiVanChuyen thì gán luôn cho đồng bộ:
            ViewBag.PhiVanChuyen = model.PhiVanChuyen;

            return View(model);
        }

        // ==========================================
        // TRANG CHECKOUT 2: ĐĂNG KÝ GÓI ĐỊNH KỲ (UC02) - GET
        // ==========================================
        public async Task<IActionResult> CheckoutDinhKy()
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
                // Lấy chính xác Đơn Giá Niêm Yết gốc
                var product = await _context.NongSans.FindAsync(item.NongSanId);
                decimal giaGoc = product != null ? product.GiaBanNiemYet : item.Gia;

                model.Items.Add(new CartItemViewModel
                {
                    NongSanId = item.NongSanId,
                    SoLuongDat = item.SoLuong, 
                    // Tính giá thực tế từ GIÁ GỐC NIÊM YẾT
                    DonGiaThoiDiem = _khuyenMaiService.TinhGiaBanThucTe(item.NongSanId, giaGoc)
                });
            }

            // Tìm địa chỉ mặc định gán trực tiếp vào model của Đăng ký định kỳ
            var listDiaChiDinhKy = _context.SoDiaChis
                .Where(d => d.KhachHangId == currentUserId.Value)
                .OrderByDescending(d => d.IsDefault)
                .ToList();

            var diaChiMacDinhDK = listDiaChiDinhKy.FirstOrDefault(d => d.IsDefault == true);
            if (diaChiMacDinhDK != null)
            {
                model.DiaChiId = diaChiMacDinhDK.DiaChiId;
            }

            ViewBag.DanhSachDiaChi = new SelectList(listDiaChiDinhKy.Select(d => new {
                Id = d.DiaChiId,
                Text = $"{(string.IsNullOrEmpty(d.LoaiDiaChi) ? "Địa chỉ" : d.LoaiDiaChi)} - {d.DiaChiGiao}"
            }), "Id", "Text", model.DiaChiId);

            ViewBag.DiaChiJson = JsonSerializer.Serialize(listDiaChiDinhKy.Select(d => new {
                d.DiaChiId,
                d.TenNguoiNhan,
                d.SoDienThoaiNhan,
                d.DiaChiGiao,
                IsDefault = d.IsDefault ? 1 : 0
            }));

            // TÍNH TỔNG TIỀN 1 ĐỢT THEO GIÁ ĐÃ GIẢM KHUYẾN MÃI THỰC TẾ
            ViewBag.TongTienMotDot = model.Items.Sum(i => i.SoLuongDat * i.DonGiaThoiDiem);

            // Trả về View dành riêng cho Gói định kỳ
            return View(model);
        }

        // =================================================================
        // UC01: ĐẶT ĐƠN HÀNG LẺ TRỰC TUYẾN - POST (Có tích hợp điểm tích lũy & Gửi Mail xác nhận)
        // =================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DatDonHangLe(CheckoutViewModel model, int? KhuyenMaiId, int diemDungForm)
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
                    int? customerId = GetCurrentKhachHangId();

                    // 1. TÍNH TỔNG TIỀN HÀNG THEO GIÁ ĐÃ GIẢM KHUYẾN MÃI THỰC TẾ
                    decimal tongTienHangDaGiam = 0;
                    foreach (var item in cart)
                    {
                        var product = await _context.NongSans.FindAsync(item.NongSanId);
                        decimal giaGoc = product != null ? product.GiaBanNiemYet : item.Gia;
                        
                        // Tính giá thực tế của từng sản phẩm tại thời điểm đặt
                        decimal giaThucTe = _khuyenMaiService.TinhGiaBanThucTe(item.NongSanId, giaGoc);
                        tongTienHangDaGiam += item.SoLuong * giaThucTe;
                    }

                    // 2. Tính tiền ship dựa trên Tổng tiền đã giảm
                    decimal phiShip = await TinhToanTienShipThucTe(tongTienHangDaGiam); 
                    decimal tongTienDonHang = tongTienHangDaGiam + phiShip;

                    // --- LOGIC XỬ LÝ ĐIỂM TÍCH LŨY ---
                    decimal soTienGiamByDiem = 0;
                    if (customerId.HasValue && diemDungForm > 0)
                    {
                        var khachHang = await _context.KhachHangs.FindAsync(customerId.Value);
                        if (khachHang != null)
                        {
                            int diemHopLeToiDa = Math.Min(khachHang.DiemTichLuy, (int)tongTienDonHang);

                            if (diemDungForm <= khachHang.DiemTichLuy)
                            {
                                int diemThucTeSuDung = Math.Min(diemDungForm, diemHopLeToiDa);
                                soTienGiamByDiem = diemThucTeSuDung;

                                khachHang.DiemTichLuy -= diemThucTeSuDung;
                                _context.KhachHangs.Update(khachHang);
                            }
                            else
                            {
                                ModelState.AddModelError("", "Số điểm tích lũy sử dụng không hợp lệ.");
                                return View("CheckoutDonLe", model);
                            }
                        }
                    }

                    // Số tiền cuối cùng KH phải trả
                    decimal tongTienCuoiCung = tongTienDonHang - soTienGiamByDiem;

                    // Khởi tạo đối tượng đơn hàng
                    var donHang = new DonHangLe
                    {
                        KhachHangId = customerId,
                        NgayDat = DateTime.Now,
                        KhungGioGiaoHang = model.KhungGioGiaoHang,
                        PhuongThucThanhToan = model.PhuongThucThanhToan,
                        TrangThaiDonHang = OrderStatuses.ChoDuyet,
                        
                        // Gán chuẩn số tiền đã giảm sâu nhất
                        TongTienTamTinh = tongTienDonHang, 
                        TongTienThucTe = tongTienCuoiCung, // 👈 ĐÂY LÀ SỐ TIỀN THANH TOÁN CHUẨN
                        TienChenhLech = soTienGiamByDiem
                    };

                    // 2. XỬ LÝ ĐỊA CHỈ: GÁN THẲNG VÀO 3 CỘT TEXT CỦA ĐƠN HÀNG LẺ
                    if (customerId != null)
                    {
                        // 🟢 Lấy thông tin tài khoản Khách hàng trực tiếp từ DB để đảm bảo lấy đúng Email
                        var currentCustomer = await _context.KhachHangs.FindAsync(customerId.Value);
                        string? userEmail = currentCustomer?.Email ?? model.EmailNonAccount;
                        // LUỒNG 1: KHÁCH ĐÃ ĐĂNG NHẬP (Đọc từ sổ địa chỉ)
                        var diaChiSodoch = await _context.SoDiaChis.FindAsync(model.DiaChiId);
                        if (diaChiSodoch != null)
                        {
                            donHang.DiaChiId = diaChiSodoch.DiaChiId;
                            donHang.NameCusNonAccount = diaChiSodoch.TenNguoiNhan;
                            donHang.PhoneNonAccount = diaChiSodoch.SoDienThoaiNhan;
                            donHang.AddressNonAccount = diaChiSodoch.DiaChiGiao;
                            donHang.EmailNonAccount = userEmail;
                        }
                        else
                        {
                            donHang.DiaChiId = null; 
                            donHang.NameCusNonAccount = model.NameCusNonAccount;
                            donHang.PhoneNonAccount = model.PhoneNonAccount;
                            donHang.AddressNonAccount = model.AddressNonAccount;
                            donHang.EmailNonAccount = userEmail;
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
                        donHang.EmailNonAccount = model.EmailNonAccount;
                    }

                    if(KhuyenMaiId.HasValue && KhuyenMaiId.Value > 1)
                    {
                        donHang.KhuyenMaiId = KhuyenMaiId.Value;

                        var voucher = await _context.KhuyenMais.FindAsync(KhuyenMaiId.Value);
                        if (voucher != null)
                        {
                            voucher.SoLuotDaDung +=1;
                            _context.Entry(voucher).State = EntityState.Modified;
                        }
                    }

                    // 3. LƯU ĐƠN HÀNG VÀO DATABASE để sinh tự động DonHangLeId
                    _context.DonHangLes.Add(donHang);
                    await _context.SaveChangesAsync();

                    // 4. LƯU CHI TIẾT ĐƠN HÀNG LẺ
                    foreach (var item in cart)
                    {
                        var product = await _context.NongSans.FindAsync(item.NongSanId);
                        
                        // An toàn: Nếu product bị xóa/null trong DB thì lấy tạm item.Gia từ Session làm phương án dự phòng
                        decimal giaGocNiemYet = product != null ? product.GiaBanNiemYet : item.Gia;

                        // Tính đơn giá thực tế đã áp dụng khuyến mãi từ GIÁ NIÊM YẾT GỐC
                        decimal donGiaThucTe = _khuyenMaiService.TinhGiaBanThucTe(item.NongSanId, giaGocNiemYet);

                        var chiTiet = new ChiTietDonHangLe
                        {
                            DonHangLeId = donHang.DonHangLeId,
                            NongSanId = item.NongSanId,
                            SoLuongDat = item.SoLuong,
                            DonGiaThoiDiem = donGiaThucTe
                        };

                        _context.ChiTietDonHangLes.Add(chiTiet);
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    // Lưu ID đơn hàng vừa tạo vào TempData hoặc Session để trang kết quả đọc được an toàn
                    int newOrderId = donHang.DonHangLeId;

                    // =================================================================
                    // 💡 BỔ SUNG: LOGIC GỬI EMAIL XÁC NHẬN CHO KHÁCH VÃNG LAI
                    // =================================================================
                    if (!customerId.HasValue && !string.IsNullOrEmpty(model.EmailNonAccount))
                    {
                        try
                        {
                            // Mã hóa Token tra cứu an toàn: {Mã_Đơn}_{SĐT}_Key
                            string rawData = $"{donHang.DonHangLeId}_{donHang.PhoneNonAccount}_GuestSecretKey";
                            string token = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(rawData));

                            // Sinh URL dẫn thẳng tới Action GuestTrackingDetail
                            string trackingUrl = Url.Action("GuestTrackingDetail", "Notification", 
                                new { orderId = donHang.DonHangLeId, token = token }, Request.Scheme);

                            string emailBody = $@"
                                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #e0e0e0; border-radius: 8px; padding: 20px;'>
                                    <h2 style='color: #198754;'>Cảm ơn bạn đã đặt hàng tại Green Fresh!</h2>
                                    <p>Xin chào <strong>{donHang.NameCusNonAccount}</strong>,</p>
                                    <p>Đơn hàng <strong>#{donHang.DonHangLeId}</strong> của bạn đã được tiếp nhận thành công vào lúc {donHang.NgayDat:dd/MM/yyyy HH:mm}.</p>
                                    
                                    <table style='width: 100%; border-collapse: collapse; margin: 20px 0;'>
                                        <tr style='background-color: #f8f9fa;'>
                                            <td style='padding: 10px; border: 1px solid #dee2e6;'><strong>Tổng tiền thanh toán:</strong></td>
                                            <td style='padding: 10px; border: 1px solid #dee2e6; color: #dc3545; font-weight: bold;'>{donHang.TongTienThucTe:#,##0} VNĐ</td>
                                        </tr>
                                        <tr>
                                            <td style='padding: 10px; border: 1px solid #dee2e6;'><strong>Địa chỉ giao:</strong></td>
                                            <td style='padding: 10px; border: 1px solid #dee2e6;'>{donHang.AddressNonAccount}</td>
                                        </tr>
                                    </table>

                                    <p>Bạn có thể bấm vào nút bên dưới để xem trực tiếp tiến độ đơn hàng mà không cần đăng nhập:</p>
                                    <p style='text-align: center; margin: 25px 0;'>
                                        <a href='{trackingUrl}' style='background-color: #198754; color: #ffffff; padding: 12px 24px; text-decoration: none; border-radius: 25px; font-weight: bold; display: inline-block;'>Tra Cứu Tiến Độ Đơn Hàng</a>
                                    </p>
                                    <p style='color: #6c757d; font-size: 13px;'>Hoặc bạn có thể truy cập website và tra cứu bằng Mã đơn <strong>#{donHang.DonHangLeId}</strong> và Số điện thoại <strong>{donHang.PhoneNonAccount}</strong>.</p>
                                </div>";

                            // Chạy ngầm Task gửi mail để không làm nghẽn/chậm luồng phản hồi UI của người dùng
                            _ = Task.Run(() => _emailService.SendEmailAsync(model.EmailNonAccount, $"[Green Fresh] Xác nhận đơn hàng #{donHang.DonHangLeId}", emailBody));
                        }
                        catch 
                        { 
                            // Nuốt lỗi gửi mail nếu có sự cố mạng SMTP để tránh làm hỏng luồng Đặt hàng thành công
                        }
                    }
                    // =================================================================

                    // 5. XÓA SẠCH GIỎ HÀNG KHỎI SESSION
                    HttpContext.Session.Remove("UserCart");

                    // Ghi nhận thông báo thành công có kèm thông tin điểm
                    if (soTienGiamByDiem > 0)
                    {
                        TempData["OrderSuccessMessage"] = $"Đặt hàng thành công! Bạn đã dùng {soTienGiamByDiem:#,##0} điểm để giảm giá {soTienGiamByDiem:#,##0} đ.";
                    }

                    // 6. ĐIỀU HƯỚNG THANH TOÁN HOẶC THÀNH CÔNG
                    if (donHang.PhuongThucThanhToan == "VNPAY")
                    {
                        return RedirectToAction("RedirectToVnPay", "Payment", new { orderId = newOrderId, type = "le" });
                    }
                    if (donHang.PhuongThucThanhToan == "MOMO")
                    {
                        return RedirectToAction("RedirectToMoMo", "Payment", new { orderId = newOrderId, type = "le" });
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

            int selectedDiaChiId = model.DiaChiId;

            // Tìm trong Sổ địa chỉ của khách hàng
            var diaChiChon = await _context.SoDiaChis
                .FirstOrDefaultAsync(d => d.DiaChiId == selectedDiaChiId && d.KhachHangId == currentUserId.Value);

            // Nếu ID gửi lên không tìm thấy (hoặc = 0), tự động lấy Địa chỉ mặc định của khách
            if (diaChiChon == null)
            {
                diaChiChon = await _context.SoDiaChis
                    .Where(d => d.KhachHangId == currentUserId.Value)
                    .OrderByDescending(d => d.IsDefault)
                    .FirstOrDefaultAsync();

                if (diaChiChon == null)
                {
                    ModelState.AddModelError("", "Vui lòng thêm địa chỉ nhận hàng trong sổ địa chỉ trước khi đăng ký gói!");
                    return View("CheckoutDinhKy", model);
                }
            }

            // ĐỌC LẠI GIỎ HÀNG TỪ SESSION GIỐNG KHỐI GET ĐỂ ĐẢM BẢO DỮ LIỆU ĐÚNG GỐC CỦA HÀO
            var sessionData = HttpContext.Session.GetString("UserCart");
            var cartItems = sessionData == null ? new List<GioHang>() : JsonSerializer.Deserialize<List<GioHang>>(sessionData);

            if (cartItems == null || !cartItems.Any())
            {
                ModelState.AddModelError("", "Vui lòng chọn nông sản cho gói định kỳ!");
                return View("CheckoutDinhKy", model);
            }

            // Nạp lại danh sách Items vào model từ Session để tính toán chính xác tuyệt đối
            model.Items = new List<CartItemViewModel>();
            foreach (var item in cartItems)
            {
                var product = await _context.NongSans.FindAsync(item.NongSanId);
                decimal giaGoc = product != null ? product.GiaBanNiemYet : item.Gia;

                // ✅ TÍNH GIÁ BÁN THỰC TẾ SAU GIẢM GIÁ/KHUYẾN MÃI
                decimal giaThucTe = _khuyenMaiService.TinhGiaBanThucTe(item.NongSanId, giaGoc);

                model.Items.Add(new CartItemViewModel
                {
                    NongSanId = item.NongSanId,
                    SoLuongDat = item.SoLuong, 
                    DonGiaThoiDiem = giaThucTe // 👈 Đã tính theo giá giảm
                });
            }

            // TIẾN HÀNH TÍNH TOÁN CHI PHÍ TRỌN GÓI TRẢ TRƯỚC 1 LẦN
            DateTime ngayGiaoDuKien = DateTime.Now.AddDays(1);
            int soThang = model.SoThangDangKy ?? 1;
            
            // 1. Tính tổng tiền của riêng 1 đợt giao
            decimal tongTienMotDot = model.Items.Sum(i => i.SoLuongDat * i.DonGiaThoiDiem);
            
            // 2. Tính số đợt giao dựa trên tần suất (Hàng tuần = 4 đợt/tháng, Cách tuần = 2 đợt/tháng)
            int soDotGiao = (model.TanSuatGiao == "HangTuan" || model.TanSuatGiao == Date.HangTuan) ? (soThang * 4) : (soThang * 2);
            
            // 3. Nhân ra tổng chi phí trọn gói của toàn bộ kỳ hạn
            decimal tongTienGoi = tongTienMotDot * soDotGiao;

            DayOfWeek? targetDay = null;
            switch (model.ThuTrongTuan)
            {
                case Date.Thu2: targetDay = DayOfWeek.Monday; break;
                case Date.Thu3: targetDay = DayOfWeek.Tuesday; break;
                case Date.Thu4: targetDay = DayOfWeek.Wednesday; break;
                case Date.Thu5: targetDay = DayOfWeek.Thursday; break;
                case Date.Thu6: targetDay = DayOfWeek.Friday; break;
                case Date.Thu7: targetDay = DayOfWeek.Saturday; break;
                case Date.CN:   targetDay = DayOfWeek.Sunday; break;
            }

            if (targetDay.HasValue)
            {
                int daysUntilTarget = ((int)targetDay.Value - (int)DateTime.Now.DayOfWeek + 7) % 7;
                if (daysUntilTarget == 0) daysUntilTarget = 7; 
                ngayGiaoDuKien = DateTime.Now.AddDays(daysUntilTarget);
            }

            // Khởi tạo thực thể gói định kỳ với tổng tiền trọn gói
            var goiDinhKy = new GoiDangKyDinhKy
            {
                KhachHangId = currentUserId.Value,
                DiaChiId = diaChiChon.DiaChiId,
                KhuyenMaiId = null,
                NgayBatDau = ngayGiaoDuKien, // Lấy ngày đợt giao đầu tiên để chuẩn lịch trình
                NgayKetThuc = ngayGiaoDuKien.AddMonths(soThang),
                TanSuatGiao = model.TanSuatGiao ?? Date.HangTuan,
                ThuTrongTuan = model.ThuTrongTuan ?? Date.Thu2,
                TongTienGoi = tongTienGoi,                 // Đưa số tiền trọn gói thanh toán vào đây
                TrangThaiGoi = OrderStatuses.HoatDong      // Giữ nguyên trạng thái hoạt động mặc định của Hào
            };

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    _context.GoiDangKyDinhKies.Add(goiDinhKy);
                    await _context.SaveChangesAsync(); 

                    // Lưu chi tiết gói nông sản định kỳ 
                    foreach (var item in model.Items)
                    {
                        var chiTietGoi = new ChiTietGoiDinhKy
                        {
                            GoiId = goiDinhKy.GoiId,
                            NongSanId = item.NongSanId,
                            SoLuongMoiDot = (decimal)item.SoLuongDat // Map chuẩn thuộc tính SoLuongMoiDot của Hào
                        };
                        _context.ChiTietGoiDinhKies.Add(chiTietGoi);
                    }

                    await _context.SaveChangesAsync();
                    int newGoiId = goiDinhKy.GoiId;
                    
                    var dsDotGiao = SinhCacDotGiaoDinhKy(goiDinhKy);

                    if(dsDotGiao.Any())
                    {
                        _context.DotGiaoDinhKies.AddRange(dsDotGiao);
                        await _context.SaveChangesAsync();
                    }
                    await transaction.CommitAsync();

                    // Xóa giỏ hàng Session sau khi tạo thực thể dữ liệu thành công
                    HttpContext.Session.Remove("UserCart");

                    // GIỮ NGUYÊN TOÀN BỘ PHÂN LUỒNG REDIRECT SANG VNPAY / MOMO CỦA HÀO
                    if (model.PhuongThucThanhToan == "VNPAY")
                    {
                        return RedirectToAction("RedirectToVnPay", "Payment", new { orderId = newGoiId, type = "dinhky" });
                    }
                    if (model.PhuongThucThanhToan == "MOMO")
                    {
                        return RedirectToAction("RedirectToMoMo", "Payment", new { orderId = newGoiId, type = "dinhky" });
                    }

                    // Mặc định chuyển sang trang thông báo thành công nếu chọn phương thức khác
                    return RedirectToAction("OrderPackageSuccess", "Notification", new { orderId = newGoiId });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    ModelState.AddModelError("", "Đã xảy ra lỗi khi tạo gói định kỳ: " + ex.InnerException?.Message ?? ex.Message);
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
        public async Task<IActionResult> OrderHistory(string? searchTerm, string? trangThai, string? thoiGian)
        {
            // 1. Kiểm tra người dùng đăng nhập chưa
            int? currentUserId = GetCurrentKhachHangId();
            if (currentUserId == null)
            {
                TempData["ErrorMessage"] = "Bạn phải đăng nhập để xem lịch sử đơn hàng!";
                return RedirectToAction("Login", "Account");
            }

            // 2. Khởi tạo Query lấy danh sách đơn hàng của đúng Khách hàng này
            var query = _context.DonHangLes
                .Where(d => d.KhachHangId == currentUserId)
                .AsQueryable();

            // 3. Lọc theo từ khóa (Mã đơn hàng)
            if (!string.IsNullOrEmpty(searchTerm))
            {
                string term = searchTerm.Trim().ToLower();
                query = query.Where(n => n.DonHangLeId.ToString().Contains(term));
            }

            // 4. Lọc theo Trạng thái đơn hàng
            if (!string.IsNullOrEmpty(trangThai) && trangThai != "TatCa")
            {
                query = query.Where(n => n.TrangThaiDonHang == trangThai);
            }

            // 5. Lọc theo Mốc thời gian
            if (!string.IsNullOrEmpty(thoiGian))
            {
                DateTime now = DateTime.Now;
                switch (thoiGian)
                {
                    case "7ngay":
                        query = query.Where(n => n.NgayDat >= now.AddDays(-7));
                        break;
                    case "30ngay":
                        query = query.Where(n => n.NgayDat >= now.AddDays(-30));
                        break;
                    case "thangNay":
                        var startOfMonth = new DateTime(now.Year, now.Month, 1);
                        query = query.Where(n => n.NgayDat >= startOfMonth);
                        break;
                }
            }

            // Giữ lại các giá trị lọc ra ViewBag để hiển thị trên View
            ViewBag.SearchTerm = searchTerm;
            ViewBag.TrangThaiHienTai = trangThai ?? "TatCa";
            ViewBag.ThoiGianHienTai = thoiGian ?? "TatCa";

            // 6. Lấy danh sách sắp xếp đơn mới nhất lên đầu
            var danhSachDonHang = await query
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
        public async Task<IActionResult> HuyDonHangLe(int id, string? phoneGuest)
        {
            int? currentUserId = GetCurrentKhachHangId();
            DonHangLe? donHang = null;

            // Chuẩn hóa SĐT đầu vào từ client
            string cleanPhone = string.IsNullOrEmpty(phoneGuest) 
                ? "" 
                : phoneGuest.Trim().Replace(" ", "").Replace(".", "");

            if (!string.IsNullOrEmpty(cleanPhone) && cleanPhone.StartsWith("+84"))
            {
                cleanPhone = "0" + cleanPhone.Substring(3);
            }
            // 1. XÁC THỰC QUYỀN HỦY ĐƠN
            if (currentUserId.HasValue)
            {
                // 🟢 TH 1: Khách hàng đã đăng nhập -> Kiểm tra theo ID tài khoản
                donHang = await _context.DonHangLes
                    .Include(d => d.ChiTietDonHangLes)
                    .FirstOrDefaultAsync(d => d.DonHangLeId == id && d.KhachHangId == currentUserId.Value);
            }
            // 🟢 TH 2: Tra cứu vãng lai (Nếu không tìm thấy theo Account hoặc chưa đăng nhập)
            if (donHang == null && !string.IsNullOrEmpty(cleanPhone))
            {
                // Lấy đơn hàng theo ID trước, sau đó đối chiếu SĐT linh hoạt
                var candidateOrder = await _context.DonHangLes
                    .Include(d => d.ChiTietDonHangLes)
                    .Include(d => d.DiaChi) // Include thêm bảng Địa chỉ để check SoDienThoaiNhan
                    .FirstOrDefaultAsync(d => d.DonHangLeId == id);

                if (candidateOrder != null)
                {
                    // Lấy tất cả các trường SĐT có thể có trong đơn hàng
                    string phone1 = candidateOrder.PhoneNonAccount?.Trim().Replace(" ", "").Replace(".", "") ?? "";
                    string phone2 = candidateOrder.DiaChi?.SoDienThoaiNhan?.Trim().Replace(" ", "").Replace(".", "") ?? "";
                    
                    if (phone1.StartsWith("+84")) phone1 = "0" + phone1.Substring(3);
                    if (phone2.StartsWith("+84")) phone2 = "0" + phone2.Substring(3);

                    // Kiểm tra khớp ít nhất 1 trong các SĐT
                    if (cleanPhone == phone1 || cleanPhone == phone2 || cleanPhone.EndsWith(phone1) || cleanPhone.EndsWith(phone2))
                    {
                        donHang = candidateOrder;
                    }
                }
            }

            // Trả về lỗi chi tiết nếu vẫn không khớp
            if (donHang == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đơn hàng hợp lệ hoặc thông tin xác thực không đúng!" });
            }

            // Cho phép hủy toàn bộ trạng thái ngoại trừ HoanThanh và DaHuy
            if (donHang.TrangThaiDonHang == OrderStatuses.HoanThanh || donHang.TrangThaiDonHang == OrderStatuses.DaHuy)
            {
                return Json(new { success = false, message = $"Không thể hủy đơn hàng này vì đơn đã ở trạng thái: {donHang.TrangThaiDonHang}!" });
            }

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // =========================================================
                    // FIX LOGIC 1: HOÀN TỒN KHO VỀ BẢNG LOHANG
                    // =========================================================
                    foreach (var item in donHang.ChiTietDonHangLes)
                    {
                        // Tìm lô hàng còn hạn sử dụng mới nhất của nông sản này để cộng trả lại kho
                        var loHangTarget = await _context.LoHangs
                            .Where(l => l.NongSanId == item.NongSanId && l.HanSuDung >= DateTime.Now)
                            .OrderByDescending(l => l.NgayNhapKho) // Lấy lô mới nhất
                            .FirstOrDefaultAsync();

                        // Nếu không tìm thấy lô còn hạn, lấy lô gần nhất bất kỳ của nông sản đó
                        if (loHangTarget == null)
                        {
                            loHangTarget = await _context.LoHangs
                                .Where(l => l.NongSanId == item.NongSanId)
                                .OrderByDescending(l => l.NgayNhapKho)
                                .FirstOrDefaultAsync();
                        }

                        if (loHangTarget != null)
                        {
                            loHangTarget.SoLuongTon += item.SoLuongDat; 
                            _context.LoHangs.Update(loHangTarget);
                        }
                    }

                    // =========================================================
                    // FIX LOGIC 2: HOÀN LẠI ĐIỂM TÍCH LŨY (Nếu đơn có dùng điểm)
                    // =========================================================
                    if (donHang.TienChenhLech > 0 && donHang.KhachHangId.HasValue)
                    {
                        var khachHang = await _context.KhachHangs.FindAsync(donHang.KhachHangId.Value);
                        if (khachHang != null)
                        {
                            khachHang.DiemTichLuy += (int)donHang.TienChenhLech;
                            _context.KhachHangs.Update(khachHang);
                        }
                    }

                    // =========================================================
                    // FIX LOGIC 3: HOÀN TIỀN NẾU ĐÃ THANH TOÁN TRỰC TUYẾN
                    // =========================================================
                    string refundMessage = "";
                    if (donHang.TrangThaiThanhToan == OrderStatuses.DaThanhToan || donHang.PhuongThucThanhToan == "VNPAY" || donHang.PhuongThucThanhToan == "MOMO")
                    {
                        var giaoDich = await _context.GiaoDichThanhToans
                            .FirstOrDefaultAsync(g => g.DonHangLeId == id);

                        if (giaoDich != null)
                        {
                            giaoDich.TrangThai = 2; // Đã hoàn tiền
                            _context.GiaoDichThanhToans.Update(giaoDich);
                        }
                        refundMessage = $" Hệ thống đã hoàn trả số tiền {donHang.TongTienThucTe:#,##0} VNĐ vào tài khoản thanh toán của bạn.";
                    }

                    // Cập nhật trạng thái đơn thành Đã hủy
                    donHang.TrangThaiDonHang = OrderStatuses.DaHuy;
                    _context.DonHangLes.Update(donHang);

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return Json(new { success = true, message = $"Hủy đơn hàng #{id} thành công!{refundMessage}" });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return Json(new { success = false, message = "Lỗi hệ thống khi hủy đơn: " + ex.Message });
                }
            }
        }

        // =================================================================
        // 1. HIỂN THỊ DANH SÁCH GÓI ĐỊNH KỲ CỦA CUSTOMER (CÓ BỘ LỌC)
        // =================================================================
        public async Task<IActionResult> LichSuGoiDinhKy(string? searchTerm, string? trangThai, string? thoiGian)
        {
            int? currentUserId = GetCurrentKhachHangId();
            if (currentUserId == null)
            {
                TempData["ErrorMessage"] = "Bạn phải đăng nhập để xem lịch sử gói định kỳ!";
                return RedirectToAction("Login", "Account");
            }

            var query = _context.GoiDangKyDinhKies
                .Include(g => g.DiaChi)
                .Include(g => g.GiaoDichThanhToans)
                .Where(g => g.KhachHangId == currentUserId.Value)
                .AsQueryable();

            // 1. Lọc theo từ khóa (Mã gói)
            if (!string.IsNullOrEmpty(searchTerm))
            {
                string term = searchTerm.Trim().ToLower();
                query = query.Where(g => g.GoiId.ToString().Contains(term));
            }

            // 2. Lọc theo Trạng thái gói
            if (!string.IsNullOrEmpty(trangThai) && trangThai != "TatCa")
            {
                query = query.Where(g => g.TrangThaiGoi == trangThai);
            }

            // 3. Lọc theo Mốc thời gian bắt đầu gói
            if (!string.IsNullOrEmpty(thoiGian))
            {
                DateTime now = DateTime.Now;
                switch (thoiGian)
                {
                    case "7ngay":
                        query = query.Where(g => g.NgayBatDau >= now.AddDays(-7));
                        break;
                    case "30ngay":
                        query = query.Where(g => g.NgayBatDau >= now.AddDays(-30));
                        break;
                    case "thangNay":
                        var startOfMonth = new DateTime(now.Year, now.Month, 1);
                        query = query.Where(g => g.NgayBatDau >= startOfMonth);
                        break;
                }
            }

            // Giữ lại giá trị lọc ra ViewBag
            ViewBag.SearchTerm = searchTerm;
            ViewBag.TrangThaiHienTai = trangThai ?? "TatCa";
            ViewBag.ThoiGianHienTai = thoiGian ?? "TatCa";

            var danhSachGoi = await query
                .OrderByDescending(g => g.NgayBatDau)
                .ToListAsync();

            return View(danhSachGoi);
        }

        // =================================================================
        // 2. API LẤY CHI TIẾT CÁC ĐỢT GIAO VÀ SẢN PHẨM CỦA GÓI ĐỊNH KỲ (AJAX)
        // =================================================================
        [HttpGet]
        public async Task<IActionResult> GetChiTietGoiDinhKy(int id)
        {
            int? currentUserId = GetCurrentKhachHangId();
            if (currentUserId == null)
            {
                return Json(new { success = false, message = "Chưa đăng nhập!" });
            }

            var goiDinhKy = await _context.GoiDangKyDinhKies
                .Include(g => g.DotGiaoDinhKies)
                .Include(g => g.ChiTietGoiDinhKies)
                    .ThenInclude(ct => ct.NongSan)
                .FirstOrDefaultAsync(g => g.GoiId == id && g.KhachHangId == currentUserId.Value);

            if (goiDinhKy == null)
            {
                return Json(new { success = false, message = "Không tìm thấy gói định kỳ!" });
            }

            // Tạo danh sách tên các nông sản kèm số lượng mỗi đợt
            string danhsachSanPham = string.Join(", ", goiDinhKy.ChiTietGoiDinhKies
                .Select(c => $"{c.NongSan?.TenNongSan ?? "Nông sản"} (x{c.SoLuongMoiDot:#,##0.##})"));

            var resultData = goiDinhKy.DotGiaoDinhKies
                .OrderBy(d => d.NgayGiaoThucTe)
                .Select((d, index) => new
                {
                    dotSo = index + 1,
                    ngayGiao = d.NgayGiaoThucTe.ToString("dd/MM/yyyy"),
                    ghiChu = string.IsNullOrEmpty(danhsachSanPham) ? "Theo danh mục gói" : danhsachSanPham,
                    trangThai = d.TrangThaiGiao
                })
                .ToList();

            return Json(new { success = true, data = resultData });
        }

        // =================================================================
        // 2. XỬ LÝ HỦY GÓI ĐỊNH KỲ QUA AJAX
        // =================================================================
        [HttpPost]
        public async Task<IActionResult> HuyGoiDinhKy(int id)
        {
            var goiDangKy = await _context.GoiDangKyDinhKies
                .Include(g => g.DotGiaoDinhKies)
                .Include(g => g.ChiTietGoiDinhKies)
                .FirstOrDefaultAsync(g => g.GoiId == id);

            if (goiDangKy == null)
            {
                return Json(new { success = false, message = "Không tìm thấy thông tin gói đăng ký định kỳ này!" });
            }

            if (goiDangKy.TrangThaiGoi == OrderStatuses.DaHuy)
            {
                return Json(new { success = false, message = "Gói đăng ký định kỳ này đã được hủy từ trước!" });
            }
            if (goiDangKy.TrangThaiGoi == OrderStatuses.HoanThanh)
            {
                return Json(new { success = false, message = "Gói đăng ký đã hoàn thành toàn bộ lịch trình, không thể hủy!" });
            }

            decimal soTienHoanTra = 0;
            int tongSoDotBanDau = goiDangKy.DotGiaoDinhKies.Count;

            if (tongSoDotBanDau > 0)
            {
                decimal giaTriMotDotGiao = goiDangKy.TongTienGoi / tongSoDotBanDau;

                var dsDotChuaGiao = goiDangKy.DotGiaoDinhKies
                    .Where(d => d.TrangThaiGiao == OrderStatuses.ChoDuyet || d.TrangThaiGiao == OrderStatuses.ChoXuLy)
                    .ToList();

                int soDotChuaGiao = dsDotChuaGiao.Count;

                if (soDotChuaGiao > 0)
                {
                    decimal tienDuConLai = soDotChuaGiao * giaTriMotDotGiao;
                    decimal tiLePhatHuyGoi = 0.10m; // Phạt 10%
                    decimal phiPhat = tienDuConLai * tiLePhatHuyGoi;

                    soTienHoanTra = tienDuConLai - phiPhat;
                    if (soTienHoanTra < 0) soTienHoanTra = 0;
                    if (soTienHoanTra > goiDangKy.TongTienGoi) soTienHoanTra = goiDangKy.TongTienGoi;
                }
            }

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // HOÀN TỒN KHO CÁC ĐỢT GIAO CHƯA XỬ LÝ VỀ BẢNG LOHANG
                    var dsDotCanHuy = goiDangKy.DotGiaoDinhKies
                        .Where(d => d.TrangThaiGiao == OrderStatuses.ChoDuyet || d.TrangThaiGiao == OrderStatuses.ChoXuLy)
                        .ToList();

                    foreach (var dot in dsDotCanHuy)
                    {
                        foreach (var item in goiDangKy.ChiTietGoiDinhKies)
                        {
                            var loHangTarget = await _context.LoHangs
                                .Where(l => l.NongSanId == item.NongSanId && l.HanSuDung >= DateTime.Now)
                                .OrderByDescending(l => l.NgayNhapKho)
                                .FirstOrDefaultAsync();

                            if (loHangTarget == null)
                            {
                                loHangTarget = await _context.LoHangs
                                    .Where(l => l.NongSanId == item.NongSanId)
                                    .OrderByDescending(l => l.NgayNhapKho)
                                    .FirstOrDefaultAsync();
                            }

                            if (loHangTarget != null)
                            {
                                loHangTarget.SoLuongTon += item.SoLuongMoiDot; // Điều chỉnh lại đúng tên cột số lượng của bảng LoHang
                                _context.LoHangs.Update(loHangTarget);
                            }
                        }
                        dot.TrangThaiGiao = OrderStatuses.DaHuy;
                    }

                    // Đánh dấu gói chính thành Đã hủy
                    goiDangKy.TrangThaiGoi = OrderStatuses.DaHuy;
                    _context.GoiDangKyDinhKies.Update(goiDangKy);

                    // Cập nhật giao dịch
                    var giaoDich = await _context.GiaoDichThanhToans.FirstOrDefaultAsync(g => g.GoiDangKyId == id);
                    if (giaoDich != null)
                    {
                        giaoDich.TrangThai = 2; // Đã hoàn tiền
                        _context.GiaoDichThanhToans.Update(giaoDich);
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return Json(new { 
                        success = true, 
                        message = $"Hủy gói thành công! Số tiền hoàn trả lại vào ví/thẻ của bạn là: {soTienHoanTra:N0} VNĐ (Đã trừ 10% phí hủy gói)." 
                    });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return Json(new { success = false, message = "Đã xảy ra lỗi hệ thống trong quá trình hủy gói: " + ex.Message });
                }
            }
        }

        // Ví dụ minh họa một hàm xử lý thanh toán / tạo đơn hàng trong OrderController
        // Hào lồng logic này vào vị trí code tạo đơn hàng lẻ của bạn nhé:
        public async Task<decimal> TinhToanTienShipThucTe(decimal tongTienHang)
        {
            // 1. Đọc phí ship mặc định từ bảng ThamSo lên
            var thamSoPhiShip = await _context.ThamSos.FirstOrDefaultAsync(t => t.MaThamSo == "TS5");
            decimal phiShipMacDinh = thamSoPhiShip != null ? thamSoPhiShip.GiaTri : 30000; // Backup dự phòng 30,000đ

            // 2. Đọc ngưỡng miễn phí ship từ bảng ThamSo lên
            var thamSoNguongFree = await _context.ThamSos.FirstOrDefaultAsync(t => t.MaThamSo == "TS4");
            decimal nguongMienPhiShip = thamSoNguongFree != null ? thamSoNguongFree.GiaTri : 500000; // Backup dự phòng 500,000đ

            // 3. Tiến hành kiểm tra logic động
            decimal phiShipPhaiTra = phiShipMacDinh;
            
            if (tongTienHang >= nguongMienPhiShip)
            {
                phiShipPhaiTra = 0; // Đạt ngưỡng tối thiểu -> Miễn phí giao hàng toàn phần
            }

            return phiShipPhaiTra;
        }

        private List<DotGiaoDinhKy> SinhCacDotGiaoDinhKy(GoiDangKyDinhKy goiRegist)
        {
            var danhSachDotGiao = new List<DotGiaoDinhKy>();
            
            // 1. Chuyển đổi chuỗi ThuTrongTuan từ class Date sang cấu trúc DayOfWeek của hệ thống
            DayOfWeek targetDay = DayOfWeek.Monday; // Mặc định dự phòng là Thứ 2
            switch (goiRegist.ThuTrongTuan)
            {
                case Date.Thu2: targetDay = DayOfWeek.Monday; break;
                case Date.Thu3: targetDay = DayOfWeek.Tuesday; break;
                case Date.Thu4: targetDay = DayOfWeek.Wednesday; break;
                case Date.Thu5: targetDay = DayOfWeek.Thursday; break;
                case Date.Thu6: targetDay = DayOfWeek.Friday; break;
                case Date.Thu7: targetDay = DayOfWeek.Saturday; break;
                case Date.CN:   targetDay = DayOfWeek.Sunday; break;
            }

            // 2. Xác định khoảng cách ngày nhảy dựa trên tần suất giao (7 ngày hoặc 14 ngày)
            // Khớp đúng chuẩn chuỗi tiếng Việt có dấu trong file Date.cs của Hào
            int stepDays = 7; 
            if (goiRegist.TanSuatGiao == Date.CachTuan)
            {
                stepDays = 14;
            }

            // 3. Tìm ngày giao đầu tiên hợp lệ (bằng hoặc sau NgayBatDau và phải đúng Thứ khách chọn)
            DateTime ngayGiaoChay = goiRegist.NgayBatDau;
            while (ngayGiaoChay.DayOfWeek != targetDay)
            {
                ngayGiaoChay = ngayGiaoChay.AddDays(1);
            }

            // Đợt đếm số thứ tự lịch trình
            int soThuTuDot = 1;

            // 4. Vòng lặp sinh tự động lịch trình cho đến khi vượt quá NgayKetThuc của gói
            while (ngayGiaoChay <= goiRegist.NgayKetThuc)
            {
                var dotGiao = new DotGiaoDinhKy
                {
                    GoiId = goiRegist.GoiId,
                    NgayGiaoThucTe = ngayGiaoChay,
                    TrongLuongThucTeDot = 0m,
                    TrangThaiGiao = OrderStatuses.ChoDuyet, // "Chờ duyệt"
                    
                };

                danhSachDotGiao.Add(dotGiao);
                
                // Nhảy sang tuần kế tiếp hoặc 2 tuần kế tiếp theo cấu hình cấu trúc gói
                ngayGiaoChay = ngayGiaoChay.AddDays(stepDays);
                soThuTuDot++;
            }

            return danhSachDotGiao;
        }
    }
}