using System;
using System.Collections.Generic;

namespace WebWeb.Models
{
    public partial class DanhGiaSanPham
    {
        public int DanhGiaId { get; set; }
        public int KhachHangId { get; set; }
        public int NongSanId { get; set; }
        public int DonHangLeId { get; set; }
        public int SoSao { get; set; }
        public string? BinhLuan { get; set; }
        public string? HinhAnhThucTe { get; set; }
        public DateTime NgayDanhGia { get; set; }

        public virtual DonHangLe DonHangLe { get; set; } = null!;
        public virtual KhachHang KhachHang { get; set; } = null!;
        public virtual NongSan NongSan { get; set; } = null!;
    }
}
