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

        public OrderController(ECommerceDBContext context, KhuyenMaiService khuyenMaiService)
        {
            _context = context;
            _khuyenMaiService = khuyenMaiService;
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
            
            // Đọc giỏ hàng từ Session (Giữ nguyên code cũ của bạn)
            var sessionData = HttpContext.Session.GetString("UserCart");
            var cartItems = sessionData == null ? new List<GioHang>() : JsonSerializer.Deserialize<List<GioHang>>(sessionData);

            if (cartItems == null || !cartItems.Any())
            {
                TempData["ErrorMessage"] = "Giỏ hàng của bạn đang trống, không thể thanh toán!";
                return RedirectToAction("Index", "Cart");
            }

            foreach (var item in cartItems)
            {
                model.Items.Add(new CartItemViewModel
                {
                    NongSanId = item.NongSanId,
                    SoLuongDat = item.SoLuong, 
                    DonGiaThoiDiem = _khuyenMaiService.TinhGiaBanThucTe(item.NongSanId, item.Gia)
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

            ViewBag.TongTienMotDot = cartItems.Sum(i => i.SoLuong * i.Gia);

            // Trả về View dành riêng cho Gói định kỳ
            return View(model);
        }

        // =================================================================
        // UC01: ĐẶT ĐƠN HÀNG LẺ TRỰC TUYẾN - POST (Có tích hợp điểm tích lũy)
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
                    int? customerId = GetCurrentKhachHangId(); // Null nếu là khách vãng lai
                    decimal phiShip = await TinhToanTienShipThucTe(cart.Sum(item => item.SoLuong * item.Gia)); // Gọi hàm lấy cấu hình động ở trên
                    decimal tongTienHang = cart.Sum(item => item.SoLuong * item.Gia) + phiShip;

                    // --- LOGIC XỬ LÝ ĐIỂM TÍCH LŨY (Đã ràng buộc không vượt quá giá trị đơn hàng) ---
                    decimal soTienGiamByDiem = 0;
                    if (customerId.HasValue && diemDungForm > 0)
                    {
                        var khachHang = await _context.KhachHangs.FindAsync(customerId.Value);
                        if (khachHang != null)
                        {
                            // Điểm tối đa được phép dùng = Giá trị nhỏ hơn giữa (Điểm đang có) và (Tổng tiền đơn hàng)
                            int diemHợpLeToiDa = Math.Min(khachHang.DiemTichLuy, (int)tongTienHang);

                            // Nếu số điểm form gửi lên hợp lệ (không vượt quá số điểm thực tế của họ)
                            if (diemDungForm <= khachHang.DiemTichLuy)
                            {
                                // Khống chế số điểm sử dụng thực tế không vượt quá giá trị đơn hàng
                                int diemThucTeSuDung = Math.Min(diemDungForm, diemHợpLeToiDa);
                                
                                soTienGiamByDiem = diemThucTeSuDung;

                                // Trừ số điểm thực tế sử dụng dưới Database
                                khachHang.DiemTichLuy -= diemThucTeSuDung;
                                _context.KhachHangs.Update(khachHang);
                            }
                            else
                            {
                                // Phát hiện gian lận gửi điểm giả lập lớn hơn số điểm họ có thực tế
                                ModelState.AddModelError("", "Số điểm tích lũy sử dụng không hợp lệ.");
                                return View("CheckoutDonLe", model);
                            }
                        }
                    }

                    // Khởi tạo đối tượng đơn hàng mới với số tiền đã trừ điểm
                    var donHang = new DonHangLe
                    {
                        KhachHangId = customerId,
                        NgayDat = DateTime.Now,
                        KhungGioGiaoHang = model.KhungGioGiaoHang,
                        PhuongThucThanhToan = model.PhuongThucThanhToan,
                        TrangThaiDonHang = OrderStatuses.ChoDuyet,
                        
                        // Khấu trừ điểm trực tiếp vào hóa đơn
                        TongTienTamTinh = tongTienHang - soTienGiamByDiem,
                        TongTienThucTe = tongTienHang - soTienGiamByDiem, 
                        TienChenhLech = soTienGiamByDiem // Lưu số tiền đã giảm bằng điểm vào đây để thống kê đối soát
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
                        var chiTiet = new ChiTietDonHangLe
                        {
                            DonHangLeId = donHang.DonHangLeId,
                            NongSanId = item.NongSanId,
                            SoLuongDat = item.SoLuong,
                            DonGiaThoiDiem = _khuyenMaiService.TinhGiaBanThucTe(item.NongSanId, product.GiaBanNiemYet)
                        };
                        _context.ChiTietDonHangLes.Add(chiTiet);
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    // Lưu ID đơn hàng vừa tạo vào TempData hoặc Session để trang kết quả đọc được an toàn
                    int newOrderId = donHang.DonHangLeId;

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
                model.Items.Add(new CartItemViewModel
                {
                    NongSanId = item.NongSanId,
                    SoLuongDat = item.SoLuong, 
                    DonGiaThoiDiem = item.Gia
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
            if (donHang.TrangThaiDonHang != OrderStatuses.ChoDuyet)
            {
                return Json(new { success = false, message = $"Không thể hủy đơn hàng này vì đơn đang ở trạng thái: {donHang.TrangThaiDonHang}!" });
            }

            string refundMessage = "";

            // =================================================================
            // LOGIC HOÀN TIỀN KHI ĐƠN ĐÃ THANH TOÁN (SỬ DỤNG EF CORE THUẦN)
            // =================================================================
            if(donHang.TrangThaiThanhToan == OrderStatuses.DaThanhToan)
            {
                // Tìm giao dịch thanh toán liên quan đến đơn hàng này
                var giaoDich = await _context.GiaoDichThanhToans
                    .FirstOrDefaultAsync(g => g.DonHangLeId == id);

                if (giaoDich != null)
                {
                    giaoDich.TrangThai = 2; // Cập nhật trạng thái giao dịch
                    _context.GiaoDichThanhToans.Update(giaoDich);
                    await _context.SaveChangesAsync();
                }
                refundMessage = $" Hệ thống đã hoàn trả số tiền {donHang.TongTienTamTinh.ToString("#,##0")} VNĐ của giao dịch gốc.";
            }
            try
            {
                // Cập nhật trạng thái hủy
                donHang.TrangThaiDonHang = OrderStatuses.DaHuy;
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = $"Hủy đơn hàng #{id} thành công!{refundMessage}" });
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
        public async Task<IActionResult> HuyGoiDinhKy(int id)
        {
            // 1. Tìm gói đăng ký kèm theo danh sách các đợt giao hàng định kỳ
            var goiDangKy = await _context.GoiDangKyDinhKies
                .Include(g => g.DotGiaoDinhKies)
                .FirstOrDefaultAsync(g => g.GoiId == id);

            if (goiDangKy == null)
            {
                return Json(new { success = false, message = "Không tìm thấy thông tin gói đăng ký định kỳ này!" });
            }

            // 2. Kiểm tra điều kiện trạng thái gói (Chỉ cho hủy gói khi đang hoạt động)
            if (goiDangKy.TrangThaiGoi == OrderStatuses.DaHuy)
            {
                return Json(new { success = false, message = "Gói đăng ký định kỳ này đã được hủy từ trước!" });
            }
            if (goiDangKy.TrangThaiGoi == OrderStatuses.HoanThanh)
            {
                return Json(new { success = false, message = "Gói đăng ký đã hoàn thành toàn bộ lịch trình, không thể hủy!" });
            }

            // 3. TIẾN HÀNH TÍNH TOÁN TIỀN HOÀN TRẢ ĐỒNG BỘ
            decimal soTienHoanTra = 0;

            // Đếm tổng số đợt giao hàng ban đầu của gói
            int tongSoDotBanDau = goiDangKy.DotGiaoDinhKies.Count;

            if (tongSoDotBanDau > 0)
            {
                // Tính giá trị kinh tế trung bình của 1 đợt giao hàng dựa trên số tiền khách đã trả trước
                decimal giaTriMotDotGiao = goiDangKy.TongTienGoi / tongSoDotBanDau;

                // Các đợt được coi là "ĐÃ DÙNG/KHÔNG THỂ HOÀN TIỀN": Đã giao, Đang giao, Đang chuẩn bị hàng.
                // Chỉ những đợt ở trạng thái "Chờ xử lý" (hoặc chưa khởi tạo lịch) mới được tính hoàn tiền.
                int soDotChuaGiao = goiDangKy.DotGiaoDinhKies
                    .Count(d => d.TrangThaiGiao == OrderStatuses.ChoXuLy);

                if (soDotChuaGiao > 0)
                {
                    // Số tiền dư lý thuyết của các đợt chưa giao
                    decimal tienDuConLai = soDotChuaGiao * giaTriMotDotGiao;

                    // Áp dụng phí phạt hủy ngang hợp đồng (Ví dụ: phạt 10% tiền dư để bù chi phí vận hành, giữ lại 90%)
                    decimal tiLePhatHuyGoi = 0.10m; 
                    decimal phiPhat = tienDuConLai * tiLePhatHuyGoi;

                    soTienHoanTra = tienDuConLai - phiPhat;
                    
                    // Đảm bảo số tiền hoàn không âm và không vượt quá tổng số tiền gói ban đầu
                    if (soTienHoanTra < 0) soTienHoanTra = 0;
                    if (soTienHoanTra > goiDangKy.TongTienGoi) soTienHoanTra = goiDangKy.TongTienGoi;
                }
            }

            // 4. CẬP NHẬT TRẠNG THÁI CÁC ĐỐI TƯỢNG VÀO CƠ SỞ DỮ LIỆU
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // Cập nhật trạng thái gói chính thành Đã hủy
                    goiDangKy.TrangThaiGoi = OrderStatuses.DaHuy;

                    // Hủy toàn bộ những đợt giao hàng "Chờ xử lý" (chưa giao) thuộc gói này
                    var dsDotChuaGiao = goiDangKy.DotGiaoDinhKies
                        .Where(d => d.TrangThaiGiao == OrderStatuses.ChoXuLy);
                    foreach (var dot in dsDotChuaGiao)
                    {
                        dot.TrangThaiGiao = OrderStatuses.DaHuy;
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    // 5. TRẢ VỀ KẾT QUẢ CHO AJAX Ở GIAO DIỆN
                    return Json(new { 
                        success = true, 
                        message = $"Hủy gói thành công! Số tiền hoàn trả lại qua ví/thẻ của bạn là: {soTienHoanTra.ToString("N0")} VNĐ (Đã trừ phí hủy gói nếu có)." 
                    });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return Json(new { success = false, message = "Đã xảy ra lỗi hệ thống trong quá trình hủy gói và tính toán hoàn tiền!" });
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