using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using WebWeb.Models;
using WebWeb.ViewModels;

namespace WebWeb.Controllers
{
    public class AccountController : Controller
    {
        private readonly ECommerceDBContext _context;

        public AccountController(ECommerceDBContext context)
        {
            _context = context;
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

            return RedirectToAction(nameof(Login));
        }
        #endregion

        #region ĐĂNG XUẤT
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }
        #endregion

        #region THAY ĐỔI MẬT KHẨU (Bổ sung theo yêu cầu)
        
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
    }
}