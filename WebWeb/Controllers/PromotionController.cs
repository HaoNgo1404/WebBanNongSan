using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebWeb.Models;
using WebWeb.Models.ViewModels;

namespace WebWeb.Controllers // Hoặc WebWeb.Areas.Customer.Controllers
{
    // Nếu nằm trong Area Customer thì thêm: [Area("Customer")]
    public class PromotionController : BaseController
    {
        private readonly ECommerceDBContext _context;

        public PromotionController(ECommerceDBContext context)
        {
            _context = context;
        }

        // ========================================================
        // ACTION CHECK VOUCHER CHO KHÁCH HÀNG
        // ========================================================
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> CheckVoucher(string code, List<CartItemViewModel> cartItems)
        {
            if (string.IsNullOrEmpty(code) || cartItems == null || !cartItems.Any())
            {
                return Json(new { success = false, message = "Dữ liệu giỏ hàng hoặc mã giảm giá trống!" });
            }

            // 1. Tính tổng tiền tạm tính ban đầu
            decimal tongTienToanGio = cartItems.Sum(item => item.DonGiaThoiDiem * item.SoLuongDat);

            // 2. Tìm mã voucher đang kích hoạt
            var cleanCode = code.Trim().ToUpper();
            var voucher = await _context.KhuyenMais
                .FirstOrDefaultAsync(k => k.VoucherCode != null && k.VoucherCode.ToUpper() == cleanCode && k.TrangThai == true);

            if (voucher == null) 
            {
                return Json(new { success = false, message = "Mã giảm giá không tồn tại hoặc đã bị tạm ngưng!" });
            }

            // 3. Kiểm tra các điều kiện cơ bản
            var now = DateTime.Now;
            if (now < voucher.NgayBatDau || now > voucher.NgayKetThuc)
                return Json(new { success = false, message = "Mã giảm giá đã hết hạn hoặc chưa tới thời gian áp dụng!" });

            if (voucher.SoLuotPhatHanh > 0 && voucher.SoLuotDaDung >= voucher.SoLuotPhatHanh)
                return Json(new { success = false, message = "Mã giảm giá này đã hết lượt sử dụng!" });

            if (tongTienToanGio < voucher.GiaTriDonToiThieu)
                return Json(new { success = false, message = $"Đơn hàng chưa đạt giá trị tối thiểu {voucher.GiaTriDonToiThieu:N0}đ để áp dụng mã này!" });

            // ========================================================
            // KIỂM TRA ĐIỀU KIỆN MÃ CHÀO MỪNG TÀI KHOẢN MỚI
            // ========================================================
            if (cleanCode == "BANMOI50")
            {
                int currentUserId = GetCurrentUserId(); // Lấy ID khách hàng
                if (currentUserId == 0)
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập tài khoản khách hàng để sử dụng mã ưu đãi này!" });
                }

                // 1. Tìm thông tin khách hàng kèm danh sách đơn hàng
                var khachHang = await _context.KhachHangs
                    .Include(kh => kh.DonHangLes)
                    .FirstOrDefaultAsync(kh => kh.KhachHangId == currentUserId);

                if (khachHang == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy thông tin tài khoản khách hàng!" });
                }

                // 2. KIỂM TRA NGÀY ĐĂNG KÝ
                var ngayDangKy = khachHang.NgayDangKy;
                int soNgayDaTroiQua = Math.Abs((DateTime.Now.Date - ngayDangKy.Date).Days);

                if (soNgayDaTroiQua > 20)
                {
                    return Json(new { 
                        success = false, 
                        message = $"Mã ưu đãi BANMOI50 đã hết hạn! Tài khoản của bạn đã tạo được {soNgayDaTroiQua} ngày (Chỉ áp dụng trong 30 ngày đầu tạo tài khoản)." 
                    });
                }

                // 3. QUÉT LỊCH SỬ ĐƠN HÀNG LẺ
                int soDonHangThucTe = 0;
                if (khachHang.DonHangLes != null && khachHang.DonHangLes.Any())
                {
                    soDonHangThucTe = khachHang.DonHangLes.Count(d => 
                        d.TrangThaiDonHang != null && 
                        !d.TrangThaiDonHang.Equals(OrderStatuses.DaHuy, StringComparison.OrdinalIgnoreCase) &&
                        !d.TrangThaiDonHang.Equals("Đã hủy", StringComparison.OrdinalIgnoreCase)
                    );
                }

                if (soDonHangThucTe > 0)
                {
                    return Json(new { success = false, message = "Mã giảm giá này chỉ áp dụng cho đơn hàng ĐẦU TIÊN của tài khoản mới!" });
                }
            }

            // ========================================================
            // LOGIC LỌC SẢN PHẨM THEO PHẠM VI KHÓA NGOẠI
            // ========================================================
            var danhSachHopLe = cartItems.AsQueryable();

            if (voucher.NongSanId.HasValue)
            {
                danhSachHopLe = danhSachHopLe.Where(i => i.NongSanId == voucher.NongSanId.Value);
            }
            else if (voucher.DanhMucId.HasValue)
            {
                var dsIdNongSanThuocDanhMuc = await _context.NongSans
                    .Where(n => n.DanhMucId == voucher.DanhMucId.Value)
                    .Select(n => n.NongSanId)
                    .ToListAsync();

                danhSachHopLe = danhSachHopLe.Where(i => dsIdNongSanThuocDanhMuc.Contains(i.NongSanId));
            }

            decimal tongTienHangDuocGiam = danhSachHopLe.Sum(item => item.DonGiaThoiDiem * item.SoLuongDat);

            if (tongTienHangDuocGiam == 0)
            {
                return Json(new { 
                    success = false, 
                    message = "Mã ưu đãi này không áp dụng cho bất kỳ sản phẩm nào hiện có trong giỏ hàng!" 
                });
            }

            // 4. Tính toán số tiền thực chiết khấu
            decimal soTienGiam = 0;
            if (voucher.LoaiGiamGia == 1) // Giảm theo %
            {
                soTienGiam = tongTienHangDuocGiam * (voucher.MucGiam / 100m);
                if (voucher.SoTienGiamToiDa > 0 && soTienGiam > voucher.SoTienGiamToiDa)
                {
                    soTienGiam = voucher.SoTienGiamToiDa;
                }
            }
            else // Giảm số tiền cố định
            {
                soTienGiam = voucher.MucGiam;
            }

            if (soTienGiam > tongTienToanGio) soTienGiam = tongTienToanGio;

            return Json(new { 
                success = true, 
                khuyenMaiId = voucher.KhuyenMaiId,
                soTienGiam = soTienGiam,
                message = $"Áp dụng mã thành công! Bạn được giảm {soTienGiam:N0}đ." 
            });
        }
    }
}