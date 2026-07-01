using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebWeb.Models;
using WebWeb.ViewModels; // Nhận LoginViewModel và ChangePasswordViewModel

namespace WebWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AdminAccountController : Controller
    {
        private readonly ECommerceDBContext _context;

        public AdminAccountController(ECommerceDBContext context)
        {
            _context = context;
        }

        // ==========================================
        // 1. ĐĂNG NHẬP ADMIN (Không cần đăng nhập trước)
        // ==========================================
        [HttpGet]
        public IActionResult Login()
        {
            // Nếu đã đăng nhập Admin rồi thì đẩy thẳng vào Dashboard, không bắt đăng nhập lại
            if (User.Identity != null && User.Identity.IsAuthenticated && User.IsInRole("Admin"))
            {
                return RedirectToAction("Index", "Dashboard");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // Tìm tài khoản Nhân viên có quyền Admin trong Hệ thống
            var employee = await _context.NhanViens
                .FirstOrDefaultAsync(n => n.Email == model.Email && n.MatKhau == model.Password);

            if (employee != null)
            {
                // Tạo Claims chuẩn cho AdminAccount
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, employee.NhanVienId.ToString()),
                    new Claim(ClaimTypes.Name, employee.HoTen),
                    new Claim(ClaimTypes.Email, employee.Email),
                    new Claim(ClaimTypes.Role, "Admin")
                };

                var claimsIdentity = new ClaimsIdentity(claims, "AdminScheme");
                
                // Đăng nhập bằng AdminScheme
                await HttpContext.SignInAsync("AdminScheme", new ClaimsPrincipal(claimsIdentity));

                // Đăng nhập thành công, chuyển hướng vào trang chủ Admin
                return RedirectToAction("Index", "Dashboard");
            }

            ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu Admin không chính xác.");
            return View(model);
        }

        // ==========================================
        // 2. ĐĂNG XUẤT ADMIN (CHUYỂN THÀNH GET)
        // ==========================================
        public async Task<IActionResult> Logout()
        {
            // Đăng xuất và xóa sạch Cookie cấu hình của AdminScheme
            await HttpContext.SignOutAsync("AdminScheme");
            
            // Điều hướng rõ ràng về trang Login của AdminAccount thuộc Area Admin
            return RedirectToAction("Login", "AdminAccount", new { area = "Admin" });
        }

        // ==========================================
        // 3. XEM & CẬP NHẬT HỒ SƠ ADMIN (Bắt buộc quyền Admin)
        // ==========================================
        [Authorize(Roles = "Admin", AuthenticationSchemes = "AdminScheme")]
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return RedirectToAction(nameof(Login));

            int userId = int.Parse(userIdClaim);
            var adminInfo = await _context.NhanViens.FindAsync(userId);
            if (adminInfo == null) return NotFound();

            return View(adminInfo);
        }

        [Authorize(Roles = "Admin", AuthenticationSchemes = "AdminScheme")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(NhanVien model)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return RedirectToAction(nameof(Login));

            int userId = int.Parse(userIdClaim);
            var entity = await _context.NhanViens.FindAsync(userId);
            if (entity == null) return NotFound();

            if (ModelState.IsValid)
            {
                entity.HoTen = model.HoTen;
                _context.Update(entity);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Cập nhật thông tin hồ sơ thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(entity);
        }

        // ==========================================
        // 4. ĐỔI MẬT KHẨU ADMIN (Bắt buộc quyền Admin)
        // ==========================================
        [Authorize(Roles = "Admin", AuthenticationSchemes = "AdminScheme")]
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [Authorize(Roles = "Admin", AuthenticationSchemes = "AdminScheme")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return RedirectToAction(nameof(Login));

            int userId = int.Parse(userIdClaim);
            var entity = await _context.NhanViens.FindAsync(userId);
            if (entity == null) return NotFound();

            if (entity.MatKhau != model.OldPassword)
            {
                ModelState.AddModelError("OldPassword", "Mật khẩu cũ không chính xác.");
                return View(model);
            }

            entity.MatKhau = model.NewPassword;
            _context.Update(entity);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đổi mật khẩu thành công!";
            return View();
        }
    }
}