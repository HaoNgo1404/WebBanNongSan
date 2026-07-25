using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WebWeb.Models;

namespace WebWeb.Services
{
    public class KhuyenMaiService
    {
        private readonly ECommerceDBContext _context;

        public KhuyenMaiService(ECommerceDBContext context)
        {
            _context = context;
        }

        // =========================================================================
        // 1. TÍNH GIÁ BÁN THỰC TẾ HÀNG HÓA (Hiển thị ra View/Danh sách sản phẩm)
        // =========================================================================
        public decimal TinhGiaBanThucTe(int nongSanId, decimal giaGoc)
        {
            var now = DateTime.Now;

            // Lấy thông tin sản phẩm
            var product = _context.NongSans.AsNoTracking().FirstOrDefault(n => n.NongSanId == nongSanId);
            if (product == null) return giaGoc;

            // Tìm chương trình khuyến mãi tự động (VoucherCode trống hoặc null) đang kích hoạt
            var km = _context.KhuyenMais.AsNoTracking()
                .FirstOrDefault(k => k.TrangThai == true 
                                && now >= k.NgayBatDau 
                                && now <= k.NgayKetThuc
                                && string.IsNullOrEmpty(k.VoucherCode)
                                && (k.NongSanId == nongSanId || k.DanhMucId == product.DanhMucId));

            if (km == null) return giaGoc;

            // Tính số tiền giảm
            decimal soTienGiam = 0;
            if (km.LoaiGiamGia == 1) // Giảm theo %
            {
                soTienGiam = giaGoc * (km.MucGiam / 100m);
                if (km.SoTienGiamToiDa > 0 && soTienGiam > km.SoTienGiamToiDa) 
                {
                    soTienGiam = km.SoTienGiamToiDa;
                }
            }
            else // Giảm số tiền cố định
            {
                soTienGiam = km.MucGiam;
            }

            return Math.Max(0, giaGoc - soTienGiam);
        }

        // =========================================================================
        // 2. KIỂM TRA VÀ ÁP DỤNG MÃ VOUCHER (Dùng cho Checkout/Giỏ hàng & BANMOI50)
        // =========================================================================
        public async Task<(bool HopLe, string ThongBao, decimal SoTienGiam)> KiemTraVoucherAsync(string maVoucher, int? khachHangId, decimal tongTienDonHang)
        {
            if (string.IsNullOrWhiteSpace(maVoucher))
            {
                return (false, "Vui lòng nhập mã giảm giá!", 0);
            }

            var now = DateTime.Now;
            var cleanCode = maVoucher.Trim().ToLower();

            // Tìm voucher theo VoucherCode khớp trong DB
            var voucher = await _context.KhuyenMais.FirstOrDefaultAsync(k => 
                k.VoucherCode != null && k.VoucherCode.ToLower() == cleanCode && k.TrangThai == true);

            if (voucher == null)
            {
                return (false, "Mã khuyến mãi không tồn tại hoặc đã bị khóa!", 0);
            }

            if (now < voucher.NgayBatDau || now > voucher.NgayKetThuc)
            {
                return (false, "Mã khuyến mãi đã hết hạn hoặc chưa đến thời gian áp dụng!", 0);
            }

            // Kiểm tra số lượt phát hành (Sử dụng SoLuotPhatHanh từ Model KhuyenMai)
            if (voucher.SoLuotPhatHanh > 0 && voucher.SoLuotDaDung >= voucher.SoLuotPhatHanh)
            {
                return (false, "Mã khuyến mãi này đã hết lượt sử dụng!", 0);
            }

            // Kiểm tra giá trị đơn tối thiểu (Sử dụng GiaTriDonToiThieu từ Model KhuyenMai)
            if (voucher.GiaTriDonToiThieu > 0 && tongTienDonHang < voucher.GiaTriDonToiThieu)
            {
                return (false, $"Đơn hàng phải đạt tối thiểu {voucher.GiaTriDonToiThieu:N0}đ để áp dụng mã này!", 0);
            }

            // --- ĐIỀU KIỆN ĐẶC BIỆT DÀNH RIÊNG CHO MÃ BANMOI50 ---
            if (cleanCode == "banmoi50")
            {
                if (!khachHangId.HasValue)
                {
                    return (false, "Vui lòng đăng nhập tài khoản để áp dụng mã ưu đãi người mới!", 0);
                }

                // Load Khách hàng kèm tập hợp Đơn hàng lẻ
                var khachHang = await _context.KhachHangs
                    .Include(k => k.DonHangLes)
                    .FirstOrDefaultAsync(k => k.KhachHangId == khachHangId.Value);

                if (khachHang == null)
                {
                    return (false, "Không tìm thấy thông tin tài khoản!", 0);
                }

                // 1. KIỂM TRA SỐ LƯỢNG ĐƠN HÀNG LẺ ĐÃ ĐẶT
                // Chỉ tính những đơn khác trạng thái 'DaHuy' (Đã hủy)
                bool daCoDonHang = khachHang.DonHangLes != null && 
                                khachHang.DonHangLes.Any(d => d.TrangThaiDonHang != "DaHuy");

                if (daCoDonHang)
                {
                    return (false, "Mã BANMOI50 chỉ áp dụng cho đơn hàng ĐẦU TIÊN của bạn!", 0);
                }

                // 2. KIỂM TRA NGÀY ĐĂNG KÝ (Nên để thông báo rõ ràng hơn nếu bị quá hạn)
                if (khachHang.NgayDangKy < DateTime.Now.AddDays(-7))
                {
                    return (false, "Mã BANMOI50 đã hết hạn do tài khoản của bạn đăng ký quá 7 ngày!", 0);
                }
            }
            // Tính toán số tiền được giảm
            decimal soTienGiam = 0;
            if (voucher.LoaiGiamGia == 1) // Giảm %
            {
                soTienGiam = tongTienDonHang * (voucher.MucGiam / 100m);
                if (voucher.SoTienGiamToiDa > 0 && soTienGiam > voucher.SoTienGiamToiDa)
                {
                    soTienGiam = voucher.SoTienGiamToiDa;
                }
            }
            else // Giảm tiền trực tiếp
            {
                soTienGiam = voucher.MucGiam;
            }

            return (true, "Áp dụng mã giảm giá thành công!", soTienGiam);
        }
    }
}