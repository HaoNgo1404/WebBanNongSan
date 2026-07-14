using System;
using System.Linq;
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

        // Hàm tính giá chạy đồng bộ (Synchronous) dành riêng cho việc hiển thị ngoài View
        public decimal TinhGiaBanThucTe(int nongSanId, decimal giaGoc)
        {
            var now = DateTime.Now;

            // Lấy mã danh mục của nông sản
            var product = _context.NongSans.AsNoTracking().FirstOrDefault(n => n.NongSanId == nongSanId);
            if (product == null) return giaGoc;
            int danhMucId = product.DanhMucId;

            // Quét tìm khuyến mãi tự động (VoucherCode trống hoặc null)
            var km = _context.KhuyenMais.AsNoTracking()
                .FirstOrDefault(k => k.TrangThai == true 
                                && now >= k.NgayBatDau 
                                && now <= k.NgayKetThuc
                                && (k.VoucherCode == null || k.VoucherCode == "")
                                && (k.NongSanId == nongSanId || k.DanhMucId == danhMucId));

            if (km == null) return giaGoc;

            // Tính toán số tiền giảm
            decimal soTienGiam = 0;
            if (km.LoaiGiamGia == 1) // Giảm %
            {
                soTienGiam = giaGoc * (km.MucGiam / 100m);
                if (km.SoTienGiamToiDa > 0 && soTienGiam > km.SoTienGiamToiDa) soTienGiam = km.SoTienGiamToiDa;
            }
            else // Giảm tiền cố định
            {
                soTienGiam = km.MucGiam;
            }

            return Math.Max(0, giaGoc - soTienGiam);
        }
    }
}