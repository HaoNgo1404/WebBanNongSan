using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using WebWeb.Models;

namespace WebWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin", AuthenticationSchemes = "AdminScheme")]
    public class CskhController : Controller
    {
        private readonly ECommerceDBContext _context;

        public CskhController(ECommerceDBContext context)
        {
            _context = context;
        }

        // 1. Danh sách ticket hỗ trợ
        public async Task<IActionResult> Index(string? trangThai)
        {
            var query = _context.SupportTickets
                .Include(t => t.KhachHang)
                .Include(t => t.NhanVien) // Include thêm thông tin nhân viên trả lời
                .AsQueryable();

            // XỬ LÝ LỌC TRẠNG THÁI
            if (!string.IsNullOrEmpty(trangThai) && trangThai != "TatCa")
            {
                // Trường hợp 1: Chọn tab "Đã xử lý" (hoặc truyền DaTraLoi/DaXem)
                if (trangThai == "DaXuLy" || trangThai == OrderStatuses.DaTraLoi || trangThai == OrderStatuses.DaXem)
                {
                    query = query.Where(t => t.TrangThai == OrderStatuses.DaTraLoi || t.TrangThai == OrderStatuses.DaXem);
                }
                // Trường hợp 2: Các trạng thái đơn lẻ khác (như "Chờ xử lý")
                else
                {
                    query = query.Where(t => t.TrangThai == trangThai);
                }
            }

            ViewBag.TrangThaiHienTai = trangThai ?? "TatCa";
            var dsTicket = await query.OrderByDescending(t => t.NgayTao).ToListAsync();
            return View(dsTicket);
        }

        // 2. Xử lý Trả lời từ Admin
        [HttpPost]
        public async Task<IActionResult> TraLoi(int ticketId, string noiDungTraLoi)
        {
            if (string.IsNullOrWhiteSpace(noiDungTraLoi))
            {
                TempData["ErrorMessage"] = "Nội dung trả lời không được để trống!";
                return RedirectToAction(nameof(Index));
            }

            var ticket = await _context.SupportTickets.FindAsync(ticketId);
            if (ticket == null) return NotFound();

            // Lấy nhanVienID của Admin đang đăng nhập (từ Claim hoặc Session)
            var adminIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(adminIdClaim, out int nhanVienId))
            {
                ticket.NhanVienID = nhanVienId;
            }

            ticket.AdminTraLoi = noiDungTraLoi;
            ticket.TrangThai = OrderStatuses.DaTraLoi; // Khớp với DB
            ticket.NgayPhanHoi = DateTime.Now;

            // Tự động lưu vào BotCache để AI học câu trả lời mới
            string cleanQuery = ticket.CauHoi.Trim().ToLower();
            bool hasCache = await _context.BotCaches.AnyAsync(b => b.UserQuery == cleanQuery);
            if (!hasCache)
            {
                _context.BotCaches.Add(new BotCache
                {
                    UserQuery = cleanQuery,
                    BotResponse = noiDungTraLoi,
                    CreatedAt = DateTime.Now,
                    HitCount = 1
                });
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã gửi phản hồi thành công và lưu kiến thức mới cho Bot!";
            return RedirectToAction(nameof(Index));
        }
    }
}