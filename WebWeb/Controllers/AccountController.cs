using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using System.Net;
using System.Net.Mail;
using WebWeb.Models;
using WebWeb.ViewModels;

namespace WebWeb.Controllers
{
    public class AccountController : Controller
    {
        private readonly ECommerceDBContext _context;
        private readonly IConfiguration _configuration;

        public AccountController(ECommerceDBContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        #region CUSTOMER LOGIN & REGISTER
        
        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // Tìm kiếm khách hàng trong DB theo Email và Mật khẩu (Nên hash mật khẩu nếu thực tế yêu cầu)
            var customer = await _context.KhachHangs
                .FirstOrDefaultAsync(k => k.Email == model.Email && k.MatKhauMaHoa == model.Password);

            if (customer != null)
            {
                // Tạo danh tính cho Customer
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, customer.KhachHangId.ToString()),
                    new Claim(ClaimTypes.Name, customer.HoTen),
                    new Claim(ClaimTypes.Email, customer.Email),
                    new Claim(ClaimTypes.MobilePhone, customer.SoDienThoai),
                    new Claim(ClaimTypes.Role, "Customer") // Gán quyền Customer
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

                // Đọc giá trị ra một biến và xóa ngay lập tức khỏi TempData để lượt sau không bị dính
                var isNewUser = TempData["IsNewUserRegistered"]?.ToString();

                if (isNewUser == "YES")
                {
                    // Bắn thông báo tặng mã BANMOI50 cho người vừa mới đăng ký xong
                    TempData["NewUserWelcome"] = $"Chào mừng {customer.HoTen} gia nhập Fresh Farm! Món quà ra mắt dành riêng cho bạn là mã giảm giá 50.000đ: <strong class='text-danger fs-4'>BANMOI50</strong> (Áp dụng cho đơn hàng từ 100k). Hãy nhanh tay mua sắm nhé!";
                }
                else
                {
                    // Tài khoản cũ đăng nhập hoàn toàn bình thường, không bao giờ hiện popup quà thành viên mới nữa
                    TempData["LoginSuccessMessage"] = $"Chào mừng bạn quay trở lại, {customer.HoTen}!";
                }

                // Đọc danh sách yêu thích tạm thời từ Session trước khi đăng nhập
                var sessionData = HttpContext.Session.GetString("UserWishlist");
                if (!string.IsNullOrEmpty(sessionData))
                {
                    var productIds = JsonSerializer.Deserialize<List<int>>(sessionData);
                    if (productIds != null && productIds.Any())
                    {
                        foreach (var productId in productIds)
                        {
                            // Kiểm tra xem sản phẩm này đã được lưu trong DB của khách hàng này chưa
                            var exists = _context.YeuThiches.Any(yt => yt.KhachHangId == customer.KhachHangId && yt.NongSanId == productId);
                            if (!exists)
                            {
                                _context.YeuThiches.Add(new YeuThich {
                                    KhachHangId = customer.KhachHangId,
                                    NongSanId = productId,
                                    NgayThem = DateTime.Now
                                });
                            }
                        }
                        _context.SaveChanges();
                        // Đồng bộ xong thì xóa sạch Session yêu thích tạm đi
                        HttpContext.Session.Remove("UserWishlist");
                    }
                }

                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError("", "Email hoặc mật khẩu khách hàng không chính xác!");
            return View(model);
        }

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // Kiểm tra trùng Email
            var emailExists = await _context.KhachHangs.AnyAsync(k => k.Email == model.Email);
            if (emailExists)
            {
                ModelState.AddModelError("Email", "Email này đã được sử dụng!");
                return View(model);
            }

            // Tạo đối tượng khách hàng mới lưu vào DB
            var newCustomer = new KhachHang
            {
                HoTen = model.FullName,
                Email = model.Email,
                SoDienThoai = model.PhoneNumber,
                MatKhauMaHoa = model.Password, // Lưu trực tiếp theo thiết kế database của Hào
                NgayDangKy = DateTime.Now
            };

            _context.KhachHangs.Add(newCustomer);
            await _context.SaveChangesAsync();

            // ---- Đánh dấu đây là người mới vừa đăng ký xong ----
            TempData["IsNewUserRegistered"] = "YES";

