using System;
using System.Collections.Generic;

namespace WebWeb.Models
{
    public partial class KhieuNai
    {
        public int KhieuNaiId { get; set; }
        public int KhachHangId { get; set; }
        public int? DonHangLeId { get; set; }
        public int? DotGiaoId { get; set; }
        public int? NhanVienId { get; set; }
        public string NoiDung { get; set; } = null!;
        public string? HinhAnhMinhChung { get; set; }
        public DateTime NgayGui { get; set; }
        public string? PhuongAnXuLy { get; set; }
        public decimal SoTienHoan { get; set; }
        public int TrangThai { get; set; }

        public virtual DonHangLe? DonHangLe { get; set; }
        public virtual DotGiaoDinhKy? DotGiao { get; set; }
        public virtual KhachHang? KhachHang { get; set; }
        public virtual NhanVien? NhanVien { get; set; }
    }
}
