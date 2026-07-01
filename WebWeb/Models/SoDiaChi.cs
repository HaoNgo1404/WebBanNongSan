using System;
using System.Collections.Generic;

namespace WebWeb.Models
{
    public partial class SoDiaChi
    {
        public SoDiaChi()
        {
            DonHangLes = new HashSet<DonHangLe>();
            GoiDangKyDinhKies = new HashSet<GoiDangKyDinhKy>();
        }

        public int DiaChiId { get; set; }
        public int KhachHangId { get; set; }
        public string TenNguoiNhan { get; set; } = null!;
        public string SoDienThoaiNhan { get; set; } = null!;
        public string DiaChiGiao { get; set; } = null!;
        public string? LoaiDiaChi { get; set; }
        public bool IsDefault { get; set; }

        public virtual KhachHang KhachHang { get; set; } = null!;
        public virtual ICollection<DonHangLe> DonHangLes { get; set; }
        public virtual ICollection<GoiDangKyDinhKy> GoiDangKyDinhKies { get; set; }
    }
}
