using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;
using WebWeb.Models;
using WebWeb.ViewModels; // Nhận LoginViewModel chung

namespace WebWeb.Areas.Shipper.Controllers
{
    [Area("Shipper")]
    [Route("Shipper/[controller]/[action]")]
    public class ShipperAccountController : Controller
    {
        private readonly ECommerceDBContext _context;

        public ShipperAccountController(ECommerceDBContext context)
        {
            _context = context;
        }

        // ==========================================
        // 1. ĐĂNG NHẬP SHIPPER
        // ==========================================
        [HttpGet]
        public IActionResult Login()
        {
            // Nếu tài xế đã đăng nhập bằng ShipperScheme rồi thì đẩy thẳng vào trang nhận đơn
            if (User.Identity != null && User.Identity.IsAuthenticated && User.IsInRole("Shipper"))
            {
                return RedirectToAction("Index", "Shipper", new { area = "Shipper" });
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // Dùng Include để kéo theo dữ liệu từ bảng VaiTroPhanQuyen lên cùng
            var shipper = await _context.NhanViens
                .Include(n => n.VaiTro) // Thay tên thuộc tính điều hướng chuẩn trong bảng NhanVien của bạn nếu khác
                .FirstOrDefaultAsync(n => n.Email == model.Email && n.MatKhau == model.Password);

            // Bạn có thể check thêm điều kiện: && shipper.VaiTro?.TenVaiTro == "Shipper"
            if (shipper != null)
            {
                // Lấy tên vai trò thực tế từ database, nếu null thì dự phòng là "Shipper"
                string tenVaiTro = shipper.VaiTro?.TenVaiTro ?? "Shipper";

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, shipper.NhanVienId.ToString()),
                    new Claim(ClaimTypes.Name, shipper.HoTen),
                    new Claim(ClaimTypes.Email, shipper.Email),
                    new Claim(ClaimTypes.Role, tenVaiTro) // Đưa tên vai trò chuẩn vào Claim Role
                };

                var claimsIdentity = new ClaimsIdentity(claims, "ShipperScheme");
                await HttpContext.SignInAsync("ShipperScheme", new ClaimsPrincipal(claimsIdentity));

                return RedirectToAction("Index", "Shipper", new { area = "Shipper" });
            }

            ModelState.AddModelError(string.Empty, "Tài khoản hoặc mật khẩu Shipper không chính xác.");
            return View(model);
        }

        // ==========================================
        // 2. ĐĂNG XUẤT SHIPPER
        // ==========================================
        public async Task<IActionResult> Logout()
        {
            // Chỉ xóa sạch Cookie của ShipperScheme
            await HttpContext.SignOutAsync("ShipperScheme");
            
            return RedirectToAction("Login", "ShipperAccount", new { area = "Shipper" });
        }

        // ==========================================
        // 3. XEM & CẬP NHẬT HỒ SƠ SHIPPER (Bắt buộc quyền Shipper)
        // ==========================================
        [Authorize(Roles = "Shipper", AuthenticationSchemes = "ShipperScheme")]
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return RedirectToAction(nameof(Login));

            int userId = int.Parse(userIdClaim);
            
            // Thêm Include tại đây để lấy thông tin vai trò hiển thị lên Form hồ sơ
            var shipperInfo = await _context.NhanViens
                .Include(n => n.VaiTro)
                .FirstOrDefaultAsync(n => n.NhanVienId == userId);

            if (shipperInfo == null) return NotFound();

            return View(shipperInfo);
        }

        [Authorize(Roles = "Shipper", AuthenticationSchemes = "ShipperScheme")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(NhanVien model)
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return RedirectToAction(nameof(Login));

            int userId = int.Parse(userIdClaim);
            var entity = await _context.NhanViens.FindAsync(userId);
            if (entity == null) return NotFound();

            // Chỉ cho phép sửa Họ tên (hoặc Số điện thoại tùy bạn cấu hình thêm)
            entity.HoTen = model.HoTen;
            
            _context.Update(entity);
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = "Cập nhật thông tin hồ sơ tài xế thành công!";
            return RedirectToAction("Index", "Shipper", new { area = "Shipper" });
        }

        // ==========================================
        // 4. ĐỔI MẬT KHẨU SHIPPER (Bắt buộc quyền Shipper)
        // ==========================================
        [Authorize(Roles = "Shipper", AuthenticationSchemes = "ShipperScheme")]
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [Authorize(Roles = "Shipper", AuthenticationSchemes = "ShipperScheme")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return RedirectToAction(nameof(Login));

            int userId = int.Parse(userIdClaim);
            var entity = await _context.NhanViens.FindAsync(userId);
            if (entity == null) return NotFound();

            if (entity.MatKhau != model.OldPassword)
            {
                ModelState.AddModelError("OldPassword", "Mật khẩu cũ của tài xế không chính xác.");
                return View(model);
            }

            entity.MatKhau = model.NewPassword;
            _context.Update(entity);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Tài xế đổi mật khẩu thành công!";
            return View();
        }
    }
}