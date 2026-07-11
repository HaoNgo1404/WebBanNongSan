using System;
using System.Collections.Generic;

namespace WebWeb.Models
{
    public partial class DotGiaoDinhKy
    {
        public DotGiaoDinhKy()
        {
            KhieuNais = new HashSet<KhieuNai>();
        }

        public int DotGiaoId { get; set; }
        public int GoiId { get; set; }
        public int? NhanVienId { get; set; }
        public DateTime NgayGiaoThucTe { get; set; }
        public decimal? TrongLuongThucTeDot { get; set; }
        public string TrangThaiGiao { get; set; } = null!;

        public virtual GoiDangKyDinhKy? Goi { get; set; }
        public virtual NhanVien? NhanVien { get; set; }
        public virtual ICollection<KhieuNai>? KhieuNais { get; set; }
    }
}
