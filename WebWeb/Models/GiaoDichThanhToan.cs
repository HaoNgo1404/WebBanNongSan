using System;
using System.Collections.Generic;

namespace WebWeb.Models
{
    public partial class GiaoDichThanhToan
    {
        public int GiaoDichId { get; set; }
        public int? DonHangLeId { get; set; }
        public int? GoiDangKyId { get; set; }
        public string MaGiaoDichCong { get; set; } = null!;
        public decimal SoTien { get; set; }
        public string PhuongThuc { get; set; } = null!;
        public DateTime NgayGiaoDich { get; set; }
        public int TrangThai { get; set; }

        public virtual DonHangLe? DonHangLe { get; set; }
        public virtual GoiDangKyDinhKy? GoiDangKy { get; set; }
    }
}
