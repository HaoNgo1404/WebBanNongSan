using System;
using System.Collections.Generic;

namespace WebWeb.Models
{
    public partial class GoiDangKyDinhKy
    {
        public GoiDangKyDinhKy()
        {
            ChiTietGoiDinhKies = new HashSet<ChiTietGoiDinhKy>();
            DotGiaoDinhKies = new HashSet<DotGiaoDinhKy>();
            GiaoDichThanhToans = new HashSet<GiaoDichThanhToan>();
        }

        public int GoiId { get; set; }
        public int KhachHangId { get; set; }
        public int DiaChiId { get; set; }
        public int KhuyenMaiId { get; set; }
        public DateTime NgayBatDau { get; set; }
        public DateTime NgayKetThuc { get; set; }
        public string TanSuatGiao { get; set; } = null!;
        public string ThuTrongTuan { get; set; } = null!;
        public decimal TongTienGoi { get; set; }
        public string TrangThaiGoi { get; set; } = null!;

        public virtual SoDiaChi? DiaChi { get; set; }
        public virtual KhachHang? KhachHang { get; set; }
        public virtual KhuyenMai? KhuyenMai { get; set; }
        public virtual ICollection<ChiTietGoiDinhKy>? ChiTietGoiDinhKies { get; set; }
        public virtual ICollection<DotGiaoDinhKy>? DotGiaoDinhKies { get; set; }
        public virtual ICollection<GiaoDichThanhToan>? GiaoDichThanhToans { get; set; }
    }
}