            return RedirectToAction(nameof(Login));
        }
        // ĐĂNG XUẤT
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }
        // =================================================================
        // QUÊN MẬT KHẨU - [GET] Hiển thị giao diện nhập Email
        // =================================================================
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // =================================================================
        // QUÊN MẬT KHẨU - [POST] Xử lý kiểm tra email và gửi yêu cầu khôi phục
        // =================================================================
        [HttpPost]
        public async Task<IActionResult> ForgotPassword(LoginViewModel model)
        {
            if (string.IsNullOrEmpty(model.Email))
            {
                ModelState.AddModelError("Email", "Vui lòng nhập Email của bạn!");
                return View(model);
            }

            var khachHang = await _context.KhachHangs.FirstOrDefaultAsync(k => k.Email == model.Email.Trim());
            if (khachHang == null)
            {
                ModelState.AddModelError("Email", "Email này không tồn tại trên hệ thống!");
                return View(model);
            }

            // 1. Tạo OTP 6 số ngẫu nhiên
            string otp = new Random().Next(100000, 999999).ToString();

            // 2. Lưu vào Session để đối chiếu lúc sau
            HttpContext.Session.SetString("ResetOTP", otp);
            HttpContext.Session.SetString("ResetEmail", khachHang.Email);
            HttpContext.Session.SetString("OTPExpiry", DateTime.Now.AddMinutes(5).ToString());

            // // 3. Gửi Mail
            // try
            // {
            //     await SendResetPasswordEmailAsync(khachHang.Email, otp);
            //     TempData["SendOTPSuccessMessage"] = "Mã OTP đã được gửi đến Email của bạn! Vui lòng kiểm tra hộp thư.";
            //     return RedirectToAction(nameof(VerifyOTP));
            // }
            // catch (Exception ex)
            // {
            //     ModelState.AddModelError("", "Không thể gửi email. Vui lòng kiểm tra lại cấu hình EmailSettings! Lỗi: " + ex.Message);
            //     return View(model);
            // }

            // 3. Giả lập thành công & Bắn ngay mã OTP ra TempData để hiển thị ở trang VerifyOTP
            TempData["DemoOTP"] = otp; // Lưu OTP demo
            TempData["SendOTPSuccessMessage"] = "Mã xác thực OTP đã được hệ thống tạo thành công!";

            return RedirectToAction(nameof(VerifyOTP));
        }

        // =================================================================
        // BƯỚC XÁC THỰC OTP
        // =================================================================
        [HttpGet]
        public IActionResult VerifyOTP()
        {
            var email = HttpContext.Session.GetString("ResetEmail");
            if (string.IsNullOrEmpty(email)) return RedirectToAction(nameof(ForgotPassword));
            
            ViewBag.Email = email;
            return View();
        }

        [HttpPost]
        public IActionResult VerifyOTP(string inputOTP)
        {
            var sessionOTP = HttpContext.Session.GetString("ResetOTP");
            var expiryStr = HttpContext.Session.GetString("OTPExpiry");

            if (string.IsNullOrEmpty(sessionOTP) || string.IsNullOrEmpty(expiryStr))
            {
                ModelState.AddModelError("", "Mã OTP đã hết hạn hoặc không hợp lệ. Vui lòng yêu cầu lại!");
                return View();
            }

            if (DateTime.Now > DateTime.Parse(expiryStr))
            {
                ModelState.AddModelError("", "Mã OTP đã quá thời hạn 5 phút!");
                return View();
            }

            if (sessionOTP != inputOTP?.Trim())
            {
                ModelState.AddModelError("", "Mã OTP nhập vào không chính xác!");
                return View();
            }

            // Đánh dấu đã xác thực OTP thành công
            HttpContext.Session.SetString("IsOTPVerified", "TRUE");
            return RedirectToAction(nameof(ResetPassword));
        }

        // =================================================================
        // BƯỚC GỬI LẠI MÃ OTP MỚI
        // =================================================================
        [HttpPost]
        public IActionResult ResendOTP()
        {
            var email = HttpContext.Session.GetString("ResetEmail");
            if (string.IsNullOrEmpty(email))
            {
                return Json(new { success = false, message = "Phiên khôi phục đã hết hạn. Vui lòng thử lại từ đầu!" });
            }

            // 1. Sinh mã OTP 6 số MỚI
            string newOtp = new Random().Next(100000, 999999).ToString();

            // 2. Cập nhật lại Session
            HttpContext.Session.SetString("ResetOTP", newOtp);
            HttpContext.Session.SetString("OTPExpiry", DateTime.Now.AddMinutes(5).ToString());

            // 3. Trả về mã OTP mới dưới dạng JSON cho AJAX
            return Json(new { success = true, newOtp = newOtp, email = email });
        }

        // =================================================================
        // BƯỚC NHẬP MẬT KHẨU MỚI
        // =================================================================
        [HttpGet]
        public IActionResult ResetPassword()
        {
            var isVerified = HttpContext.Session.GetString("IsOTPVerified");
            if (isVerified != "TRUE") return RedirectToAction(nameof(ForgotPassword));

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(string newPassword, string confirmPassword)
        {
            if (string.IsNullOrEmpty(newPassword) || newPassword.Length < 6)
            {
                ModelState.AddModelError("", "Mật khẩu phải từ 6 ký tự trở lên!");
                return View();
            }

            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError("", "Mật khẩu nhập lại không trùng khớp!");
                return View();
            }

            var email = HttpContext.Session.GetString("ResetEmail");
            var khachHang = await _context.KhachHangs.FirstOrDefaultAsync(k => k.Email == email);

            if (khachHang != null)
            {
                khachHang.MatKhauMaHoa = newPassword; // Cập nhật mật khẩu mới
                await _context.SaveChangesAsync();

                // Xóa sạch Session khôi phục
                HttpContext.Session.Remove("ResetOTP");
                HttpContext.Session.Remove("ResetEmail");
                HttpContext.Session.Remove("OTPExpiry");
                HttpContext.Session.Remove("IsOTPVerified");

                TempData["ResetPasswordSuccessMessage"] = "Mật khẩu của bạn đã được cập nhật thành công. Vui lòng đăng nhập lại!";
                return RedirectToAction(nameof(Login));
            }

            return RedirectToAction(nameof(ForgotPassword));
        }
        
        [Authorize] // Bắt buộc đăng nhập mới được vào đổi
        [HttpGet]
        public IActionResult ChangePassword() => View();

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // Lấy ID người dùng hiện tại đang đăng nhập hệ thống
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return RedirectToAction(nameof(Login));
            int userId = int.Parse(userIdClaim);

            // Kiểm tra xem là Customer đổi hay Admin đổi dựa vào Role gắn lúc đăng nhập
            if (User.IsInRole("Customer"))
            {
                var customer = await _context.KhachHangs.FindAsync(userId);
                if (customer == null || customer.MatKhauMaHoa != model.OldPassword)
                {
                    ModelState.AddModelError("OldPassword", "Mật khẩu cũ không đúng!");
                    return View(model);
                }
                customer.MatKhauMaHoa = model.NewPassword; // Cập nhật mật khẩu mới
            }
            else if (User.IsInRole("Admin"))
            {
                var employee = await _context.NhanViens.FindAsync(userId);
                if (employee == null || employee.MatKhau != model.OldPassword)
                {
                    ModelState.AddModelError("OldPassword", "Mật khẩu cũ không đúng!");
                    return View(model);
                }
                employee.MatKhau = model.NewPassword;
            }

            await _context.SaveChangesAsync();
            ViewBag.SuccessMessage = "Thay đổi mật khẩu thành công!";
            return View();
        }
        #endregion

        // =================================================================
        // 1. TRANG HIỂN THỊ THÔNG TIN CÁ NHÂN
        // =================================================================
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Profile()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int customerId))
            {
                return RedirectToAction("Login");
            }

            var customer = await _context.KhachHangs.FindAsync(customerId);
            if (customer == null) return NotFound();

            return View(customer);
        }

        // =================================================================
        // 2. XỬ LÝ CẬP NHẬT THÔNG TIN QUA AJAX
        // =================================================================
        [HttpPost]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> UpdateProfile(string hoTen, string email)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int customerId))
            {
                return Json(new { success = false, message = "Phiên đăng nhập đã hết hạn!" });
            }

            if (string.IsNullOrEmpty(hoTen) || string.IsNullOrEmpty(email))
            {
                return Json(new { success = false, message = "Vui lòng nhập đầy đủ họ tên và email!" });
            }

            var customer = await _context.KhachHangs.FindAsync(customerId);
            if (customer == null)
            {
                return Json(new { success = false, message = "Không tìm thấy thông tin khách hàng!" });
            }

            // Kiểm tra trùng lặp email với tài khoản khác
            var emailExists = await _context.KhachHangs
                .AnyAsync(k => k.Email == email && k.KhachHangId != customerId);
            if (emailExists)
            {
                return Json(new { success = false, message = "Email này đã được sử dụng bởi tài khoản khác!" });
            }

            // Tiến hành cập nhật dữ liệu vào DB
            customer.HoTen = hoTen;
            customer.Email = email;
            await _context.SaveChangesAsync();

            // Cập nhật lại Cookie Identity để hiển thị đúng tên mới trên Header/Menu ngay lập tức
            var identity = (ClaimsIdentity)User.Identity;
            var nameClaim = identity.FindFirst(ClaimTypes.Name);
            var emailClaim = identity.FindFirst(ClaimTypes.Email);
            
            if (nameClaim != null) identity.RemoveClaim(nameClaim);
            if (emailClaim != null) identity.RemoveClaim(emailClaim);
            
            identity.AddClaim(new Claim(ClaimTypes.Name, hoTen));
            identity.AddClaim(new Claim(ClaimTypes.Email, email));

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

            return Json(new { success = true, message = "Cập nhật thông tin cá nhân thành công!" });
        }
        
        // =================================================================
        // 1. HIỂN THỊ DANH SÁCH ĐỊA CHỈ
        // =================================================================
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Addresses()
        {
            // Lấy KhachHangId từ Claim đăng nhập
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int customerId))
            {
                return RedirectToAction("Login");
            }

            // Lấy toàn bộ danh sách sổ địa chỉ của khách hàng này, đẩy địa chỉ mặc định lên đầu
            var listDiaChi = await _context.SoDiaChis
                .Where(d => d.KhachHangId == customerId)
                .OrderByDescending(d => d.IsDefault)
                .ToListAsync();

            return View(listDiaChi);
        }

        // =================================================================
        // 2. THÊM HOẶC SỬA ĐỊA CHỈ (XỬ LÝ QUA MODAL / AJAX ĐỂ TRANG MƯỢT MÀ)
        // =================================================================
        [HttpPost]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> SaveAddress(SoDiaChi model)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int customerId))
            {
                return Json(new { success = false, message = "Chưa đăng nhập!" });
            }

            if (string.IsNullOrEmpty(model.TenNguoiNhan) || string.IsNullOrEmpty(model.SoDienThoaiNhan) || string.IsNullOrEmpty(model.DiaChiGiao))
            {
                return Json(new { success = false, message = "Vui lòng nhập đầy đủ các trường bắt buộc!" });
            }

            // Nếu người dùng tick chọn địa chỉ này làm mặc định, hạ tất cả các địa chỉ cũ xuống false
            if (model.IsDefault)
            {
                var defaultAddresses = await _context.SoDiaChis
                    .Where(d => d.KhachHangId == customerId && d.IsDefault)
                    .ToListAsync();
                foreach (var addr in defaultAddresses)
                {
                    addr.IsDefault = false;
                }
            }

            if (model.DiaChiId == 0)
            {
                // HÀNH ĐỘNG THÊM MỚI
                model.KhachHangId = customerId;
                
                // Nếu đây là địa chỉ đầu tiên của họ, tự động gán làm mặc định luôn
                var hasAnyAddress = await _context.SoDiaChis.AnyAsync(d => d.KhachHangId == customerId);
                if (!hasAnyAddress) model.IsDefault = true;

                _context.SoDiaChis.Add(model);
            }
            else
            {
                // HÀNH ĐỘNG CẬP NHẬT (SỬA)
                var existingAddress = await _context.SoDiaChis
                    .FirstOrDefaultAsync(d => d.DiaChiId == model.DiaChiId && d.KhachHangId == customerId);
                
                if (existingAddress == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy địa chỉ hợp lệ!" });
                }

                existingAddress.TenNguoiNhan = model.TenNguoiNhan;
                existingAddress.SoDienThoaiNhan = model.SoDienThoaiNhan;
                existingAddress.DiaChiGiao = model.DiaChiGiao;
                existingAddress.LoaiDiaChi = model.LoaiDiaChi;
                // Nếu địa chỉ cũ đang là mặc định mà họ không chọn mặc định nữa thì giữ nguyên (tránh trường hợp không có địa chỉ nào mặc định)
                if (!existingAddress.IsDefault)
                {
                    existingAddress.IsDefault = model.IsDefault;
                }
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Lưu thông tin địa chỉ thành công!" });
        }

        // =================================================================
        // 3. ĐẶT LÀM ĐỊA CHỈ MẶC ĐỊNH CHANH CHÓNG
        // =================================================================
        [HttpPost]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> SetDefaultAddress(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int customerId))
            {
                return Json(new { success = false });
            }

            var address = await _context.SoDiaChis.FirstOrDefaultAsync(d => d.DiaChiId == id && d.KhachHangId == customerId);
            if (address == null) return Json(new { success = false });

            // Hạ tất cả các cái khác xuống
            var allAddresses = await _context.SoDiaChis.Where(d => d.KhachHangId == customerId).ToListAsync();
            foreach (var item in allAddresses)
            {
                item.IsDefault = (item.DiaChiId == id);
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        // =================================================================
        // 4. XÓA ĐỊA CHỈ
        // =================================================================
        [HttpPost]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> DeleteAddress(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int customerId))
            {
                return Json(new { success = false, message = "Chưa đăng nhập!" });
            }

            var address = await _context.SoDiaChis.FirstOrDefaultAsync(d => d.DiaChiId == id && d.KhachHangId == customerId);
            if (address == null)
            {
                return Json(new { success = false, message = "Địa chỉ không tồn tại!" });
            }

            if (address.IsDefault)
            {
                return Json(new { success = false, message = "Không thể xóa địa chỉ mặc định! Vui lòng đặt địa chỉ khác làm mặc định trước." });
            }

            _context.SoDiaChis.Remove(address);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đã xóa địa chỉ thành công!" });
        }

        // Hàm hỗ trợ gửi Email OTP qua Gmail SMTP
        private async Task SendResetPasswordEmailAsync(string toEmail, string otpCode)
        {
            var fromEmail = _configuration["EmailSettings:FromEmail"];
            var fromPassword = _configuration["EmailSettings:Password"];

            var smtpClient = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential(fromEmail, fromPassword),
                EnableSsl = true,
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(fromEmail, "Green Fresh Farm"),
                Subject = "[Green Fresh] Mã OTP đặt lại mật khẩu",
                Body = $@"
                    <div style='font-family: Arial, sans-serif; padding: 20px; background-color: #f8f9fa;'>
                        <div style='background-color: #ffffff; padding: 25px; border-radius: 10px; max-width: 500px; margin: auto; border: 1px solid #e2e8f0;'>
                            <h2 style='color: #198754; text-align: center;'>🌱 Green Fresh Farm</h2>
                            <p>Xin chào,</p>
                            <p>Bạn đã yêu cầu khôi phục mật khẩu cho tài khoản: <strong>{toEmail}</strong></p>
                            <p>Mã OTP xác thực của bạn là:</p>

                            <div style='text-align: center; margin: 20px 0;'>
                                <span style='font-size: 32px; font-weight: bold; color: #198754; letter-spacing: 8px; background: #e8f5e9; padding: 10px 25px; border-radius: 8px; display: inline-block;'>{otpCode}</span>
                            </div>

                            <p style='color: #6c757d; font-size: 13px;'>Mã này có hiệu lực trong <strong>5 phút</strong>. Vui lòng không tiết lộ mã này cho bất kỳ ai.</p>
                        </div>
                    </div>",
                IsBodyHtml = true,
            };
            mailMessage.To.Add(toEmail);

            await smtpClient.SendMailAsync(mailMessage);
        }

        // =================================================================
        // TRANG XEM LỊCH SỬ KHIẾU NẠI & HỖ TRỢ CSKH DÀNH CHO KHÁCH HÀNG
        // =================================================================
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> SupportHistory()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int customerId))
            {
                return RedirectToAction("Login");
            }

            // Lấy danh sách khiếu nại của khách hàng, kèm thông tin đơn hàng liên quan
            var dsKhieuNai = await _context.KhieuNais
                .Include(k => k.DonHangLe)
                .Where(k => k.KhachHangId == customerId)
                .OrderByDescending(k => k.NgayGui)
                .ToListAsync();

            return View(dsKhieuNai);
        }

        // =================================================================
        // TRANG XEM CHI TIẾT KHIẾU NẠI & HỖ TRỢ CSKH DÀNH CHO KHÁCH HÀNG
        // =================================================================
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> SupportDetail(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int customerId))
            {
                return RedirectToAction("Login");
            }

            // Truy vấn thông tin chi tiết khiếu nại kèm thông tin Đơn hàng, Đợt giao và Nhân viên hỗ trợ
            var khieuNai = await _context.KhieuNais
                .Include(k => k.DonHangLe)
                .Include(k => k.DotGiao)
                .Include(k => k.NhanVien)
                .FirstOrDefaultAsync(k => k.KhieuNaiId == id && k.KhachHangId == customerId);

            if (khieuNai == null)
            {
                return NotFound(); // Không tìm thấy hoặc khiếu nại không thuộc sở hữu của khách hàng này
            }

            return View(khieuNai);
        }
    }
}